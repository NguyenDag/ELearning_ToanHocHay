# Hướng dẫn import khung chương trình lên production

Quy trình để tài khoản **SystemAdmin** nạp 3 bộ khung chương trình Toán 6 trong
thư mục này vào một môi trường đang chạy và xuất bản (publish) cho học sinh thấy.

> SystemAdmin làm được cả 3 khâu: **import → duyệt (review) → publish**.
> ContentEditor chỉ import + submit; AcademicReviewer duyệt + publish.

Có 2 cách: **Swagger UI** (bấm nút) hoặc **script / curl**. Xem [`import-all.sh`](import-all.sh)
để chạy cả 3 bộ trong một lệnh.

---

## 0. Điều kiện

| Cần | Ghi chú |
|---|---|
| Tài khoản SystemAdmin | tạo tự động từ biến môi trường `DefaultAdmin__Email` / `DefaultAdmin__Password` khi deploy |
| `Subject` mã `MATH`, `GradeLevel` mã `G6` | đã được seed sẵn khi migrate — không phải làm gì. Nếu thiếu: tạo qua `POST /api/catalog/subjects`, `/api/catalog/grade-levels` |
| `CurriculumFramework` mã `KNTT` / `CTST` / `CD` | đã seed sẵn. Nếu chưa có, importer **tự tạo** khi chạy |
| Reverse proxy cho phép upload vài MB | request thực tế ~0,3 MB; nginx mặc định `client_max_body_size` chỉ 1 MB |
| Bộ file trong thư mục này | lấy từ repo, hoặc tạo lại bằng `tools/CurriculumImportBuilder` (`dotnet run -c Release`) |

Các file mỗi bộ sách:
- Khung chương trình: `course.csv`, `nodes.csv`, `blocks.csv`, `flashcards.csv`, `resources.csv`
- Đánh giá (tuỳ chọn, nạp cùng lúc): `question-bank.csv`, `questions.csv`, `question-options.csv`, `exercises.csv`, `exercise-questions.csv`
  — tạo ngân hàng câu hỏi + bài tập gắn vào node chương/bài; mỗi bài tập có tier `Free` / `Standard` / `Premium`.

Lược đồ từng cột: xem [README.md](README.md).

---

## 1. Cách A — Swagger UI (không cần lệnh)

1. Mở `https://<domain>/swagger`.
2. `POST /api/auth/login` → "Try it out" → nhập email/mật khẩu admin → **Execute** → copy `Data.Token`.
3. Bấm **Authorize** (góc trên phải) → nhập `Bearer <token>` → Authorize.
4. Nhóm **ContentImport**:
   - `POST /api/content/import/validate` → "Try it out" → chọn các file của bộ KNTT (5 file khung + 5 file đánh giá) → **Execute**.
     Xem `Data.Valid = true`. Nếu có `Issues` mức `Error` thì sửa file rồi thử lại.
   - `POST /api/content/import/course` → chọn lại các file → **Execute**.
     Ghi lại `Data.CourseId` và `Data.CourseVersionId`. `Data.Counts` có cả số `Questions` / `Exercises`.
5. Lặp bước 4 cho `toan-6-chan-troi-sang-tao` và `toan-6-canh-dieu`.
6. Nhóm **Courses**, với mỗi `CourseVersionId`:
   - `POST /api/courses/versions/{versionId}/submit`
   - `POST /api/courses/versions/{versionId}/review` body `{ "Decision": "Approve" }`
   - `POST /api/courses/versions/{versionId}/publish`
7. Kiểm tra: `GET /api/courses?subjectId=1&gradeLevelId=1` (không cần token) — phải thấy 3 khoá `Status: "Published"`.

---

## 2. Cách B — script

```bash
# cần: bash, curl, jq
export THH_API="https://<domain>"
export THH_EMAIL="admin@..."
export THH_PASSWORD="..."

cd docs/content-import
./import-all.sh                 # import + submit + approve + publish cả 3 bộ
./import-all.sh --dry-run       # chỉ validate, không ghi gì
./import-all.sh --no-publish    # import xong để ở Draft, tự publish sau
```

Script tự: đăng nhập → validate → import từng bộ → submit → review Approve → publish → in tóm tắt.
Nếu validate có lỗi mức `Error`, script dừng và in danh sách lỗi.

---

## 3. Cách C — curl từng bước

