using static CurriculumImportBuilder.Dsl;

namespace CurriculumImportBuilder;

internal static partial class KnttBook
{
    // =====================================================================
    //  CHƯƠNG IV — MỘT SỐ HÌNH PHẲNG TRONG THỰC TIỄN
    // =====================================================================
    private static Chapter ChapterIV() => new("c4", "Chương IV. Một số hình phẳng trong thực tiễn", IsFree: false, new List<Lesson>
    {
        L("c4b18", "Bài 18. Hình tam giác đều. Hình vuông. Hình lục giác đều", free: false, 45, new[]
        {
            H("Bài 18. Hình tam giác đều. Hình vuông. Hình lục giác đều"),
            T("Các hình này xuất hiện nhiều trong thực tế: biển báo giao thông, gạch lát nền, tổ ong…"),
            D("Hình tam giác đều",
                "có ba cạnh bằng nhau và ba góc bằng nhau, mỗi góc bằng $60^\\circ$."),
            D("Hình vuông",
                "có bốn cạnh bằng nhau, bốn góc vuông, hai đường chéo bằng nhau và vuông góc với nhau."),
            D("Hình lục giác đều",
                "có sáu cạnh bằng nhau và sáu góc bằng nhau; ba đường chéo chính bằng nhau và cắt nhau tại một điểm "
                + "chia mỗi đường chéo thành hai phần bằng nhau."),
            F(@"\text{Tam giác đều: } P=3a;\qquad \text{Hình vuông: } P=4a,\ \ S=a^2"),
            Ex("Một viên gạch hình vuông cạnh 40 cm. Tính chu vi và diện tích viên gạch.",
                "$P=4\\cdot 40=160$ cm; $S=40^2=1600\\ \\text{cm}^2$."),
            N("Lục giác đều được ghép bởi 6 tam giác đều bằng nhau có chung một đỉnh ở tâm."),
        }, new[]
        {
            C("Tam giác đều", "3 cạnh bằng nhau, mỗi góc $60^\\circ$."),
            C("Hình vuông", "4 cạnh bằng nhau, 4 góc vuông."),
            C("Lục giác đều", "6 cạnh bằng nhau, 6 góc bằng nhau."),
            C("Chu vi hình vuông", "$P=4a$; diện tích $S=a^2$."),
        }),

        L("c4b19", "Bài 19. Hình chữ nhật. Hình thoi. Hình bình hành. Hình thang cân", free: false, 45, new[]
        {
            H("Bài 19. Hình chữ nhật. Hình thoi. Hình bình hành. Hình thang cân"),
            D("Hình chữ nhật",
                "có bốn góc vuông; các cạnh đối bằng nhau và song song; hai đường chéo bằng nhau và cắt nhau tại trung điểm mỗi đường."),
            D("Hình thoi",
                "có bốn cạnh bằng nhau; các cạnh đối song song; hai đường chéo vuông góc với nhau và cắt nhau tại trung điểm mỗi đường."),
            D("Hình bình hành",
                "có các cạnh đối bằng nhau và song song; các góc đối bằng nhau; hai đường chéo cắt nhau tại trung điểm mỗi đường."),
            D("Hình thang cân",
                "có hai cạnh đáy song song, hai cạnh bên bằng nhau, hai góc kề một đáy bằng nhau và hai đường chéo bằng nhau."),
            Ex("Hình chữ nhật có hai kích thước 5 cm và 8 cm. Tính độ dài đường chéo bằng cách vẽ và đo, rồi tính chu vi.",
                "Chu vi $P=2(5+8)=26$ cm. Đường chéo đo được xấp xỉ $9{,}4$ cm (bằng $\\sqrt{5^2+8^2}$, học ở lớp trên)."),
            N("Hình vuông vừa là hình chữ nhật, vừa là hình thoi."),
        }, new[]
        {
            C("Hình chữ nhật", "4 góc vuông, hai đường chéo bằng nhau."),
            C("Hình thoi", "4 cạnh bằng nhau, hai đường chéo vuông góc."),
            C("Hình bình hành", "Các cạnh đối song song và bằng nhau."),
            C("Hình thang cân", "Hai cạnh bên bằng nhau, hai đường chéo bằng nhau."),
        }),

        L("c4b20", "Bài 20. Chu vi và diện tích của một số tứ giác đã học", free: false, 45, new[]
        {
            H("Bài 20. Chu vi và diện tích của một số tứ giác đã học"),
            D("Chu vi",
                "của một hình bằng tổng độ dài các cạnh của hình đó."),
            D("Công thức diện tích",
                "Hình chữ nhật: $S=a\\cdot b$. Hình vuông: $S=a^2$. Hình bình hành: $S=a\\cdot h$ ($h$ là chiều cao ứng với cạnh $a$). "
                + "Hình thoi: $S=\\dfrac{1}{2}\\cdot m\\cdot n$ ($m,n$ là hai đường chéo). "
                + "Hình thang: $S=\\dfrac{1}{2}(a+b)\\cdot h$."),
            F(@"S_{\text{cn}}=ab;\quad S_{\text{bh}}=ah;\quad S_{\text{thoi}}=\tfrac12 mn;\quad S_{\text{thang}}=\tfrac12 (a+b)h"),
            Ex("Mảnh vườn hình thang có hai đáy 10 m và 14 m, chiều cao 8 m. Tính diện tích.",
                "$S=\\dfrac{1}{2}(10+14)\\cdot 8=\\dfrac{1}{2}\\cdot 24\\cdot 8=96\\ \\text{m}^2$."),
            Ex("Một mảnh đất hình chữ nhật dài 25 m, rộng 18 m. Tính chu vi và diện tích.",
                "$P=2(25+18)=86$ m; $S=25\\cdot 18=450\\ \\text{m}^2$."),
            N("Luôn đưa các độ dài về cùng một đơn vị trước khi tính; đơn vị diện tích là đơn vị độ dài bình phương."),
        }, new[]
        {
            C("Chu vi", "Tổng độ dài các cạnh."),
            C("Diện tích hình bình hành", "$S=a\\cdot h$."),
            C("Diện tích hình thoi", "$S=\\dfrac{1}{2}\\cdot d_1\\cdot d_2$."),
            C("Diện tích hình thang", "$S=\\dfrac{1}{2}(a+b)\\cdot h$."),
        }),

        Review("c4lt", "Luyện tập chung — Chương IV",
            "Ôn lại: nhận biết và nêu tính chất của tam giác đều, hình vuông, lục giác đều, hình chữ nhật, hình thoi, "
            + "hình bình hành, hình thang cân; công thức chu vi và diện tích."),
        Review("c4cc", "Bài tập cuối chương IV",
            "Tổng hợp toàn chương: vẽ hình theo yêu cầu; tính chu vi, diện tích các hình trong bài toán thực tế "
            + "về mảnh đất, viên gạch, khung tranh, mảnh vườn."),
    });

