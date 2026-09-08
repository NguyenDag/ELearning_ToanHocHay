using static CurriculumImportBuilder.Dsl;

namespace CurriculumImportBuilder;

internal static partial class KnttBook
{
    // =====================================================================
    //  CHƯƠNG VII — SỐ THẬP PHÂN
    // =====================================================================
    private static Chapter ChapterVII() => new("c7", "Chương VII. Số thập phân", IsFree: false, new List<Lesson>
    {
        L("c7b28", "Bài 28. Số thập phân", free: false, 45, new[]
        {
            H("Bài 28. Số thập phân"),
            D("Số thập phân",
                "gồm phần số nguyên viết bên trái dấu phẩy và phần thập phân viết bên phải dấu phẩy. "
                + "Mỗi phân số thập phân (mẫu là $10;100;1000;\\dots$) đều viết được dưới dạng số thập phân và ngược lại."),
            F(@"\dfrac{-325}{100}=-3{,}25;\qquad 0{,}7=\dfrac{7}{10};\qquad -3{,}25=-\Big(3+\dfrac{2}{10}+\dfrac{5}{100}\Big)"),
            D("Số thập phân âm và số đối",
                "Số thập phân âm nhỏ hơn 0. Số đối của $3{,}25$ là $-3{,}25$."),
            D("So sánh hai số thập phân",
                "Số thập phân âm luôn nhỏ hơn số thập phân dương. Với hai số dương: so sánh phần nguyên trước, "
                + "phần nguyên bằng nhau thì so sánh lần lượt các chữ số ở từng hàng thập phân. Với hai số âm thì ngược lại."),
            Ex("So sánh $-2{,}17$ và $-2{,}9$.",
                "Xét hai số đối $2{,}17$ và $2{,}9$: $2{,}17<2{,}9$ nên $-2{,}17>-2{,}9$."),
            N("Thêm hoặc bớt các chữ số 0 ở tận cùng bên phải phần thập phân không làm thay đổi giá trị: $1{,}5=1{,}50$."),
        }, new[]
        {
            C("Số thập phân", "Phần nguyên, dấu phẩy, phần thập phân."),
            C("$0{,}7$", "$=\\dfrac{7}{10}$."),
            C("So sánh", "Âm < dương; cùng dấu thì so từng hàng."),
            C("Số đối của $a$", "Số thập phân trái dấu, cùng độ lớn."),
        }),

        L("c7b29", "Bài 29. Tính toán với số thập phân", free: false, 45, new[]
        {
            H("Bài 29. Tính toán với số thập phân"),
            D("Cộng, trừ số thập phân",
                "đặt tính sao cho các dấu phẩy thẳng cột, rồi cộng (trừ) như với số tự nhiên và đặt dấu phẩy ở kết quả "
                + "thẳng cột với các dấu phẩy đã cho. Với số âm, áp dụng quy tắc dấu như với số nguyên."),
            D("Nhân, chia số thập phân",
                "Nhân: bỏ dấu phẩy để nhân như số tự nhiên, rồi đếm tổng số chữ số ở phần thập phân của các thừa số để đặt dấu phẩy. "
                + "Chia: chuyển phép chia về chia cho số tự nhiên bằng cách nhân cả số bị chia và số chia với $10;100;\\dots$"),
            F(@"a\cdot(b+c)=ab+ac;\qquad (-a)\cdot b=-(ab);\qquad (-a)\cdot(-b)=ab"),
            Ex("Tính $3{,}6\\cdot 2{,}5$ và $(-7{,}2):0{,}9$.",
                "$3{,}6\\cdot 2{,}5=9{,}00=9$. $(-7{,}2):0{,}9=(-72):9=-8$."),
            Ex("Tính hợp lí $4{,}7+(-3{,}2)+5{,}3+(-6{,}8)$.",
                "$=(4{,}7+5{,}3)+\\big[(-3{,}2)+(-6{,}8)\\big]=10+(-10)=0$."),
            N("Các tính chất giao hoán, kết hợp, phân phối vẫn đúng với số thập phân — dùng để tính nhanh."),
        }, new[]
        {
            C("Cộng, trừ", "Đặt dấu phẩy thẳng cột."),
            C("Nhân", "Nhân như số tự nhiên rồi đếm chữ số thập phân."),
            C("Chia cho số thập phân", "Nhân cả hai với $10;100;\\dots$ để mẫu thành số tự nhiên."),
        }),

        L("c7b30", "Bài 30. Làm tròn và ước lượng", free: false, 45, new[]
        {
            H("Bài 30. Làm tròn và ước lượng"),
            D("Quy tắc làm tròn số thập phân",
                "Xác định hàng cần làm tròn. Nhìn chữ số ngay bên phải hàng đó: nếu nó $\\ge 5$ thì tăng chữ số ở hàng làm tròn "
                + "thêm 1; nếu nó $<5$ thì giữ nguyên. Các chữ số bên phải hàng làm tròn bị bỏ đi (hoặc thay bằng 0)."),
            Ex("Làm tròn $3{,}14159$ đến hàng phần trăm và đến hàng phần mười.",
                "Đến hàng phần trăm: chữ số hàng phần nghìn là 1 $(<5)$ nên được $3{,}14$. "
                + "Đến hàng phần mười: chữ số hàng phần trăm là 4 $(<5)$ nên được $3{,}1$."),
            D("Ước lượng kết quả",
                "làm tròn các số trong phép tính đến hàng thích hợp rồi tính nhẩm để dự đoán nhanh kết quả, "
                + "giúp kiểm tra kết quả tính có hợp lí không."),
            Ex("Ước lượng $19{,}8\\cdot 5{,}1$.",
                "$19{,}8\\approx 20$; $5{,}1\\approx 5$; $20\\cdot 5=100$. Kết quả thực tế $\\approx 100{,}98$ — phù hợp."),
            N("Kết quả làm tròn được viết với dấu \"$\\approx$\" (xấp xỉ). Trong đo đạc, thường làm tròn theo độ chính xác của dụng cụ."),
        }, new[]
        {
            C("Chữ số $\\ge 5$", "Làm tròn lên."),
            C("Chữ số $<5$", "Làm tròn xuống (giữ nguyên)."),
            C("Ước lượng", "Làm tròn rồi tính nhẩm để kiểm tra."),
            C("Kí hiệu", "$\\approx$ nghĩa là \"xấp xỉ\"."),
        }),

        L("c7b31", "Bài 31. Một số bài toán về tỉ số và tỉ số phần trăm", free: false, 45, new[]
        {
            H("Bài 31. Một số bài toán về tỉ số và tỉ số phần trăm"),
            D("Tỉ số",
                "Tỉ số của $a$ và $b$ (với $b\\ne 0$) là thương $a:b$, viết là $\\dfrac{a}{b}$ hoặc $a:b$."),
            D("Tỉ số phần trăm",
                "Tỉ số phần trăm của $a$ và $b$ là $\\dfrac{a}{b}\\cdot 100\\%$."),
            F(@"\text{Tỉ số phần trăm của }a\text{ và }b=\dfrac{a}{b}\cdot 100\%"),
            Ex("Một lớp có 40 học sinh, trong đó 22 học sinh nữ. Tính tỉ số phần trăm học sinh nữ.",
                "$\\dfrac{22}{40}\\cdot 100\\%=55\\%$."),
            Ex("Một chiếc áo giá 250 000 đồng được giảm giá $12\\%$. Tính số tiền phải trả.",
                "Số tiền giảm $=250\\,000\\cdot\\dfrac{12}{100}=30\\,000$ đồng. Phải trả $250\\,000-30\\,000=220\\,000$ đồng."),
            N("Tìm $p\\%$ của $a$: tính $a\\cdot\\dfrac{p}{100}$. Tìm $a$ khi biết $p\\%$ của nó là $b$: tính $b:\\dfrac{p}{100}$."),
        }, new[]
        {
            C("Tỉ số của $a$ và $b$", "$a:b=\\dfrac{a}{b}$."),
            C("Tỉ số phần trăm", "$\\dfrac{a}{b}\\cdot100\\%$."),
            C("$p\\%$ của $a$", "$a\\cdot\\dfrac{p}{100}$."),
        }),

        Review("c7lt", "Luyện tập chung — Chương VII",
            "Ôn lại: đọc, viết, so sánh số thập phân; bốn phép tính với số thập phân và tính nhanh; làm tròn và ước lượng."),
        Review("c7cc", "Bài tập cuối chương VII",
            "Tổng hợp toàn chương: bài toán thực tế về tiền, đo lường, phần trăm giảm giá, lãi suất, tỉ lệ."),
    });

