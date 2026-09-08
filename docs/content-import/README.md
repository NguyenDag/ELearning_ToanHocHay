# Khung chương trình Toán 6 — bộ file import

Bộ dữ liệu nội dung cho 3 khung chương trình (bộ sách giáo khoa) môn Toán lớp 6:

| Thư mục / file | Bộ sách | Khung |
|---|---|---|
| `toan-6-ket-noi-tri-thuc/` · `.xlsx` | Kết nối tri thức với cuộc sống (KNTT) | **Biên soạn chi tiết** — 9 chương, 43 bài + luyện tập chung / bài tập cuối chương, mỗi bài có heading, lý thuyết, định nghĩa, công thức (LaTeX), ví dụ có lời giải, chú ý, flashcard |
| `toan-6-chan-troi-sang-tao/` · `.xlsx` | Chân trời sáng tạo (CTST) | Danh mục đầy đủ chương + bài, mỗi bài một khung chuẩn (tiêu đề + mục tiêu) |
| `toan-6-canh-dieu/` · `.xlsx` | Cánh Diều (CD) | Danh mục đầy đủ chương + bài, mỗi bài một khung chuẩn |

Mỗi bộ sách có **2 dạng** giống hệt nhau về nội dung:

- Một thư mục **CSV** (UTF-8 có BOM, mở trực tiếp bằng Excel):
  - Khung chương trình: `course.csv`, `nodes.csv`, `blocks.csv`, `flashcards.csv`, `resources.csv`
  - Đánh giá (tuỳ chọn): `question-bank.csv`, `questions.csv`, `question-options.csv`, `exercises.csv`, `exercise-questions.csv`
- Một **workbook `.xlsx`** cùng tên, mỗi file CSV là một sheet (`Course`, `Nodes`, … `QuestionBank`, `Questions`, `QuestionOptions`, `Exercises`, `ExerciseQuestions`).

Phần đánh giá đi kèm luôn trong một lần import: tạo một `QuestionBank` (theo môn + lớp, gắn khoá học),
các `Question` + `QuestionOption` (đã duyệt), và `Exercise` (đã xuất bản) gắn vào node chương/bài theo `NodeKey`.
Mỗi bài tập có **tier**: `Free` mở cho mọi học sinh, `Standard` / `Premium` yêu cầu gói tương ứng. KNTT có đủ
4 dạng (Practice/Quiz Free, Test Standard, Exam Premium) cho từng chương + 4 đề học kì; CTST/CD nhẹ hơn.