```bash
API=https://<domain>
EMAIL=admin@... ; PASS=...
SLUG=toan-6-ket-noi-tri-thuc          # đổi cho từng bộ

# 3.1 token
TOKEN=$(curl -s -X POST "$API/api/auth/login" -H "Content-Type: application/json" \
  -d "{\"Email\":\"$EMAIL\",\"Password\":\"$PASS\"}" | jq -r '.Data.Token')

# các phần -F dùng chung (5 file khung + 5 file đánh giá)
FILES=(
  -F "Course=@$SLUG/course.csv"                 -F "Nodes=@$SLUG/nodes.csv"
  -F "Blocks=@$SLUG/blocks.csv"                 -F "Flashcards=@$SLUG/flashcards.csv"
  -F "Resources=@$SLUG/resources.csv"           -F "QuestionBank=@$SLUG/question-bank.csv"
  -F "Questions=@$SLUG/questions.csv"           -F "QuestionOptions=@$SLUG/question-options.csv"
  -F "Exercises=@$SLUG/exercises.csv"           -F "ExerciseQuestions=@$SLUG/exercise-questions.csv"
)

# 3.2 validate (không ghi)
curl -s -X POST "$API/api/content/import/validate" -H "Authorization: Bearer $TOKEN" \
  "${FILES[@]}" | jq '{Valid, ErrorCount, WarningCount, Counts}'

# 3.3 import → tạo khoá học + version Draft v1 (kèm ngân hàng câu hỏi + bài tập)
VID=$(curl -s -X POST "$API/api/content/import/course" -H "Authorization: Bearer $TOKEN" \
  "${FILES[@]}" | jq -r '.Data.CourseVersionId')

# 3.4 Draft → Published
curl -s -X POST "$API/api/courses/versions/$VID/submit"  -H "Authorization: Bearer $TOKEN" >/dev/null
curl -s -X POST "$API/api/courses/versions/$VID/review"  -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" -d '{"Decision":"Approve"}' >/dev/null
curl -s -X POST "$API/api/courses/versions/$VID/publish" -H "Authorization: Bearer $TOKEN"
```

---

## 4. Kiểm tra sau khi publish

```bash
curl -s "$API/api/courses?subjectId=1&gradeLevelId=1" | jq '.Data[] | {Title, Slug, Status, PublishedVersionId}'
curl -s "$API/api/content/versions/$VID/tree"     -H "Authorization: Bearer $TOKEN" | jq '.Data | length'
curl -s "$API/api/content/import/jobs"            -H "Authorization: Bearer $TOKEN" | jq '.Data[] | {ImportJobId, Status, TotalRows, CreatedAt}'
```

`GET /api/content/import/jobs/{id}` trả chi tiết một lần import kèm danh sách issue đã lưu.

---

## 5. Cập nhật nội dung về sau

Không import trực tiếp vào version đang `Published`. Hai lựa chọn:

**a) Version mới (khuyến nghị — giữ bản đang chạy):**
```bash
NEWVID=$(curl -s -X POST "$API/api/courses/$COURSE_ID/versions" -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"Label":"Cập nhật 2026-2027"}' | jq -r '.Data.CourseVersionId')
curl -s -X POST "$API/api/content/import/versions/$NEWVID" -H "Authorization: Bearer $TOKEN" \
  -F "Nodes=@$SLUG/nodes.csv" -F "Blocks=@$SLUG/blocks.csv" \
  -F "Flashcards=@$SLUG/flashcards.csv" -F "Resources=@$SLUG/resources.csv"
# rồi submit → review → publish; version cũ tự chuyển Archived
```

**b) Ghi đè Draft hiện có:** thêm `?replace=true` → xoá sạch nội dung của version đó rồi ghi lại.

Thêm `?dryRun=true` vào bất kỳ endpoint import nào để thử mà không ghi.

---

## 6. Giới hạn & cấu hình

| Khoá `SystemConfig` | Mặc định | Ý nghĩa |
|---|---|---|
| `content.import.maxRowsPerJob` | 5000 | tổng dòng (nodes + blocks + cards + resources) / lần |
| `content.maxTreeDepth` | 4 | độ sâu tối đa của cây node |
| — (hằng trong code) | 10 MB / file, 30 MB / request | kích thước upload |

Chỉnh 2 khoá đầu qua `PUT /api/admin/config/{key}` body `{ "Value": "8000" }` (SystemAdmin).