    // =====================================================================
    //  CHƯƠNG V — TÍNH ĐỐI XỨNG CỦA HÌNH PHẲNG TRONG TỰ NHIÊN
    // =====================================================================
    private static Chapter ChapterV() => new("c5", "Chương V. Tính đối xứng của hình phẳng trong tự nhiên", IsFree: false, new List<Lesson>
    {
        L("c5b21", "Bài 21. Hình có trục đối xứng", free: false, 45, new[]
        {
            H("Bài 21. Hình có trục đối xứng"),
            D("Trục đối xứng",
                "Đường thẳng $d$ là trục đối xứng của hình $H$ nếu khi gấp hình theo đường thẳng $d$ thì hai phần của hình "
                + "chồng khít lên nhau. Khi đó $H$ được gọi là *hình có trục đối xứng*."),
            T("Ví dụ: đoạn thẳng có trục đối xứng là đường trung trực của nó; hình tròn có vô số trục đối xứng; "
                + "hình vuông có 4 trục đối xứng; hình chữ nhật (không phải hình vuông) có 2 trục đối xứng; "
                + "tam giác đều có 3 trục đối xứng."),
            Ex("Trong các chữ cái in hoa $A,\\ B,\\ D,\\ H,\\ N$, chữ nào có trục đối xứng?",
                "$A$ có 1 trục đối xứng (dọc). $B$ và $D$ có 1 trục đối xứng (ngang). $H$ có 2 trục đối xứng. "
                + "$N$ không có trục đối xứng."),
            N("Một hình có thể có một, nhiều, hoặc không có trục đối xứng nào."),
        }, new[]
        {
            C("Trục đối xứng", "Đường gấp làm hai nửa hình chồng khít."),
            C("Hình vuông", "Có 4 trục đối xứng."),
            C("Hình chữ nhật", "Có 2 trục đối xứng."),
            C("Hình tròn", "Có vô số trục đối xứng."),
        }),

        L("c5b22", "Bài 22. Hình có tâm đối xứng", free: false, 45, new[]
        {
            H("Bài 22. Hình có tâm đối xứng"),
            D("Tâm đối xứng",
                "Điểm $O$ là tâm đối xứng của hình $H$ nếu khi quay hình $H$ nửa vòng ($180^\\circ$) quanh điểm $O$ thì "
                + "hình thu được chồng khít với hình ban đầu. Khi đó $H$ được gọi là *hình có tâm đối xứng*."),
            T("Ví dụ: hình bình hành, hình chữ nhật, hình thoi, hình vuông đều có tâm đối xứng là giao điểm hai đường chéo; "
                + "hình tròn có tâm đối xứng là tâm của nó; đoạn thẳng có tâm đối xứng là trung điểm."),
            Ex("Chữ cái in hoa nào trong $S,\\ H,\\ O,\\ X,\\ A$ có tâm đối xứng?",
                "$S,\\ H,\\ O,\\ X$ có tâm đối xứng; $A$ không có tâm đối xứng."),
            N("Một hình vừa có thể có trục đối xứng vừa có tâm đối xứng (ví dụ hình vuông, hình tròn)."),
        }, new[]
        {
            C("Tâm đối xứng", "Điểm quay hình nửa vòng thì hình trùng chính nó."),
            C("Hình bình hành", "Có tâm đối xứng, không có trục đối xứng."),
            C("Đoạn thẳng", "Tâm đối xứng là trung điểm."),
        }),

        L("c5b23", "Bài 23. Vai trò của tính đối xứng trong thế giới tự nhiên", free: false, 45, new[]
        {
            H("Bài 23. Vai trò của tính đối xứng trong thế giới tự nhiên"),
            T("Tính đối xứng có mặt khắp nơi: cánh bướm, bông tuyết, lá cây, tổ ong, hoa văn trang trí, kiến trúc. "
                + "Đối xứng tạo nên sự cân đối, hài hoà và vững chắc."),
            D("Ý nghĩa",
                "Trong tự nhiên, đối xứng giúp sinh vật cân bằng khi di chuyển và phát triển ổn định. "
                + "Trong kĩ thuật và nghệ thuật, đối xứng giúp thiết kế cân đối, tiết kiệm vật liệu và dễ chế tạo."),
            Ex("Bông tuyết thường có mấy trục đối xứng?",
                "Bông tuyết sáu cánh có 6 trục đối xứng và 1 tâm đối xứng."),
            N("Nhiều công trình như đền, tháp, cầu được thiết kế đối xứng để phân bố lực đều và tạo vẻ đẹp cân đối."),
        }, new[]
        {
            C("Đối xứng trong tự nhiên", "Cánh bướm, bông tuyết, lá cây, tổ ong."),
            C("Lợi ích", "Cân đối, hài hoà, vững chắc, tiết kiệm vật liệu."),
        }),

        Review("c5lt", "Luyện tập chung — Chương V",
            "Ôn lại: nhận biết hình có trục đối xứng và tìm các trục đối xứng; nhận biết hình có tâm đối xứng; "
            + "vẽ thêm để một hình trở nên đối xứng."),
        Review("c5cc", "Bài tập cuối chương V",
            "Tổng hợp toàn chương: tìm trục và tâm đối xứng của chữ cái, biển báo, hoa văn; thiết kế hoạ tiết đối xứng đơn giản."),
    });

