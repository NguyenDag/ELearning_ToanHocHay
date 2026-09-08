#!/usr/bin/env bash
#
# Import 3 bộ khung chương trình Toán 6 vào một môi trường đang chạy và publish.
# Yêu cầu: bash, curl, jq. Chạy từ thư mục docs/content-import/.
#
#   export THH_API="https://<domain>"
#   export THH_EMAIL="admin@..."
#   export THH_PASSWORD="..."
#   ./import-all.sh [--dry-run] [--no-publish] [--replace] [slug ...]
#
#   --dry-run     chỉ validate, không ghi gì
#   --no-publish  import xong để version ở Draft (tự submit/review/publish sau)
#   --replace     nếu khoá học đã tồn tại thì import đè vào version Draft của nó
#   slug ...      chỉ chạy các bộ được nêu (mặc định: cả 3)
#
set -euo pipefail

API="${THH_API:?dat bien THH_API}"
EMAIL="${THH_EMAIL:?dat bien THH_EMAIL}"
PASSWORD="${THH_PASSWORD:?dat bien THH_PASSWORD}"

DRY_RUN=0
NO_PUBLISH=0
REPLACE=0
SLUGS=()

for arg in "$@"; do
  case "$arg" in
    --dry-run)    DRY_RUN=1 ;;
    --no-publish) NO_PUBLISH=1 ;;
    --replace)    REPLACE=1 ;;
    -*)           echo "Tham so la: $arg" >&2; exit 2 ;;
    *)            SLUGS+=("$arg") ;;
  esac
done

if [ "${#SLUGS[@]}" -eq 0 ]; then
  SLUGS=(toan-6-ket-noi-tri-thuc toan-6-chan-troi-sang-tao toan-6-canh-dieu)
fi

command -v jq   >/dev/null || { echo "Can cai jq"   >&2; exit 1; }
command -v curl >/dev/null || { echo "Can cai curl" >&2; exit 1; }

say() { printf '\033[1;36m==>\033[0m %s\n' "$*"; }
die() { printf '\033[1;31mLOI:\033[0m %s\n' "$*" >&2; exit 1; }