Nạp qua API `api/content/import` (vai trò ContentEditor / AcademicReviewer / SystemAdmin) — xem [mục "Nạp qua API"](#nạp-qua-api) bên dưới. Ghi vào một `CourseVersion` ở trạng thái `Draft`; các cột được đặt tên khớp entity để ánh xạ 1–1.

> **Quy trình đưa lên production** (đăng nhập → import → duyệt → publish, kèm script chạy cả 3 bộ một lệnh): [HUONG-DAN-IMPORT.md](HUONG-DAN-IMPORT.md) · [import-all.sh](import-all.sh).

## Nguồn sinh & cách tạo lại

Các file trong thư mục này là **đầu ra** của bộ sinh [`tools/CurriculumImportBuilder`](../../tools/CurriculumImportBuilder). Nội dung được giữ dạng dữ liệu có cấu trúc trong mã C# (mỗi bài là một chuỗi `Block`), tách theo file:

- `KnttChapters1to3.cs`, `KnttChapters4to6.cs`, `KnttChapters7to9.cs` — nội dung chi tiết KNTT
- `CtstBook.cs`, `CanhDieuBook.cs` — khung CTST / CD

Chạy lại sau khi sửa nội dung:

```bash
cd tools/CurriculumImportBuilder
dotnet run -c Release          # ghi đè docs/content-import/
dotnet run -c Release -- <thư-mục-khác>   # xuất ra nơi khác
```

## Lược đồ file

### `course.csv` — 1 dòng dữ liệu

| Cột | Ý nghĩa | Ánh xạ |
|---|---|---|
| `Slug` | Định danh khoá học, duy nhất | `Course.Slug` |
| `Title` | Tên khoá học | `Course.Title` |
| `SubjectCode` | Mã môn (`MATH`) | tra `Subject.Code` |
| `GradeCode` | Mã lớp (`G6`) | tra `GradeLevel.Code` |
| `FrameworkCode` | Mã bộ sách (`KNTT` / `CTST` / `CD`) | tra / tạo `CurriculumFramework.Code` |
| `FrameworkName` | Tên bộ sách | `CurriculumFramework.Name` |
| `Publisher` | Nhà xuất bản | `CurriculumFramework.Publisher` |
| `ListPrice` | Giá niêm yết (VND) | `Course.ListPrice` |
| `VersionLabel` | Nhãn phiên bản | `CourseVersion.Label` |
| `Description` | Mô tả | `Course.Description` |

### `nodes.csv` — cây nội dung (`ContentNode`)

| Cột | Ý nghĩa |
|---|---|
| `NodeKey` | Khoá cục bộ trong file (vd `c1`, `c1b1`) — dùng để nối quan hệ, **không** phải id thật |
| `ParentKey` | `NodeKey` của node cha; rỗng = node gốc (chương) |
| `NodeType` | `Chapter` hoặc `Lesson` (khớp enum `NodeType`) |
| `Title` | Tiêu đề node |
| `OrderIndex` | Thứ tự trong cùng cấp cha (bắt đầu từ 1) |
| `IsFree` | `true`/`false` — node mở cho khách / học sinh chưa mua |
| `DurationMinutes` | Thời lượng dự kiến của bài học (rỗng với chương) |

`Slug` không có trong file — importer tự sinh từ `Title` (có sẵn hàm `Slugify` trong `DemoContent`/hệ thống). `MaterializedPath`, `Depth` do importer tính khi dựng cây.

### `blocks.csv` — nội dung bài học (`ContentBlock`)

| Cột | Ý nghĩa |
|---|---|
| `NodeKey` | Bài học chứa khối này (trỏ tới `nodes.csv`, luôn là node `Lesson`) |
| `OrderIndex` | Thứ tự khối trong bài (bắt đầu từ 1) |
| `BlockType` | Khớp enum `LessonBlockType`: `Heading`, `Text`, `Definition`, `Example`, `Note`, `Formula`, `Image`, `Video`, `Animation`, `Embed`, `Audio`, `Pdf` |
| `ContentText` | Nội dung Markdown; công thức toán viết bằng LaTeX giữa `$...$`. Với `Image`/`Video`/… có thể để trống |
| `ContentUrl` | URL cho khối media (rỗng với khối văn bản) |
| `MetadataJson` | JSON phụ (vd `{"alt":"...","caption":"..."}` cho ảnh) |

Quy ước nội dung trong bản KNTT:

- `Heading` bắt đầu bằng `#` (tiêu đề bài) hoặc `##` (tiêu đề mục).
- `Definition` mở đầu `**<thuật ngữ>.**`.
- `Example` có dạng `**Ví dụ.** <đề>` + dòng trống + `**Lời giải.** <giải>`.
- `Note` mở đầu `**Chú ý.**`.
- `Formula` là biểu thức LaTeX (không bọc `$`), hiển thị dạng khối.

### `flashcards.csv` — thẻ ghi nhớ (`FlashcardDeck` + `Flashcard`)

| Cột | Ý nghĩa |
|---|---|
| `NodeKey` | Bài học chứa bộ thẻ |
| `DeckTitle` | Tên bộ thẻ — mọi dòng cùng `NodeKey` + `DeckTitle` gộp thành một `FlashcardDeck` |
| `CardOrder` | Thứ tự thẻ trong bộ |
| `FrontText` | Mặt trước (thuật ngữ) |
| `BackText` | Mặt sau (giải nghĩa) |
| `Hint` | Gợi ý (có thể rỗng) |

### `resources.csv` — tài liệu đính kèm (`LessonResource`)

| Cột | Ý nghĩa |
|---|---|
| `NodeKey` | Bài học chứa tài liệu |
| `Title` | Tên tài liệu |
| `ResourceType` | Khớp enum `ResourceType`: `Pdf`, `Slide`, `Doc`, `Sheet`, `ExternalLink` |
| `ExternalUrl` | Liên kết ngoài |
| `IsDownloadable` | `true`/`false` |
| `OrderIndex` | Thứ tự |

Hiện để trống cho cả 3 bộ (chưa có file tài liệu thật).

### `question-bank.csv` — ngân hàng câu hỏi (`QuestionBank`, 1 dòng)

| Cột | Ý nghĩa |
|---|---|
| `BankKey` | Khoá cục bộ, được `questions.csv` / các file khác tham chiếu |
| `BankName` | Tên ngân hàng (≤ 255) |
| `Description` | Mô tả |

Importer tạo `QuestionBank` với `SubjectId` / `GradeLevelId` lấy từ khoá học, `CourseId` = khoá đang import, `IsActive = true`.

### `questions.csv` — câu hỏi (`Question` + `QuestionOption`)

| Cột | Ý nghĩa |
|---|---|
| `QuestionKey` | Khoá cục bộ, duy nhất |
| `BankKey` | Trỏ tới `question-bank.csv` (khớp hoặc bỏ trống) |
| `NodeKey` | (tuỳ chọn) gắn câu hỏi vào một node chương/bài — tạo `QuestionNode` |
| `QuestionType` | `MultipleChoice` / `TrueFalse` / `FillBlank` / `Essay` |
| `Difficulty` | `Easy` / `Medium` / `Hard` (mặc định `Medium`) |
| `QuestionText` | Nội dung câu hỏi (LaTeX giữa `$...$`) |
| `CorrectAnswer` | Đáp án. `FillBlank`: chuỗi đáp số; `TrueFalse`: `true`/`false`; `MultipleChoice`: có thể để trống (suy từ phương án đúng) |
| `Explanation` | Lời giải / giải thích |

Câu hỏi được tạo ở trạng thái `Approved` + `IsActive = true` (dùng được ngay trong bài tập).

### `question-options.csv` — phương án trắc nghiệm (`QuestionOption`)

| Cột | Ý nghĩa |
|---|---|
| `QuestionKey` | Trỏ tới `questions.csv` |
| `OrderIndex` | Thứ tự phương án |
| `OptionText` | Nội dung phương án |
| `IsCorrect` | `true` cho phương án đúng |

Bắt buộc với `MultipleChoice` / `TrueFalse`: ≥ 2 phương án và ≥ 1 phương án đúng.

### `exercises.csv` — bài tập / đề (`Exercise`)

| Cột | Ý nghĩa |
|---|---|
| `ExerciseKey` | Khoá cục bộ, duy nhất |
| `NodeKey` | Node chương/bài mà bài tập gắn vào (trỏ `nodes.csv`) |
| `ExerciseName` | Tên bài tập (≤ 255) |
| `ExerciseType` | `Practice` (ôn luyện) / `Quiz` (bài tập) / `Test` (kiểm tra) / `Exam` (đề thi) |
| `Tier` | `Free` → `IsFree=true`; `Standard` / `Premium` → yêu cầu gói tương ứng |
| `DurationMinutes` | Thời gian làm bài (rỗng = không giới hạn) |
| `MaxAttempts` | Số lượt tối đa (rỗng = không giới hạn) |
| `PassingPercent` | % điểm để đạt (0–100, mặc định 50) — importer tính `PassingScore = TotalScores × %` |

Bài tập được tạo ở trạng thái `Published` + `IsActive = true`.

### `exercise-questions.csv` — câu hỏi trong bài tập (`ExerciseQuestion`)

| Cột | Ý nghĩa |
|---|---|
| `ExerciseKey` | Trỏ `exercises.csv` |
| `QuestionKey` | Trỏ `questions.csv` |
| `Score` | Điểm của câu trong bài (mặc định 1) |

Mỗi bài tập cần ≥ 1 dòng. `TotalScores` = tổng `Score`, `TotalQuestions` = số dòng.

## Nạp qua API

Endpoint: `api/content/import` — controller [`ContentImportController`](../../Controllers/ContentImportController.cs),
service [`ContentImportService`](../../Services/Implementations/ContentImportService.cs),
bộ kiểm tra thuần [`ContentImportParser`](../../Services/Helpers/ContentImportParser.cs).
Quyền: `ContentEditor`, `AcademicReviewer`, `SystemAdmin`. Gửi file dạng `multipart/form-data`
với các phần tên `Course`, `Nodes`, `Blocks`, `Flashcards`, `Resources` (đặt tên file `*.csv`).

| Method + path | Dùng khi | Ghi chú |
|---|---|---|
| `POST /api/content/import/validate` | Kiểm tra bộ file, **không ghi gì** | thêm `?versionId=` để kiểm tra theo ngữ cảnh version |
| `POST /api/content/import/course` | Tạo **Course + CourseVersion (Draft) mới** rồi import | cần `Course`; `?dryRun=true` để chỉ thử |
| `POST /api/content/import/versions/{versionId}` | Import vào **CourseVersion Draft có sẵn** | `?replace=true` thay toàn bộ nội dung cũ; `?dryRun=true` để chỉ thử |
| `GET /api/content/import/jobs` · `GET /api/content/import/jobs/{id}` | Lịch sử import (`ContentImportJob`) | job chi tiết kèm danh sách issue đã lưu |

Kết quả trả về (`ContentImportResultDto`): `Valid`, `Committed`, `Counts` (số chương / bài / block / thẻ …),
`ErrorCount`, `WarningCount` và `Issues[]` — mỗi issue có `File`, `Row` (số dòng dữ liệu, không tính header),
`NodeKey`, `Code`, `Severity` (`Error` chặn import / `Warning` không chặn), `Message`.

Quy trình điển hình cho một bộ sách mới:

```
POST /api/content/import/course?dryRun=true   (sửa file đến khi Valid = true)
POST /api/content/import/course                → tạo Course + Draft v1 + cây nội dung
POST /api/courses/versions/{id}/submit         → InReview
POST /api/courses/versions/{id}/review         → Approve
POST /api/courses/versions/{id}/publish        → Published
```

### Kiểm tra file — các mã lỗi thường gặp

**Chặn import (`Error`):** `MISSING_FILE`, `MISSING_HEADER`, `EMPTY_FILE`, `DUP_NODE_KEY`, `MISSING_NODE_KEY`,
`BAD_NODE_TYPE`, `MISSING_TITLE`, `TITLE_TOO_LONG`, `SLUG_TOO_LONG`, `BAD_ORDER_INDEX`, `BAD_DURATION`,
`PARENT_NOT_FOUND`, `PARENT_CYCLE`, `DEPTH_EXCEEDED`, `NESTING_NOT_ALLOWED` (sai luật `NodeTypeRule` của môn),
`BLOCK_NODE_NOT_FOUND`, `BAD_BLOCK_TYPE`, `BLOCK_TEXT_REQUIRED`, `BLOCK_URL_TOO_LONG`,
`CARD_NODE_NOT_FOUND`, `CARD_TEXT_REQUIRED`, `DECK_TITLE_REQUIRED`,
`RES_NODE_NOT_FOUND`, `BAD_RESOURCE_TYPE`, `RES_TITLE_REQUIRED`, `RES_URL_REQUIRED`, `RES_URL_TOO_LONG`,
`ROW_CAP_EXCEEDED`, `VERSION_NOT_FOUND`, `VERSION_NOT_DRAFT`, `VERSION_NOT_EMPTY`,
`SUBJECT_CODE_NOT_FOUND`, `GRADE_CODE_NOT_FOUND`, `COURSE_SLUG_TAKEN`, `COURSE_SGF_EXISTS`,
`COURSE_SLUG_REQUIRED`, `COURSE_TITLE_REQUIRED`, `BAD_LIST_PRICE`.
Phần đánh giá: `BANK_KEY_REQUIRED`, `BANK_NAME_REQUIRED`, `DUP_QUESTION_KEY`, `BAD_QUESTION_TYPE`,
`QUESTION_TEXT_REQUIRED`, `CORRECT_ANSWER_REQUIRED`, `OPTIONS_TOO_FEW`, `NO_CORRECT_OPTION`,
`OPTION_QUESTION_NOT_FOUND`, `OPTION_TEXT_REQUIRED`, `DUP_EXERCISE_KEY`, `EXERCISE_NAME_REQUIRED`,
`BAD_EXERCISE_TYPE`, `BAD_TIER`, `EXERCISE_NODE_NOT_FOUND`, `BAD_PASSING_PERCENT`,
`EXERCISE_NO_QUESTIONS`, `LINK_EXERCISE_NOT_FOUND`, `LINK_QUESTION_NOT_FOUND`.

**Không chặn (`Warning`):** `SLUG_INVALID`, `BAD_BOOL`, `BLOCK_NODE_NOT_LESSON`, `BLOCK_URL_REQUIRED`,
`BAD_METADATA_JSON`, `FRAMEWORK_WILL_BE_CREATED` (bộ sách mới sẽ được tạo), `COURSE_FILE_IGNORED`,
`COURSE_EXTRA_ROWS`, `BAD_DIFFICULTY`, `QUESTION_NODE_NOT_FOUND`, `QUESTION_BANK_MISMATCH`, `DUP_LINK`.

### Giới hạn

| Khoá `SystemConfig` | Mặc định | Ý nghĩa |
|---|---|---|
| `content.import.maxRowsPerJob` | 5000 | tổng số dòng (nodes + blocks + cards + resources) / lần import |
| `content.maxTreeDepth` | 4 | độ sâu tối đa của cây node |
| — (hằng trong controller) | 10 MB / file, 30 MB / request | kích thước upload |

Điều đã kiểm khi import:

1. `Subject` (`SubjectCode`) và `GradeLevel` (`GradeCode`) **phải có sẵn**; `CurriculumFramework` (`FrameworkCode`) chưa có sẽ được tạo mới.
2. Cây node: khoá duy nhất, cha tồn tại, không vòng lặp, độ sâu trong hạn mức, và **hợp luật lồng nhau `NodeTypeRule`** của môn (mặc định: gốc → `Chapter`, `Chapter` → `Topic`/`Lesson`, …).
3. `Slug`, `MaterializedPath`, `Depth`, `ParentNodeId` do importer tự tính — không cần trong file.
4. Block/flashcard/resource: `NodeKey` phải trỏ tới một node có trong `nodes.csv`; đúng loại enum; đủ trường bắt buộc theo loại.
5. Ghi trong một transaction; mọi lần chạy thật (không `dryRun`) ghi một dòng `ContentImportJob`.