    // =====================================================================
    //  CHƯƠNG VIII — NHỮNG HÌNH HÌNH HỌC CƠ BẢN
    // =====================================================================
    private static Chapter ChapterVIII() => new("c8", "Chương VIII. Những hình hình học cơ bản", IsFree: false, new List<Lesson>
    {
        L("c8b32", "Bài 32. Điểm và đường thẳng", free: false, 45, new[]
        {
            H("Bài 32. Điểm và đường thẳng"),
            D("Điểm và đường thẳng",
                "*Điểm* và *đường thẳng* là những hình cơ bản, không định nghĩa. Điểm được đặt tên bằng chữ in hoa "
                + "($A,B,C,\\dots$); đường thẳng bằng chữ in thường ($a,b,c,\\dots$) hoặc bằng tên hai điểm thuộc nó."),
            D("Quan hệ thuộc",
                "Nếu điểm $A$ nằm trên đường thẳng $d$, ta viết $A\\in d$ và nói $d$ đi qua $A$. "
                + "Qua hai điểm phân biệt có một và chỉ một đường thẳng."),
            D("Ba điểm thẳng hàng",
                "là ba điểm cùng thuộc một đường thẳng. Trong ba điểm thẳng hàng, có một và chỉ một điểm nằm giữa hai điểm còn lại."),
            Ex("Cho ba điểm $M,N,P$ thẳng hàng, $N$ nằm giữa $M$ và $P$. Kể tên các tia và đoạn thẳng tạo thành.",
                "Đoạn thẳng: $MN,\\ NP,\\ MP$. Trên đường thẳng đó có các tia $NM$ và $NP$ đối nhau."),
            D("Hai đường thẳng cắt nhau, song song, trùng nhau",
                "Hai đường thẳng phân biệt hoặc có đúng một điểm chung (cắt nhau), hoặc không có điểm chung nào (song song)."),
            N("Đường thẳng không bị giới hạn về hai phía."),
        }, new[]
        {
            C("Qua 2 điểm phân biệt", "Có duy nhất một đường thẳng."),
            C("$A\\in d$", "Điểm $A$ thuộc đường thẳng $d$."),
            C("Thẳng hàng", "Các điểm cùng thuộc một đường thẳng."),
            C("Hai đường thẳng song song", "Không có điểm chung."),
        }),

        L("c8b33", "Bài 33. Điểm nằm giữa hai điểm. Tia", free: false, 45, new[]
        {
            H("Bài 33. Điểm nằm giữa hai điểm. Tia"),
            D("Tia",
                "Hình gồm điểm $O$ và một phần đường thẳng bị chia ra bởi $O$ được gọi là một *tia gốc $O$*."),
            D("Hai tia đối nhau",
                "Hai tia chung gốc và tạo thành một đường thẳng gọi là hai tia đối nhau. Mỗi điểm trên đường thẳng "
                + "là gốc chung của hai tia đối nhau."),
            D("Hai tia trùng nhau",
                "Tia $Ox$ và tia $Oy$ trùng nhau nếu chúng chung gốc và có thêm một điểm chung khác gốc."),
            Ex("Trên đường thẳng $xy$ lấy điểm $O$. Lấy $A$ thuộc tia $Ox$, $B$ thuộc tia $Oy$. Điểm nào nằm giữa?",
                "Vì $A$ và $B$ thuộc hai tia đối nhau gốc $O$ nên $O$ nằm giữa $A$ và $B$."),
            N("Khi đọc tên tia, gốc luôn được đọc trước: \"tia $Ox$\"."),
        }, new[]
        {
            C("Tia gốc $O$", "Điểm $O$ và một phần đường thẳng do $O$ chia ra."),
            C("Hai tia đối nhau", "Chung gốc, hợp thành một đường thẳng."),
            C("Hai tia trùng nhau", "Chung gốc và một điểm chung khác gốc."),
        }),

        L("c8b34", "Bài 34. Đoạn thẳng. Độ dài đoạn thẳng", free: false, 45, new[]
        {
            H("Bài 34. Đoạn thẳng. Độ dài đoạn thẳng"),
            D("Đoạn thẳng $AB$",
                "là hình gồm hai điểm $A$, $B$ và tất cả các điểm nằm giữa $A$ và $B$. $A$, $B$ là hai *mút*."),
            D("Độ dài đoạn thẳng",
                "Mỗi đoạn thẳng có một độ dài là một số dương. Hai đoạn thẳng bằng nhau nếu có cùng độ dài."),
            D("Cộng độ dài",
                "Nếu điểm $M$ nằm giữa hai điểm $A$ và $B$ thì $AM+MB=AB$. Ngược lại, nếu $AM+MB=AB$ thì $M$ nằm giữa $A$ và $B$."),
            F(@"M\text{ nằm giữa }A,B \iff AM+MB=AB"),
            Ex("Cho $AB=7$ cm, điểm $M$ nằm giữa $A$ và $B$ với $AM=3$ cm. Tính $MB$.",
                "$MB=AB-AM=7-3=4$ (cm)."),
            N("Muốn so sánh hai đoạn thẳng, ta so sánh độ dài của chúng."),
        }, new[]
        {
            C("Đoạn thẳng $AB$", "Hai mút $A,B$ và mọi điểm nằm giữa."),
            C("Hệ thức cộng", "$M$ giữa $A,B$ $\\iff AM+MB=AB$."),
            C("Hai đoạn thẳng bằng nhau", "Có cùng độ dài."),
        }),

        L("c8b35", "Bài 35. Trung điểm của đoạn thẳng", free: false, 45, new[]
        {
            H("Bài 35. Trung điểm của đoạn thẳng"),
            D("Trung điểm",
                "Trung điểm $M$ của đoạn thẳng $AB$ là điểm nằm giữa $A$, $B$ và cách đều $A$, $B$ (tức $MA=MB$). "
                + "Trung điểm còn được gọi là *điểm chính giữa* của đoạn thẳng."),
            F(@"M\text{ là trung điểm của }AB \iff \big(M\text{ nằm giữa }A,B\ \text{ và }\ MA=MB=\dfrac{AB}{2}\big)"),
            Ex("Đoạn thẳng $AB=10$ cm có trung điểm $I$. Tính $IA$ và $IB$.",
                "$IA=IB=\\dfrac{AB}{2}=\\dfrac{10}{2}=5$ (cm)."),
            D("Cách vẽ trung điểm",
                "Dùng thước có chia vạch: đo $AB$, tính $\\dfrac{AB}{2}$, đặt điểm $M$ trên đoạn $AB$ cách $A$ một khoảng $\\dfrac{AB}{2}$. "
                + "Hoặc gấp giấy sao cho điểm $A$ trùng điểm $B$."),
            N("Mỗi đoạn thẳng chỉ có duy nhất một trung điểm."),
        }, new[]
        {
            C("Trung điểm $M$ của $AB$", "$M$ nằm giữa và $MA=MB$."),
            C("Độ dài", "$MA=MB=\\dfrac{AB}{2}$."),
            C("Số trung điểm", "Mỗi đoạn thẳng có đúng một."),
        }),

        L("c8b36", "Bài 36. Góc", free: false, 45, new[]
        {
            H("Bài 36. Góc"),
            D("Góc",
                "là hình gồm hai tia chung gốc. Gốc chung là *đỉnh*, hai tia là hai *cạnh* của góc. "
                + "Góc có hai cạnh là hai tia đối nhau gọi là *góc bẹt*."),
            T("Kí hiệu góc: $\\widehat{xOy}$, $\\widehat{O}$ hoặc $\\angle xOy$, trong đó $O$ là đỉnh."),
            D("Điểm trong của góc",
                "Với góc $\\widehat{xOy}$ không bẹt, điểm $M$ nằm trong góc nếu tia $OM$ nằm giữa hai tia $Ox$ và $Oy$."),
            Ex("Vẽ hai tia $Oa$, $Ob$. Có mấy góc được tạo thành khi vẽ thêm tia $Oc$ nằm giữa $Oa$ và $Ob$?",
                "Có 3 góc: $\\widehat{aOc}$, $\\widehat{cOb}$ và $\\widehat{aOb}$."),
            N("Khi hai cạnh của góc trùng nhau, ta có \"góc không\" với số đo $0^\\circ$."),
        }, new[]
        {
            C("Góc", "Hình gồm hai tia chung gốc."),
            C("Đỉnh / cạnh", "Gốc chung là đỉnh; hai tia là hai cạnh."),
            C("Góc bẹt", "Hai cạnh là hai tia đối nhau."),
            C("Kí hiệu", "$\\widehat{xOy}$ với đỉnh $O$."),
        }),

        L("c8b37", "Bài 37. Số đo góc", free: false, 45, new[]
        {
            H("Bài 37. Số đo góc"),
            D("Số đo góc",
                "Mỗi góc có một số đo dương, đo bằng thước đo góc, đơn vị là *độ* (kí hiệu $^\\circ$). "
                + "Số đo của góc bẹt là $180^\\circ$; mọi góc có số đo không vượt quá $180^\\circ$."),
            D("Phân loại góc",
                "Góc nhọn: $0^\\circ<\\alpha<90^\\circ$. Góc vuông: $\\alpha=90^\\circ$. Góc tù: $90^\\circ<\\alpha<180^\\circ$. Góc bẹt: $\\alpha=180^\\circ$."),
            F(@"\text{nhọn}<90^\circ;\quad \text{vuông}=90^\circ;\quad 90^\circ<\text{tù}<180^\circ;\quad \text{bẹt}=180^\circ"),
            D("Hai góc bằng nhau",
                "nếu có cùng số đo. So sánh hai góc là so sánh số đo của chúng."),
            Ex("Tia $Oy$ nằm giữa hai tia $Ox$ và $Oz$; $\\widehat{xOy}=40^\\circ$, $\\widehat{yOz}=75^\\circ$. Tính $\\widehat{xOz}$.",
                "$\\widehat{xOz}=\\widehat{xOy}+\\widehat{yOz}=40^\\circ+75^\\circ=115^\\circ$ — là góc tù."),
            N("Nếu tia $Oy$ nằm giữa hai tia $Ox$ và $Oz$ thì $\\widehat{xOy}+\\widehat{yOz}=\\widehat{xOz}$."),
        }, new[]
        {
            C("Đơn vị số đo góc", "Độ, kí hiệu $^\\circ$."),
            C("Góc vuông", "$90^\\circ$."),
            C("Góc tù", "Lớn hơn $90^\\circ$, nhỏ hơn $180^\\circ$."),
            C("Cộng số đo góc", "$Oy$ giữa $Ox,Oz$ thì $\\widehat{xOy}+\\widehat{yOz}=\\widehat{xOz}$."),
        }),

        Review("c8lt", "Luyện tập chung — Chương VIII",
            "Ôn lại: điểm, đường thẳng, tia, đoạn thẳng và hệ thức cộng độ dài; trung điểm; góc và số đo góc."),
        Review("c8cc", "Bài tập cuối chương VIII",
            "Tổng hợp toàn chương: vẽ hình theo diễn đạt; tính độ dài đoạn thẳng và số đo góc; nhận biết trung điểm."),
    });