# ---- 1. token ----------------------------------------------------------------
say "Dang nhap $EMAIL @ $API"
LOGIN=$(curl -sS -X POST "$API/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "$(jq -n --arg e "$EMAIL" --arg p "$PASSWORD" '{Email:$e, Password:$p}')")
TOKEN=$(echo "$LOGIN" | jq -r '.Data.Token // empty')
[ -n "$TOKEN" ] || die "Dang nhap that bai: $(echo "$LOGIN" | jq -r '.Message // .')"
ROLE=$(echo "$LOGIN" | jq -r '.Data.UserType')
say "OK — vai tro: $ROLE"

AUTH=(-H "Authorization: Bearer $TOKEN")

files_of() {
  local slug="$1" out=()
  # "tên file (không .csv):Tên trường form"
  local map="course:Course nodes:Nodes blocks:Blocks flashcards:Flashcards resources:Resources \
question-bank:QuestionBank questions:Questions question-options:QuestionOptions exercises:Exercises exercise-questions:ExerciseQuestions"
  for pair in $map; do
    local name="${pair%%:*}" field="${pair##*:}"
    [ -f "$slug/$name.csv" ] && out+=(-F "$field=@$slug/$name.csv;type=text/csv")
  done
  printf '%s\n' "${out[@]}"
}

print_issues() {
  echo "$1" | jq -r '
    (.Data.Issues // [])
    | map(select(.Severity=="Error"))
    | if length==0 then "  (khong co loi)"
      else (.[] | "  [\(.File)#\(.Row // "-")] \(.Code): \(.Message)")
      end'
}

# ---- 2. moi bo sach --------------------------------------------------------
declare -a DONE
for slug in "${SLUGS[@]}"; do
  [ -d "$slug" ] || die "Khong thay thu muc $slug (chay tu docs/content-import/)"
  echo
  say "=== $slug ==="

  mapfile -t FF < <(files_of "$slug")

  # 2a. validate
  say "Kiem tra file (validate)"
  V=$(curl -sS -X POST "$API/api/content/import/validate" "${AUTH[@]}" "${FF[@]}")
  echo "$V" | jq -r '"  Valid=\(.Data.Valid)  Loi=\(.Data.ErrorCount)  Canh bao=\(.Data.WarningCount)  " +
      "Chuong=\(.Data.Counts.Chapters) Bai=\(.Data.Counts.Lessons) Block=\(.Data.Counts.Blocks) The=\(.Data.Counts.Flashcards)"'
  if [ "$(echo "$V" | jq -r '.Data.Valid')" != "true" ]; then
    print_issues "$V"
    die "$slug: file co loi, dung lai."
  fi

  if [ "$DRY_RUN" -eq 1 ]; then
    say "--dry-run: bo qua ghi."
    continue
  fi

  # 2b. import
  EXISTING=$(curl -sS "$API/api/courses/by-slug/$slug" "${AUTH[@]}" | jq -r '.Data.CourseId // empty')
  if [ -n "$EXISTING" ] && [ "$REPLACE" -eq 1 ]; then
    VID=$(curl -sS "$API/api/courses/$EXISTING/versions" "${AUTH[@]}" \
          | jq -r '[.Data[] | select(.State=="Draft")] | sort_by(.VersionNumber) | last | .CourseVersionId // empty')
    [ -n "$VID" ] || die "$slug: khoa hoc da co nhung khong con version Draft — tao version moi truoc."
    say "Import de vao version Draft #$VID (--replace)"
    R=$(curl -sS -X POST "$API/api/content/import/versions/$VID?replace=true" "${AUTH[@]}" "${FF[@]}")
  elif [ -n "$EXISTING" ]; then
    die "$slug: khoa hoc da ton tai (CourseId=$EXISTING). Dung --replace hoac tao version moi thu cong."
  else
    say "Import tao khoa hoc moi"
    R=$(curl -sS -X POST "$API/api/content/import/course" "${AUTH[@]}" "${FF[@]}")
  fi

  if [ "$(echo "$R" | jq -r '.Data.Committed')" != "true" ]; then
    print_issues "$R"
    die "$slug: import that bai — $(echo "$R" | jq -r '.Message')"
  fi
  VID=$(echo "$R" | jq -r '.Data.CourseVersionId')
  CID=$(echo "$R" | jq -r '.Data.CourseId')
  say "Da import: CourseId=$CID  CourseVersionId=$VID  Job=$(echo "$R" | jq -r '.Data.ImportJobId')"

  # 2c. publish
  if [ "$NO_PUBLISH" -eq 1 ]; then
    DONE+=("$slug  CourseId=$CID  VersionId=$VID  (Draft — chua publish)")
    continue
  fi

  say "submit -> review(Approve) -> publish"
  curl -sS -X POST "$API/api/courses/versions/$VID/submit"  "${AUTH[@]}" | jq -e '.Success' >/dev/null \
    || die "$slug: submit that bai"
  curl -sS -X POST "$API/api/courses/versions/$VID/review"  "${AUTH[@]}" \
       -H 'Content-Type: application/json' -d '{"Decision":"Approve"}' | jq -e '.Success' >/dev/null \
    || die "$slug: review that bai (vai tro $ROLE co quyen duyet khong?)"
  curl -sS -X POST "$API/api/courses/versions/$VID/publish" "${AUTH[@]}" | jq -e '.Success' >/dev/null \
    || die "$slug: publish that bai"

  DONE+=("$slug  CourseId=$CID  VersionId=$VID  Published")
done

# ---- 3. tom tat ----------------------------------------------------------
echo
say "XONG"
printf '  %s\n' "${DONE[@]:-(khong co bo nao duoc ghi)}"
echo
say "Kiem tra:  curl -s \"$API/api/courses?subjectId=1&gradeLevelId=1\" | jq '.Data[] | {Title, Status}'"
