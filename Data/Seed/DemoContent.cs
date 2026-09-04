using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Data.Seed
{
    /// <summary>
    /// Static demo content for <see cref="DemoDataSeeder"/> — chapter/lesson skeletons for the
    /// three Toán 6 textbooks and a builder for a lesson's <see cref="ContentBlock"/> stream.
    /// </summary>
    internal static class DemoContent
    {
        public const string CourseKnttSlug = "toan-6-ket-noi-tri-thuc";
        public const string CourseCtstSlug = "toan-6-chan-troi-sang-tao";
        public const string CourseCdSlug = "toan-6-canh-dieu";

        // --- placeholder media (stable URLs so re-seeds don't churn) ---
        public const string ImageUrl = "https://placehold.co/960x540/1f5fae/ffffff?text=ToanHocHay";
        public const string VideoUrl = "https://www.youtube.com/embed/l3F3Ep9F9m0";
        public const string AnimationUrl = "https://www.geogebra.org/material/iframe/id/x9k2m4n7/width/800/height/450";
        public const string EmbedUrl = "https://www.geogebra.org/calculator";
        public const string AudioUrl = "https://cdn.toanhochay.demo/audio/bai-giang-mau.mp3";
        public const string PdfUrl = "https://cdn.toanhochay.demo/pdf/bai-giang-mau.pdf";
        public const string SlideUrl = "https://cdn.toanhochay.demo/slide/bai-giang-mau.pdf";

        /// <summary>Chapter titles per framework code (KNTT / CTST / CD).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Chapters = new Dictionary<string, string[]>
        {
            ["KNTT"] = new[]
            {
                "Tập hợp các số tự nhiên",
                "Tính chia hết trong tập hợp các số tự nhiên",
                "Số nguyên",
                "Một số hình phẳng trong thực tiễn",
                "Tính đối xứng của hình phẳng trong tự nhiên",
                "Phân số",
                "Số thập phân",
                "Những hình hình học cơ bản",
                "Dữ liệu và xác suất thực nghiệm",
            },
            ["CTST"] = new[]
            {
                "Số tự nhiên",
                "Số nguyên",
                "Các hình phẳng trong thực tiễn",
                "Một số yếu tố thống kê",
                "Phân số",
                "Số thập phân",
                "Hình học trực quan. Tính đối xứng",
                "Các hình hình học cơ bản",
                "Một số yếu tố xác suất",
            },
            ["CD"] = new[]
            {
                "Số tự nhiên",
                "Số nguyên",
                "Hình học trực quan",
                "Một số yếu tố thống kê và xác suất",
                "Phân số và số thập phân",
                "Hình học phẳng",
            },
        };

        /// <summary>Lesson titles per KNTT chapter (index-aligned with <see cref="Chapters"/>["KNTT"]).</summary>
        public static readonly string[][] KnttLessons =
        {
            new[] { "Tập hợp và phần tử của tập hợp", "Cách ghi số tự nhiên", "Phép cộng và phép nhân số tự nhiên", "Luỹ thừa với số mũ tự nhiên" },
            new[] { "Quan hệ chia hết và tính chất", "Dấu hiệu chia hết cho 2, 5, 3, 9", "Số nguyên tố và hợp số", "Ước chung lớn nhất và bội chung nhỏ nhất" },
            new[] { "Tập hợp các số nguyên", "Thứ tự trong tập hợp số nguyên", "Cộng và trừ hai số nguyên", "Nhân và chia hai số nguyên" },
            new[] { "Tam giác đều, hình vuông, hình lục giác đều", "Hình chữ nhật, hình thoi, hình bình hành, hình thang cân", "Chu vi và diện tích một số hình trong thực tiễn" },
            new[] { "Hình có trục đối xứng", "Hình có tâm đối xứng", "Đối xứng trong thế giới tự nhiên" },
            new[] { "Mở rộng khái niệm phân số. Phân số bằng nhau", "So sánh phân số. Hỗn số dương", "Phép cộng và phép trừ phân số", "Phép nhân và phép chia phân số" },
            new[] { "Số thập phân", "Tính toán với số thập phân", "Làm tròn và ước lượng", "Tỉ số và tỉ số phần trăm" },
            new[] { "Điểm và đường thẳng", "Tia. Đoạn thẳng. Độ dài đoạn thẳng", "Trung điểm của đoạn thẳng", "Góc. Số đo góc" },
            new[] { "Thu thập và phân loại dữ liệu", "Biểu diễn dữ liệu trên bảng", "Biểu đồ cột và biểu đồ cột kép", "Kết quả có thể và xác suất thực nghiệm" },
        };

        /// <summary>One or two lesson titles per chapter for CTST and CD (lighter demo).</summary>
        public static string[] LightLessons(string chapterTitle) => new[]
        {
            $"Giới thiệu: {chapterTitle}",
            $"Luyện tập chương: {chapterTitle}",
        };

        // ---------------------------------------------------------------
        //  Lesson block stream
        // ---------------------------------------------------------------

        /// <summary>
        /// Builds the ordered <see cref="ContentBlock"/> list for a lesson.
        /// <paramref name="showcase"/> lessons carry all 12 <see cref="LessonBlockType"/> values;
        /// regular lessons carry a shorter theory-only stream.
        /// </summary>
        public static List<ContentBlock> BuildBlocks(int chapterNumber, string chapterTitle, string lessonTitle, bool showcase)
        {
            var s = ChapterSample(chapterNumber);
            var blocks = new List<ContentBlock>();
            int order = 0;

            void Add(LessonBlockType type, string? text = null, string? url = null, string? meta = null)
                => blocks.Add(new ContentBlock
                {
                    BlockType = type,
                    ContentText = text,
                    ContentUrl = url,
                    MetadataJson = meta,
                    OrderIndex = order++,
                });

            Add(LessonBlockType.Heading, $"# {lessonTitle}");
            Add(LessonBlockType.Text,
                $"Bài học thuộc chương **{chapterTitle}** (Toán 6). Trong bài này chúng ta tìm hiểu " +
                $"{lessonTitle.ToLowerInvariant()} thông qua ví dụ thực tế và luyện tập.");
            Add(LessonBlockType.Definition, $"**Định nghĩa.** {s.Definition}");
            Add(LessonBlockType.Formula, s.Formula);
            Add(LessonBlockType.Example, $"**Ví dụ.** {s.Example}");
            Add(LessonBlockType.Note, $"**Lưu ý.** {s.Note}");

            if (showcase)
            {
                Add(LessonBlockType.Image, null, ImageUrl,
                    $"{{\"alt\":\"Minh hoạ {lessonTitle}\",\"caption\":\"Hình minh hoạ cho {chapterTitle}\"}}");
                Add(LessonBlockType.Video, null, VideoUrl,
                    "{\"provider\":\"youtube\",\"durationSeconds\":540,\"title\":\"Bài giảng video\"}");
                Add(LessonBlockType.Animation, null, AnimationUrl,
                    "{\"tool\":\"geogebra\",\"autoplay\":false,\"title\":\"Mô phỏng tương tác\"}");
                Add(LessonBlockType.Embed, null, EmbedUrl,
                    "{\"provider\":\"geogebra\",\"title\":\"Máy tính GeoGebra\"}");
                Add(LessonBlockType.Audio, null, AudioUrl,
                    "{\"durationSeconds\":210,\"title\":\"Nghe giảng\"}");
                Add(LessonBlockType.Pdf, null, PdfUrl,
                    "{\"pages\":4,\"title\":\"Tóm tắt lý thuyết (PDF)\"}");
            }

            Add(LessonBlockType.Text, "**Bài tập tự luyện.** Hãy hoàn thành các câu hỏi trong phần luyện tập của bài học này.");
            return blocks;
        }

        private readonly record struct Sample(string Definition, string Formula, string Example, string Note);

        private static Sample ChapterSample(int chapter) => chapter switch
        {
            1 => new(
                "Tập hợp là một nhóm các đối tượng xác định; mỗi đối tượng là một phần tử của tập hợp.",
                "$A = \\{0;\\,1;\\,2;\\,3;\\,4\\}, \\quad 3 \\in A, \\quad 7 \\notin A$",
                "Cho $A$ là tập các số tự nhiên nhỏ hơn 5. Khi đó $A = \\{0; 1; 2; 3; 4\\}$ và $A$ có 5 phần tử.",
                "Số 0 là số tự nhiên nhỏ nhất; không có số tự nhiên lớn nhất."),
            2 => new(
                "Số $a$ chia hết cho số $b$ khác 0 nếu có số tự nhiên $q$ sao cho $a = b \\cdot q$.",
                "$a \\;\\vdots\\; b \\iff a = b \\cdot q \\ (q \\in \\mathbb{N})$",
                "$36 = 4 \\cdot 9$ nên $36 \\;\\vdots\\; 4$ và $36 \\;\\vdots\\; 9$. Ta có $\\gcd(36,24)=12$, $\\operatorname{lcm}(4,6)=12$.",
                "Một số chia hết cho 3 khi tổng các chữ số của nó chia hết cho 3."),
            3 => new(
                "Số nguyên gồm các số nguyên âm, số 0 và các số nguyên dương.",
                "$\\mathbb{Z} = \\{\\ldots; -2; -1; 0; 1; 2; \\ldots\\}, \\quad (-5) + 3 = -2$",
                "Nhiệt độ buổi sáng là $-3^\\circ C$, đến trưa tăng thêm $5^\\circ C$: $(-3) + 5 = 2\\,(^\\circ C)$.",
                "Tích của hai số nguyên khác dấu là một số nguyên âm."),
            4 => new(
                "Hình chữ nhật có bốn góc vuông; hình vuông là hình chữ nhật có bốn cạnh bằng nhau.",
                "$P_{\\text{cn}} = 2(a+b), \\qquad S_{\\text{cn}} = a \\cdot b, \\qquad S_{\\text{vuông}} = a^2$",
                "Mảnh đất hình chữ nhật dài 8 m, rộng 5 m có chu vi $2(8+5)=26$ m và diện tích $8 \\cdot 5 = 40\\,\\text{m}^2$.",
                "Diện tích hình thoi bằng nửa tích hai đường chéo."),
            5 => new(
                "Một hình có trục đối xứng nếu có đường thẳng chia hình thành hai phần chồng khít lên nhau khi gấp lại.",
                "Hình vuông có 4 trục đối xứng; hình tròn có vô số trục đối xứng.",
                "Chữ cái in hoa A có 1 trục đối xứng; chữ H có 2 trục đối xứng.",
                "Hình có tâm đối xứng thì khi quay nửa vòng quanh tâm sẽ trùng với chính nó."),
            6 => new(
                "Phân số $\\dfrac{a}{b}$ với $a, b \\in \\mathbb{Z}, b \\neq 0$; $a$ là tử số, $b$ là mẫu số.",
                "$\\dfrac{a}{b} = \\dfrac{a \\cdot m}{b \\cdot m}, \\qquad \\dfrac{a}{b} + \\dfrac{c}{d} = \\dfrac{ad + bc}{bd}$",
                "$\\dfrac{2}{3} + \\dfrac{1}{4} = \\dfrac{8}{12} + \\dfrac{3}{12} = \\dfrac{11}{12}$.",
                "Rút gọn phân số bằng cách chia cả tử và mẫu cho ước chung lớn nhất của chúng."),
            7 => new(
                "Số thập phân gồm phần nguyên và phần thập phân, ngăn cách bởi dấu phẩy.",
                "$3{,}25 = 3 + \\dfrac{2}{10} + \\dfrac{5}{100}, \\qquad \\text{tỉ số phần trăm}: \\dfrac{a}{b} \\cdot 100\\%$",
                "$12{,}5\\%$ của 80 là $80 \\cdot \\dfrac{12{,}5}{100} = 10$.",
                "Khi làm tròn đến hàng phần mười, ta nhìn vào chữ số hàng phần trăm."),
            8 => new(
                "Qua hai điểm phân biệt có một và chỉ một đường thẳng.",
                "$M$ là trung điểm của $AB \\iff MA = MB = \\dfrac{AB}{2}$",
                "Nếu $AB = 6$ cm và $M$ là trung điểm thì $MA = MB = 3$ cm.",
                "Góc vuông có số đo $90^\\circ$; góc bẹt có số đo $180^\\circ$."),
            9 => new(
                "Dữ liệu là các thông tin thu thập được; có thể là số liệu hoặc không phải số liệu.",
                "$\\text{Xác suất thực nghiệm} = \\dfrac{\\text{số lần xảy ra sự kiện}}{\\text{tổng số lần thử}}$",
                "Tung một đồng xu 20 lần được 12 lần mặt ngửa: xác suất thực nghiệm của mặt ngửa là $\\dfrac{12}{20} = 0{,}6$.",
                "Biểu đồ cột kép dùng để so sánh hai bộ dữ liệu trên cùng một biểu đồ."),
            _ => new(
                "Kiến thức nền tảng của chương.",
                "$a + b = b + a$",
                "Ví dụ minh hoạ cơ bản.",
                "Ghi nhớ các tính chất đã học."),
        };

        // ---------------------------------------------------------------
        //  Slug helper (Vietnamese-aware)
        // ---------------------------------------------------------------
        public static string Slugify(string value)
        {
            var lower = value.ToLowerInvariant().Replace('đ', 'd');
            var decomposed = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var ascii = sb.ToString().Normalize(NormalizationForm.FormC);
            ascii = Regex.Replace(ascii, "[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrEmpty(ascii) ? "muc" : ascii;
        }
    }
}