    // =====================================================================
    //  CHƯƠNG IX — DỮ LIỆU VÀ XÁC SUẤT THỰC NGHIỆM
    // =====================================================================
    private static Chapter ChapterIX() => new("c9", "Chương IX. Dữ liệu và xác suất thực nghiệm", IsFree: false, new List<Lesson>
    {
        L("c9b38", "Bài 38. Dữ liệu và thu thập dữ liệu", free: false, 45, new[]
        {
            H("Bài 38. Dữ liệu và thu thập dữ liệu"),
            D("Dữ liệu",
                "là những thông tin thu thập được về một đối tượng. Dữ liệu có thể là *số* (dữ liệu định lượng) "
                + "hoặc *không phải số* như tên, màu sắc, mức độ (dữ liệu định tính)."),
            D("Thu thập dữ liệu",
                "có thể bằng cách quan sát, làm thí nghiệm, lập phiếu hỏi, phỏng vấn, hoặc tra cứu từ nguồn có sẵn (sách, internet)."),
            Ex("Phân loại các dữ liệu sau: chiều cao các bạn trong tổ; môn thể thao yêu thích; số anh chị em.",
                "Chiều cao và số anh chị em là dữ liệu số; môn thể thao yêu thích là dữ liệu không phải số."),
            D("Tính hợp lí của dữ liệu",
                "Dữ liệu thu thập cần chính xác, đầy đủ và phù hợp với đối tượng. Cần loại bỏ các giá trị bất thường, vô lí."),
            N("Trước khi thu thập, cần xác định rõ: thu thập về vấn đề gì, từ ai và bằng cách nào."),
        }, new[]
        {
            C("Dữ liệu số", "Ví dụ: chiều cao, số học sinh, nhiệt độ."),
            C("Dữ liệu không phải số", "Ví dụ: màu sắc, tên, sở thích."),
            C("Cách thu thập", "Quan sát, thí nghiệm, phiếu hỏi, phỏng vấn, tra cứu."),
        }),

        L("c9b39", "Bài 39. Bảng thống kê và biểu đồ tranh", free: false, 45, new[]
        {
            H("Bài 39. Bảng thống kê và biểu đồ tranh"),
            D("Bảng thống kê",
                "trình bày dữ liệu theo hàng và cột, giúp thấy nhanh số liệu của từng loại và so sánh giữa các loại."),
            D("Biểu đồ tranh",
                "dùng biểu tượng (hình vẽ) để biểu diễn số liệu; mỗi biểu tượng thay cho một số lượng nhất định "
                + "(ghi trong phần chú giải)."),
            Ex("Trong biểu đồ tranh, mỗi $\\bigstar$ ứng với 5 quyển sách. Lớp 6A có 6 ngôi sao. Lớp 6A quyên góp bao nhiêu quyển?",
                "$6\\cdot 5=30$ quyển sách."),
            N("Khi số lượng không chia hết cho giá trị một biểu tượng, ta dùng một phần biểu tượng (ví dụ nửa ngôi sao)."),
        }, new[]
        {
            C("Bảng thống kê", "Dữ liệu sắp theo hàng và cột."),
            C("Biểu đồ tranh", "Mỗi biểu tượng thay cho một số lượng cố định."),
            C("Chú giải", "Cho biết một biểu tượng ứng với bao nhiêu đơn vị."),
        }),

        L("c9b40", "Bài 40. Biểu đồ cột", free: false, 45, new[]
        {
            H("Bài 40. Biểu đồ cột"),
            D("Biểu đồ cột",
                "biểu diễn số liệu bằng các cột hình chữ nhật có cùng chiều rộng, đặt cách đều nhau; chiều cao mỗi cột "
                + "tỉ lệ với số liệu mà nó biểu diễn."),
            D("Cách đọc",
                "Nhìn tên ở trục ngang để biết loại, nhìn vạch ở trục đứng (hoặc số ghi trên đỉnh cột) để biết số liệu; "
                + "cột cao hơn ứng với số liệu lớn hơn."),
            Ex("Biểu đồ cột về số học sinh đăng kí câu lạc bộ: Bóng đá 12, Cờ vua 8, Mĩ thuật 10. Câu lạc bộ nào đông nhất, tổng bao nhiêu?",
                "Bóng đá đông nhất (12). Tổng $12+8+10=30$ học sinh."),
            N("Các bước vẽ: kẻ hai trục; chia đơn vị đều trên trục đứng; vẽ các cột có chiều cao đúng số liệu; ghi tên, số liệu và tiêu đề."),
        }, new[]
        {
            C("Biểu đồ cột", "Chiều cao cột tỉ lệ với số liệu."),
            C("Trục ngang / trục đứng", "Loại đối tượng / giá trị số liệu."),
            C("So sánh", "Cột cao hơn → số liệu lớn hơn."),
        }),

        L("c9b41", "Bài 41. Biểu đồ cột kép", free: false, 45, new[]
        {
            H("Bài 41. Biểu đồ cột kép"),
            D("Biểu đồ cột kép",
                "gồm từng cặp cột đặt sát nhau, dùng để so sánh *hai bộ dữ liệu* cùng loại (ví dụ hai năm, hai lớp, "
                + "nam và nữ). Mỗi bộ dữ liệu được tô một màu và ghi trong phần chú giải."),
            D("Cách đọc",
                "Trong mỗi nhóm, so sánh chiều cao hai cột để thấy chênh lệch giữa hai bộ dữ liệu; so sánh giữa các nhóm "
                + "để thấy xu hướng chung."),
            Ex("Biểu đồ cột kép số cây trồng của lớp 6A và 6B trong 2 tháng: Tháng 3 (6A: 20, 6B: 15); Tháng 4 (6A: 25, 6B: 30). Nhận xét.",
                "Tháng 3 lớp 6A trồng nhiều hơn; tháng 4 lớp 6B vượt lên. Cả hai lớp đều trồng nhiều hơn ở tháng 4."),
            N("Hai cột trong một cặp phải cùng chiều rộng và dùng chung thang đo ở trục đứng."),
        }, new[]
        {
            C("Biểu đồ cột kép", "So sánh hai bộ dữ liệu cùng loại."),
            C("Chú giải màu", "Mỗi màu ứng với một bộ dữ liệu."),
            C("Đọc theo nhóm", "So sánh trong nhóm và giữa các nhóm."),
        }),

        L("c9b42", "Bài 42. Kết quả có thể và sự kiện trong một trò chơi, thí nghiệm", free: false, 45, new[]
        {
            H("Bài 42. Kết quả có thể và sự kiện trong một trò chơi, thí nghiệm"),
            D("Kết quả có thể",
                "Khi thực hiện một phép thử (tung đồng xu, gieo xúc xắc, quay vòng số…), mỗi kết quả có thể xảy ra "
                + "được gọi là một *kết quả có thể*. Tập hợp mọi kết quả có thể xác định trước khi thực hiện."),
            D("Sự kiện",
                "Một *sự kiện* liên quan đến phép thử có thể *xảy ra* hoặc *không xảy ra* tuỳ theo kết quả nhận được."),
            Ex("Gieo một con xúc xắc 6 mặt. Liệt kê các kết quả có thể và cho biết sự kiện \"số chấm là số chẵn\" gồm những kết quả nào.",
                "Kết quả có thể: $1;2;3;4;5;6$. Sự kiện \"số chấm chẵn\" xảy ra khi kết quả là $2;4;6$."),
            N("Trước khi gieo, ta không biết chắc kết quả nào xảy ra, nhưng biết chắc tập hợp các kết quả có thể."),
        }, new[]
        {
            C("Phép thử", "Hành động như tung xu, gieo xúc xắc, quay vòng số."),
            C("Kết quả có thể", "Một khả năng có thể xảy ra của phép thử."),
            C("Sự kiện", "Có thể xảy ra hoặc không, tuỳ kết quả."),
        }),

        L("c9b43", "Bài 43. Xác suất thực nghiệm", free: false, 45, new[]
        {
            H("Bài 43. Xác suất thực nghiệm"),
            D("Xác suất thực nghiệm của một sự kiện",
                "Khi thực hiện một phép thử nhiều lần, xác suất thực nghiệm của sự kiện $A$ bằng tỉ số giữa *số lần "
                + "sự kiện $A$ xảy ra* và *tổng số lần thực hiện phép thử*."),
            F(@"P_{\text{thực nghiệm}}(A)=\dfrac{\text{số lần }A\text{ xảy ra}}{\text{tổng số lần thử}}"),
            Ex("Tung một đồng xu 50 lần, có 27 lần mặt ngửa. Tính xác suất thực nghiệm của sự kiện \"xuất hiện mặt ngửa\".",
                "$P=\\dfrac{27}{50}=0{,}54$."),
            Ex("Một hộp có bi xanh và bi đỏ. Lấy ngẫu nhiên có hoàn lại 40 lần, được 24 lần bi đỏ. Ước lượng xác suất lấy được bi đỏ.",
                "$P\\approx\\dfrac{24}{40}=0{,}6$."),
            N("Số lần thử càng lớn, xác suất thực nghiệm càng gần một giá trị ổn định (xác suất lí thuyết)."),
        }, new[]
        {
            C("Xác suất thực nghiệm", "$\\dfrac{\\text{số lần xảy ra}}{\\text{tổng số lần thử}}$."),
            C("Giá trị", "Là một số từ 0 đến 1."),
            C("Khi tăng số lần thử", "Xác suất thực nghiệm dần ổn định."),
        }),

        Review("c9lt", "Luyện tập chung — Chương IX",
            "Ôn lại: phân loại và thu thập dữ liệu; đọc và vẽ bảng thống kê, biểu đồ tranh, biểu đồ cột, biểu đồ cột kép; "
            + "kết quả có thể, sự kiện và xác suất thực nghiệm."),
        Review("c9cc", "Bài tập cuối chương IX",
            "Tổng hợp toàn chương: từ một bảng dữ liệu thực tế, lập biểu đồ phù hợp, nhận xét và tính xác suất thực nghiệm."),
    });
}