    // =====================================================================
    //  CHƯƠNG VI — PHÂN SỐ
    // =====================================================================
    private static Chapter ChapterVI() => new("c6", "Chương VI. Phân số", IsFree: false, new List<Lesson>
    {
        L("c6b24", "Bài 24. Mở rộng phân số. Phân số bằng nhau", free: false, 45, new[]
        {
            H("Bài 24. Mở rộng phân số. Phân số bằng nhau"),
            D("Phân số",
                "$\\dfrac{a}{b}$ với $a,b\\in\\mathbb{Z}$ và $b\\ne 0$; $a$ là *tử số*, $b$ là *mẫu số*. "
                + "Mỗi số nguyên $a$ viết được thành phân số $\\dfrac{a}{1}$."),
            D("Hai phân số bằng nhau",
                "$\\dfrac{a}{b}=\\dfrac{c}{d}$ khi và chỉ khi $a\\cdot d=b\\cdot c$ (tích chéo bằng nhau)."),
            F(@"\dfrac{a}{b}=\dfrac{a\cdot m}{b\cdot m}\ (m\ne 0);\qquad \dfrac{a}{b}=\dfrac{a:n}{b:n}\ (n\in\text{ƯC}(a,b))"),
            D("Rút gọn – phân số tối giản",
                "Chia cả tử và mẫu cho một ước chung khác 1. Phân số *tối giản* khi ƯCLN của tử và mẫu (theo giá trị tuyệt đối) bằng 1."),
            Ex("Rút gọn $\\dfrac{-18}{24}$ và so sánh $\\dfrac{3}{4}$ với $\\dfrac{5}{6}$.",
                "$\\dfrac{-18}{24}=\\dfrac{-18:6}{24:6}=\\dfrac{-3}{4}$. Quy đồng: $\\dfrac{3}{4}=\\dfrac{9}{12}$, $\\dfrac{5}{6}=\\dfrac{10}{12}$; "
                + "vì $9<10$ nên $\\dfrac{3}{4}<\\dfrac{5}{6}$."),
            D("So sánh phân số",
                "Đưa về cùng mẫu dương rồi so sánh tử số; hoặc so sánh với các mốc như 0, $\\dfrac{1}{2}$, 1."),
            N("Người ta thường viết phân số với mẫu số dương: $\\dfrac{2}{-5}=\\dfrac{-2}{5}$."),
        }, new[]
        {
            C("Phân số", "$\\dfrac{a}{b}$ với $a,b$ nguyên, $b\\ne0$."),
            C("Bằng nhau", "$\\dfrac{a}{b}=\\dfrac{c}{d}\\iff ad=bc$."),
            C("Tính chất cơ bản", "Nhân/chia cả tử và mẫu cho cùng số khác 0."),
            C("Tối giản", "ƯCLN(|tử|, |mẫu|) $=1$."),
        }),

        L("c6b25", "Bài 25. Phép cộng và phép trừ phân số", free: false, 45, new[]
        {
            H("Bài 25. Phép cộng và phép trừ phân số"),
            D("Cộng, trừ hai phân số cùng mẫu",
                "cộng (trừ) các tử số và giữ nguyên mẫu số."),
            D("Cộng, trừ hai phân số khác mẫu",
                "quy đồng mẫu số rồi cộng (trừ) như trường hợp cùng mẫu."),
            F(@"\dfrac{a}{m}+\dfrac{b}{m}=\dfrac{a+b}{m};\qquad \dfrac{a}{b}+\dfrac{c}{d}=\dfrac{ad+bc}{bd}"),
            D("Số đối của phân số",
                "Số đối của $\\dfrac{a}{b}$ là $-\\dfrac{a}{b}=\\dfrac{-a}{b}$; tổng của một phân số và số đối của nó bằng 0. "
                + "Quy tắc trừ: $\\dfrac{a}{b}-\\dfrac{c}{d}=\\dfrac{a}{b}+\\left(-\\dfrac{c}{d}\\right)$."),
            Ex("Tính $\\dfrac{2}{3}+\\dfrac{-1}{4}$.",
                "MSC $=12$: $\\dfrac{2}{3}=\\dfrac{8}{12}$, $\\dfrac{-1}{4}=\\dfrac{-3}{12}$. Vậy $\\dfrac{8}{12}+\\dfrac{-3}{12}=\\dfrac{5}{12}$."),
            N("Phép cộng phân số có tính giao hoán và kết hợp; nên rút gọn kết quả về phân số tối giản."),
        }, new[]
        {
            C("Cùng mẫu", "Cộng/trừ tử, giữ nguyên mẫu."),
            C("Khác mẫu", "Quy đồng mẫu rồi cộng/trừ."),
            C("Số đối của $\\dfrac{a}{b}$", "$\\dfrac{-a}{b}$."),
            C("Quy tắc trừ", "Cộng với số đối."),
        }),

        L("c6b26", "Bài 26. Phép nhân và phép chia phân số", free: false, 45, new[]
        {
            H("Bài 26. Phép nhân và phép chia phân số"),
            D("Nhân hai phân số",
                "nhân các tử số với nhau và nhân các mẫu số với nhau."),
            D("Phân số nghịch đảo",
                "Phân số nghịch đảo của $\\dfrac{a}{b}$ (với $a,b\\ne 0$) là $\\dfrac{b}{a}$. Tích của một phân số và phân số nghịch đảo của nó bằng 1."),
            D("Chia hai phân số",
                "muốn chia cho một phân số khác 0, ta nhân với phân số nghịch đảo của nó."),
            F(@"\dfrac{a}{b}\cdot\dfrac{c}{d}=\dfrac{ac}{bd};\qquad \dfrac{a}{b}:\dfrac{c}{d}=\dfrac{a}{b}\cdot\dfrac{d}{c}=\dfrac{ad}{bc}"),
            Ex("Tính $\\dfrac{3}{5}\\cdot\\dfrac{10}{9}$ và $\\dfrac{4}{7}:\\dfrac{8}{21}$.",
                "$\\dfrac{3}{5}\\cdot\\dfrac{10}{9}=\\dfrac{30}{45}=\\dfrac{2}{3}$. "
                + "$\\dfrac{4}{7}:\\dfrac{8}{21}=\\dfrac{4}{7}\\cdot\\dfrac{21}{8}=\\dfrac{84}{56}=\\dfrac{3}{2}$."),
            N("Nên rút gọn chéo trước khi nhân để phép tính gọn hơn. Phép nhân phân số có tính giao hoán, kết hợp và phân phối với phép cộng."),
        }, new[]
        {
            C("Nhân phân số", "Tử nhân tử, mẫu nhân mẫu."),
            C("Nghịch đảo của $\\dfrac{a}{b}$", "$\\dfrac{b}{a}$."),
            C("Chia phân số", "Nhân với phân số nghịch đảo."),
        }),

        L("c6b27", "Bài 27. Hai bài toán về phân số", free: false, 45, new[]
        {
            H("Bài 27. Hai bài toán về phân số"),
            D("Bài toán 1 — tìm giá trị phân số của một số",
                "Muốn tìm $\\dfrac{m}{n}$ của số $a$, ta tính $a\\cdot\\dfrac{m}{n}$."),
            D("Bài toán 2 — tìm một số khi biết giá trị phân số của nó",
                "Muốn tìm một số biết $\\dfrac{m}{n}$ của nó bằng $b$, ta tính $b:\\dfrac{m}{n}$."),
            F(@"\text{Giá trị} = a\cdot\dfrac{m}{n};\qquad \text{Số cần tìm} = b:\dfrac{m}{n}"),
            Ex("Một lớp có 40 học sinh, trong đó $\\dfrac{3}{5}$ số học sinh là nữ. Tính số học sinh nữ.",
                "Số học sinh nữ $=40\\cdot\\dfrac{3}{5}=24$ (học sinh)."),
            Ex("$\\dfrac{2}{3}$ quãng đường dài 18 km. Hỏi cả quãng đường dài bao nhiêu?",
                "Cả quãng đường $=18:\\dfrac{2}{3}=18\\cdot\\dfrac{3}{2}=27$ (km)."),
            N("Phân biệt rõ: bài toán 1 cho \"cả\" tìm \"phần\"; bài toán 2 cho \"phần\" tìm \"cả\"."),
        }, new[]
        {
            C("Tìm phân số của một số", "Nhân số đó với phân số."),
            C("Tìm số biết phân số của nó", "Chia giá trị đã cho cho phân số."),
        }),

        Review("c6lt", "Luyện tập chung — Chương VI",
            "Ôn lại: rút gọn, quy đồng, so sánh phân số; cộng, trừ, nhân, chia phân số; số đối và số nghịch đảo."),
        Review("c6cc", "Bài tập cuối chương VI",
            "Tổng hợp toàn chương: tính giá trị biểu thức phân số bằng cách hợp lí; hai bài toán về phân số trong tình huống thực tế."),
    });
}
