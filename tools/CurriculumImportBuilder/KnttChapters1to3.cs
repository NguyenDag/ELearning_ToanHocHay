using static CurriculumImportBuilder.Dsl;

namespace CurriculumImportBuilder;

internal static partial class KnttBook
{
    // =====================================================================
    //  CHƯƠNG I — TẬP HỢP CÁC SỐ TỰ NHIÊN   (mở miễn phí)
    // =====================================================================
    private static Chapter ChapterI() => new("c1", "Chương I. Tập hợp các số tự nhiên", IsFree: true, new List<Lesson>
    {
        L("c1b1", "Bài 1. Tập hợp", free: true, 45, new[]
        {
            H("Bài 1. Tập hợp"),
            T("Trong đời sống, ta thường gặp những nhóm đối tượng gộp lại với nhau: bộ sưu tập tem, "
                + "đàn ong trong tổ, các bạn trong tổ học tập… Toán học gọi mỗi nhóm như vậy là một *tập hợp*."),
            D("Tập hợp", "một nhóm các đối tượng được xác định rõ ràng. Mỗi đối tượng trong nhóm là một *phần tử* của tập hợp đó."),
            T("Người ta thường đặt tên tập hợp bằng chữ cái in hoa: $A$, $B$, $M$, … Nếu $x$ là phần tử của tập hợp $A$ "
                + "ta viết $x \\in A$ (đọc: $x$ thuộc $A$); nếu $x$ không phải phần tử của $A$ ta viết $x \\notin A$."),
            D("Hai cách mô tả một tập hợp",
                "**Cách 1** – liệt kê các phần tử: viết tất cả phần tử trong dấu ngoặc nhọn, cách nhau bởi dấu \";\". "
                + "**Cách 2** – chỉ ra tính chất đặc trưng cho các phần tử."),
            F(@"A=\{0;\,1;\,2;\,3;\,4\}\qquad\text{hoặc}\qquad A=\{x\in\mathbb{N}\mid x<5\}"),
            Ex("Viết tập hợp $B$ các số tự nhiên lớn hơn 5 và nhỏ hơn 10 bằng hai cách.",
                "Liệt kê: $B=\\{6;7;8;9\\}$. Nêu tính chất đặc trưng: $B=\\{x\\in\\mathbb{N}\\mid 5<x<10\\}$. "
                + "Tập hợp $B$ có 4 phần tử; $7\\in B$ còn $10\\notin B$."),
            N("Mỗi phần tử chỉ được liệt kê một lần; thứ tự liệt kê không quan trọng. Kí hiệu $\\mathbb{N}$ chỉ tập hợp các số tự nhiên $0;1;2;3;\\dots$"),
        }, new[]
        {
            C("Phần tử", "Mỗi đối tượng thuộc một tập hợp."),
            C(@"$\in$ / $\notin$", "\"thuộc\" / \"không thuộc\"."),
            C("Cách cho tập hợp", "Liệt kê phần tử hoặc nêu tính chất đặc trưng."),
            C(@"$\mathbb{N}$", "Tập hợp các số tự nhiên $0;1;2;3;\\dots$"),
        }),

        L("c1b2", "Bài 2. Cách ghi số tự nhiên", free: true, 45, new[]
        {
            H("Bài 2. Cách ghi số tự nhiên"),
            T("Để ghi các số tự nhiên ta dùng **hệ thập phân**: mười chữ số $0,1,2,3,4,5,6,7,8,9$ và quy tắc "
                + "\"cứ mười đơn vị ở một hàng thì bằng một đơn vị ở hàng liền trước\"."),
            D("Giá trị theo vị trí",
                "Trong một số, mỗi chữ số có một giá trị phụ thuộc vị trí (hàng) của nó: hàng đơn vị, hàng chục, "
                + "hàng trăm, hàng nghìn, …"),
            F(@"\overline{abcd}=a\cdot 1000+b\cdot 100+c\cdot 10+d"),
            Ex("Viết số 3457 thành tổng giá trị các chữ số.",
                "$3457=3\\cdot 1000+4\\cdot 100+5\\cdot 10+7$. Chữ số 4 nằm ở hàng trăm nên có giá trị $400$."),
            D("Số La Mã",
                "Dùng các kí hiệu $I=1$, $V=5$, $X=10$ (cùng $L=50$, $C=100$) ghép theo quy tắc cộng/trừ để viết số. "
                + "Ví dụ $IV=4$, $VI=6$, $XII=12$, $XXVII=27$."),
            N("Khi viết một số tự nhiên có nhiều hơn ba chữ số, người ta thường tách nhóm ba chữ số từ phải sang trái cho dễ đọc: $1\\,234\\,567$."),
        }, new[]
        {
            C("Hệ thập phân", "Hệ ghi số dùng 10 chữ số, mỗi hàng gấp 10 lần hàng sau."),
            C(@"$\overline{ab}$", "Số có hai chữ số $a$ (hàng chục) và $b$ (hàng đơn vị); $\\overline{ab}=10a+b$."),
            C("XXIV", "Số 24 viết bằng số La Mã."),
        }),

        L("c1b3", "Bài 3. Thứ tự trong tập hợp các số tự nhiên", free: true, 45, new[]
        {
            H("Bài 3. Thứ tự trong tập hợp các số tự nhiên"),
            T("Các số tự nhiên được biểu diễn trên tia số: gốc là điểm 0, các điểm cách đều nhau và tăng dần từ trái sang phải."),
            D("So sánh hai số tự nhiên",
                "Trong hai số tự nhiên khác nhau luôn có một số nhỏ hơn số kia. Nếu $a$ nhỏ hơn $b$ ta viết $a<b$ "
                + "(hoặc $b>a$). Trên tia số, điểm biểu diễn số nhỏ nằm bên trái điểm biểu diễn số lớn."),
            F(@"a\le b \iff (a<b \text{ hoặc } a=b);\qquad a<b \text{ và } b<c \Rightarrow a<c"),
            D("Số liền trước – số liền sau",
                "Số liền sau của $a$ là $a+1$; số liền trước của $a$ (với $a\\ge 1$) là $a-1$. Hai số tự nhiên "
                + "liên tiếp hơn kém nhau 1 đơn vị."),
            Ex("Viết các số tự nhiên $x$ thoả mãn $12\\le x<17$.",
                "$x\\in\\{12;13;14;15;16\\}$ — gồm 5 số."),
            N("Số 0 là số tự nhiên nhỏ nhất. Không có số tự nhiên lớn nhất vì mỗi số đều có số liền sau."),
        }, new[]
        {
            C(@"$a \le b$", "$a$ nhỏ hơn hoặc bằng $b$."),
            C("Số liền sau của $a$", "$a+1$."),
            C("Tính chất bắc cầu", "$a<b$ và $b<c$ thì $a<c$."),
        }),

        L("c1b4", "Bài 4. Phép cộng và phép trừ số tự nhiên", free: true, 45, new[]
        {
            H("Bài 4. Phép cộng và phép trừ số tự nhiên"),
            T("Phép cộng hai số tự nhiên $a$ và $b$ cho kết quả là tổng $a+b$. Phép trừ $a-b$ chỉ thực hiện được "
                + "trong tập số tự nhiên khi $a\\ge b$."),
            D("Tính chất của phép cộng",
                "giao hoán, kết hợp và cộng với số 0."),
            F(@"a+b=b+a;\qquad (a+b)+c=a+(b+c);\qquad a+0=0+a=a"),
            Ex("Tính hợp lí: $37+58+63$.",
                "$37+58+63=(37+63)+58=100+58=158$ (dùng tính chất giao hoán và kết hợp để ghép số tròn trăm)."),
            D("Quan hệ giữa phép cộng và phép trừ",
                "nếu $a-b=c$ thì $a=b+c$ và $b=a-c$. Ta dùng quan hệ này để tìm số chưa biết."),
            Ex("Tìm $x$ biết $x-124=237$.",
                "$x=237+124=361$."),
            N("Khi tính một dãy phép cộng và trừ không có dấu ngoặc, ta thực hiện lần lượt từ trái sang phải."),
        }, new[]
        {
            C("Giao hoán (cộng)", "$a+b=b+a$."),
            C("Kết hợp (cộng)", "$(a+b)+c=a+(b+c)$."),
            C("Tìm số hạng chưa biết", "Số hạng = Tổng − số hạng kia."),
            C("Tìm số bị trừ", "Số bị trừ = Hiệu + số trừ."),
        }),

        L("c1b5", "Bài 5. Phép nhân và phép chia số tự nhiên", free: true, 45, new[]
        {
            H("Bài 5. Phép nhân và phép chia số tự nhiên"),
            T("Tích của hai số tự nhiên $a$ và $b$ kí hiệu $a\\cdot b$ (hoặc $ab$). Phép chia có hai loại: chia hết và chia có dư."),
            D("Tính chất của phép nhân",
                "giao hoán, kết hợp, nhân với số 1 và tính chất phân phối của phép nhân đối với phép cộng."),
            F(@"ab=ba;\quad (ab)c=a(bc);\quad a\cdot 1=a;\quad a(b+c)=ab+ac"),
            Ex("Tính nhanh $25\\cdot 12$.",
                "$25\\cdot 12=25\\cdot 4\\cdot 3=100\\cdot 3=300$."),
            D("Phép chia có dư",
                "với $a,b\\in\\mathbb{N}$, $b\\ne 0$, luôn có duy nhất cặp số $q$ (thương) và $r$ (số dư) sao cho "
                + "$a=b\\cdot q+r$ với $0\\le r<b$. Khi $r=0$ ta có phép chia hết."),
            F(@"a=b\cdot q+r\qquad (0\le r<b)"),
            Ex("Thực hiện phép chia $2023:5$.",
                "$2023=5\\cdot 404+3$, vậy thương là 404 và số dư là 3."),
            N("Không có phép chia cho 0. Nếu $a\\cdot b=0$ thì $a=0$ hoặc $b=0$."),
        }, new[]
        {
            C("Phân phối", "$a(b+c)=ab+ac$."),
            C("Phép chia có dư", "$a=bq+r$ với $0\\le r<b$."),
            C("Chia hết", "Phép chia có số dư bằng 0."),
            C("Tìm thừa số chưa biết", "Thừa số = Tích : thừa số kia."),
        }),

        L("c1b6", "Bài 6. Luỹ thừa với số mũ tự nhiên", free: true, 45, new[]
        {
            H("Bài 6. Luỹ thừa với số mũ tự nhiên"),
            T("Khi nhân nhiều thừa số bằng nhau, ta viết gọn bằng luỹ thừa."),
            D("Luỹ thừa",
                "$a^n$ (đọc: $a$ mũ $n$) là tích của $n$ thừa số $a$, với $n\\ne 0$. $a$ gọi là *cơ số*, $n$ gọi là *số mũ*."),
            F(@"a^n=\underbrace{a\cdot a\cdots a}_{n\text{ thừa số}}\quad(n\ne 0);\qquad a^1=a;\qquad a^2\text{ đọc là }a\text{ bình phương}"),
            Ex("Viết gọn $7\\cdot 7\\cdot 7\\cdot 7$ và tính $2^5$.",
                "$7\\cdot 7\\cdot 7\\cdot 7=7^4$. $2^5=2\\cdot2\\cdot2\\cdot2\\cdot2=32$."),
            D("Nhân và chia hai luỹ thừa cùng cơ số",
                "khi nhân, ta giữ nguyên cơ số và cộng các số mũ; khi chia (cơ số khác 0), ta giữ nguyên cơ số và trừ các số mũ."),
            F(@"a^m\cdot a^n=a^{m+n};\qquad a^m:a^n=a^{m-n}\ (a\ne 0,\ m\ge n)"),
            Ex("Viết kết quả dưới dạng một luỹ thừa: $3^4\\cdot 3^3$ và $5^8:5^5$.",
                "$3^4\\cdot 3^3=3^{7}$; $5^8:5^5=5^{3}=125$."),
            N("Quy ước $a^0=1$ với $a\\ne 0$. Luỹ thừa của 10: $10^n$ là số gồm chữ số 1 theo sau bởi $n$ chữ số 0."),
        }, new[]
        {
            C("Cơ số / số mũ", "Trong $a^n$: $a$ là cơ số, $n$ là số mũ."),
            C("Nhân luỹ thừa cùng cơ số", "$a^m\\cdot a^n=a^{m+n}$."),
            C("Chia luỹ thừa cùng cơ số", "$a^m:a^n=a^{m-n}$ $(a\\ne0,\\ m\\ge n)$."),
            C("$a$ bình phương", "$a^2$."),
        }),

        L("c1b7", "Bài 7. Thứ tự thực hiện các phép tính", free: true, 45, new[]
        {
            H("Bài 7. Thứ tự thực hiện các phép tính"),
            T("Một biểu thức có thể chứa nhiều phép tính. Để mọi người tính ra cùng một kết quả, cần thống nhất thứ tự thực hiện."),
            D("Quy tắc (biểu thức không có dấu ngoặc)",
                "Luỹ thừa $\\to$ nhân và chia $\\to$ cộng và trừ. Các phép cùng mức thực hiện từ trái sang phải."),
            D("Quy tắc (biểu thức có dấu ngoặc)",
                "Thực hiện trong ngoặc tròn $(\\ )$ trước, rồi ngoặc vuông $[\\ ]$, rồi ngoặc nhọn $\\{\\ \\}$."),
            F(@"(\ )\ \to\ [\ ]\ \to\ \{\ \}"),
            Ex("Tính $5+3\\cdot 2^3$.",
                "$5+3\\cdot 2^3=5+3\\cdot 8=5+24=29$ (luỹ thừa trước, rồi nhân, rồi cộng)."),
            Ex("Tính $80:\\big\\{[(11-2)\\cdot 2]+2\\big\\}$.",
                "$=80:\\{[9\\cdot 2]+2\\}=80:\\{18+2\\}=80:20=4$ (làm trong ngoặc tròn, rồi ngoặc vuông, rồi ngoặc nhọn)."),
            N("Nếu chỉ có cộng và trừ, hoặc chỉ có nhân và chia, thì luôn thực hiện từ trái sang phải."),
        }, new[]
        {
            C("Thứ tự (không ngoặc)", "Luỹ thừa → nhân, chia → cộng, trừ."),
            C("Thứ tự ngoặc", "$(\\ )$ → $[\\ ]$ → $\\{\\ \\}$."),
            C("Phép cùng mức", "Thực hiện từ trái sang phải."),
        }),

        Review("c1lt", "Luyện tập chung — Chương I",
            "Ôn lại: cách cho tập hợp, ghi số tự nhiên và giá trị theo vị trí, so sánh và thứ tự, "
            + "bốn phép tính cùng tính chất, luỹ thừa và thứ tự thực hiện các phép tính."),
        Review("c1cc", "Bài tập cuối chương I",
            "Tổng hợp toàn chương: tập hợp số tự nhiên $\\mathbb{N}$, $\\mathbb{N}^*$; tính nhanh bằng tính chất "
            + "các phép tính; luỹ thừa; tính giá trị biểu thức; giải bài toán thực tế bằng phép tính."),
    });

    // =====================================================================
    //  CHƯƠNG II — TÍNH CHIA HẾT TRONG TẬP HỢP CÁC SỐ TỰ NHIÊN
    // =====================================================================
    private static Chapter ChapterII() => new("c2", "Chương II. Tính chia hết trong tập hợp các số tự nhiên", IsFree: false, new List<Lesson>
    {
        L("c2b8", "Bài 8. Quan hệ chia hết và tính chất", free: false, 45, new[]
        {
            H("Bài 8. Quan hệ chia hết và tính chất"),
            T("Chương này nghiên cứu khi nào một số chia hết cho số khác và các ứng dụng: ước, bội, số nguyên tố, ƯCLN, BCNN."),
            D("Chia hết",
                "Cho hai số tự nhiên $a$ và $b$ với $b\\ne 0$. Nếu có số tự nhiên $q$ sao cho $a=b\\cdot q$ thì ta nói "
                + "$a$ chia hết cho $b$, kí hiệu $a\\ \\vdots\\ b$. Khi đó $b$ là *ước* của $a$ và $a$ là *bội* của $b$."),
            F(@"a\ \vdots\ b \iff \exists\,q\in\mathbb{N}:\ a=b\cdot q"),
            Ex("Tìm tất cả các ước của 12 và năm bội đầu tiên của 4.",
                "Ư(12) $=\\{1;2;3;4;6;12\\}$. Bội của 4: $0;4;8;12;16;\\dots$"),
            D("Tính chất chia hết của một tổng",
                "Nếu tất cả các số hạng của một tổng đều chia hết cho $m$ thì tổng chia hết cho $m$. Nếu chỉ một số hạng "
                + "không chia hết cho $m$ còn các số hạng khác chia hết cho $m$ thì tổng không chia hết cho $m$."),
            F(@"a\ \vdots\ m\ \text{và}\ b\ \vdots\ m \Rightarrow (a+b)\ \vdots\ m\ \text{và}\ (a-b)\ \vdots\ m"),
            N("Số 0 chia hết cho mọi số tự nhiên khác 0. Mọi số tự nhiên đều chia hết cho 1 và cho chính nó."),
        }, new[]
        {
            C(@"$a\ \vdots\ b$", "$a$ chia hết cho $b$: tồn tại $q$ để $a=bq$."),
            C("Ước / Bội", "Nếu $a\\ \\vdots\\ b$ thì $b$ là ước của $a$, $a$ là bội của $b$."),
            C("Tổng chia hết", "Các số hạng cùng $\\vdots\\ m$ thì tổng $\\vdots\\ m$."),
        }),

        L("c2b9", "Bài 9. Dấu hiệu chia hết", free: false, 45, new[]
        {
            H("Bài 9. Dấu hiệu chia hết"),
            T("Có thể nhận biết một số chia hết cho 2, 5, 3, 9 mà không cần đặt tính chia."),
            D("Dấu hiệu chia hết cho 2 và cho 5",
                "Số chia hết cho 2 khi và chỉ khi chữ số tận cùng là chữ số chẵn ($0;2;4;6;8$). "
                + "Số chia hết cho 5 khi và chỉ khi chữ số tận cùng là $0$ hoặc $5$."),
            D("Dấu hiệu chia hết cho 3 và cho 9",
                "Số chia hết cho 9 khi và chỉ khi tổng các chữ số của nó chia hết cho 9. "
                + "Số chia hết cho 3 khi và chỉ khi tổng các chữ số của nó chia hết cho 3."),
            F(@"\overline{a_1a_2\ldots a_n}\ \vdots\ 9 \iff (a_1+a_2+\cdots+a_n)\ \vdots\ 9"),
            Ex("Trong các số 372; 405; 2130, số nào chia hết cho 3, cho 9, cho cả 2 và 5?",
                "$372$: tổng chữ số $12\\ \\vdots\\ 3$ nên $372\\ \\vdots\\ 3$ (không $\\vdots\\ 9$). "
                + "$405$: tổng $9\\ \\vdots\\ 9$ nên $\\vdots\\ 3$ và $\\vdots\\ 9$. "
                + "$2130$ tận cùng là 0 nên chia hết cho cả 2 và 5; tổng chữ số $6\\ \\vdots\\ 3$."),
            N("Một số chia hết cho 9 thì chắc chắn chia hết cho 3; điều ngược lại không đúng."),
        }, new[]
        {
            C("Chia hết cho 2", "Chữ số tận cùng chẵn."),
            C("Chia hết cho 5", "Chữ số tận cùng là 0 hoặc 5."),
            C("Chia hết cho 9", "Tổng các chữ số chia hết cho 9."),
            C("Chia hết cho 3", "Tổng các chữ số chia hết cho 3."),
        }),

        L("c2b10", "Bài 10. Số nguyên tố", free: false, 45, new[]
        {
            H("Bài 10. Số nguyên tố"),
            D("Số nguyên tố – hợp số",
                "Số nguyên tố là số tự nhiên lớn hơn 1, chỉ có hai ước là 1 và chính nó. Hợp số là số tự nhiên lớn hơn 1 "
                + "và có nhiều hơn hai ước."),
            T("Các số nguyên tố nhỏ hơn 20: $2;3;5;7;11;13;17;19$. Số 2 là số nguyên tố chẵn duy nhất."),
            D("Phân tích một số ra thừa số nguyên tố",
                "là viết số đó thành tích các thừa số nguyên tố. Mỗi hợp số phân tích được duy nhất (không kể thứ tự). "
                + "Có thể dùng *sơ đồ cây* hoặc chia lần lượt cho các số nguyên tố tăng dần."),
            F(@"120=2^3\cdot 3\cdot 5"),
            Ex("Phân tích 84 ra thừa số nguyên tố.",
                "$84=2\\cdot 42=2\\cdot 2\\cdot 21=2^2\\cdot 3\\cdot 7$."),
            N("Số 0 và số 1 không phải số nguyên tố, cũng không phải hợp số."),
        }, new[]
        {
            C("Số nguyên tố", "Số tự nhiên $>1$, chỉ có 2 ước: 1 và chính nó."),
            C("Hợp số", "Số tự nhiên $>1$, có nhiều hơn 2 ước."),
            C("Số nguyên tố chẵn duy nhất", "Số 2."),
            C("Phân tích ra thừa số nguyên tố", "Viết số thành tích các số nguyên tố, ví dụ $120=2^3\\cdot3\\cdot5$."),
        }),

        L("c2b11", "Bài 11. Ước chung. Ước chung lớn nhất", free: false, 45, new[]
        {
            H("Bài 11. Ước chung. Ước chung lớn nhất"),
            D("Ước chung – ƯCLN",
                "Ước chung của hai hay nhiều số là ước của tất cả các số đó. Ước chung lớn nhất (ƯCLN) là số lớn nhất "
                + "trong các ước chung, kí hiệu $\\mathrm{ƯCLN}(a,b)$."),
            D("Cách tìm ƯCLN bằng phân tích ra thừa số nguyên tố",
                "Bước 1: phân tích mỗi số ra thừa số nguyên tố. Bước 2: chọn các thừa số nguyên tố *chung*. "
                + "Bước 3: lập tích các thừa số đó, mỗi thừa số lấy với số mũ *nhỏ nhất*."),
            F(@"36=2^2\cdot 3^2,\quad 60=2^2\cdot 3\cdot 5 \Rightarrow \mathrm{ƯCLN}(36,60)=2^2\cdot 3=12"),
            Ex("Rút gọn phân số $\\dfrac{36}{60}$ về phân số tối giản.",
                "$\\mathrm{ƯCLN}(36,60)=12$ nên $\\dfrac{36}{60}=\\dfrac{36:12}{60:12}=\\dfrac{3}{5}$."),
            D("Hai số nguyên tố cùng nhau",
                "là hai số có ƯCLN bằng 1, ví dụ 8 và 15."),
            N("Ước chung của $a$ và $b$ chính là các ước của $\\mathrm{ƯCLN}(a,b)$."),
        }, new[]
        {
            C("ƯC", "Số là ước của tất cả các số đã cho."),
            C("ƯCLN", "Ước chung lớn nhất."),
            C("Nguyên tố cùng nhau", "ƯCLN bằng 1."),
            C("Dùng để", "Rút gọn phân số về tối giản."),
        }),

        L("c2b12", "Bài 12. Bội chung. Bội chung nhỏ nhất", free: false, 45, new[]
        {
            H("Bài 12. Bội chung. Bội chung nhỏ nhất"),
            D("Bội chung – BCNN",
                "Bội chung của hai hay nhiều số là bội của tất cả các số đó. Bội chung nhỏ nhất (BCNN) là số nhỏ nhất "
                + "khác 0 trong các bội chung, kí hiệu $\\mathrm{BCNN}(a,b)$."),
            D("Cách tìm BCNN bằng phân tích ra thừa số nguyên tố",
                "Bước 1: phân tích mỗi số ra thừa số nguyên tố. Bước 2: chọn các thừa số nguyên tố *chung và riêng*. "
                + "Bước 3: lập tích các thừa số đó, mỗi thừa số lấy với số mũ *lớn nhất*."),
            F(@"8=2^3,\quad 12=2^2\cdot 3 \Rightarrow \mathrm{BCNN}(8,12)=2^3\cdot 3=24"),
            Ex("Quy đồng mẫu hai phân số $\\dfrac{5}{8}$ và $\\dfrac{7}{12}$.",
                "Mẫu chung nhỏ nhất là $\\mathrm{BCNN}(8,12)=24$: $\\dfrac{5}{8}=\\dfrac{15}{24}$, $\\dfrac{7}{12}=\\dfrac{14}{24}$."),
            F(@"\mathrm{ƯCLN}(a,b)\cdot \mathrm{BCNN}(a,b)=a\cdot b"),
            N("BCNN của các số đôi một nguyên tố cùng nhau bằng tích của chúng."),
        }, new[]
        {
            C("BC", "Số là bội của tất cả các số đã cho."),
            C("BCNN", "Bội chung nhỏ nhất (khác 0)."),
            C("Dùng để", "Quy đồng mẫu số nhiều phân số."),
            C("Liên hệ", "$\\mathrm{ƯCLN}(a,b)\\cdot\\mathrm{BCNN}(a,b)=a\\cdot b$."),
        }),

        Review("c2lt", "Luyện tập chung — Chương II",
            "Ôn lại: quan hệ chia hết và tính chất của tổng; các dấu hiệu chia hết cho 2, 5, 3, 9; "
            + "số nguyên tố và phân tích ra thừa số nguyên tố; tìm ƯCLN, BCNN."),
        Review("c2cc", "Bài tập cuối chương II",
            "Tổng hợp toàn chương: vận dụng dấu hiệu chia hết; phân tích ra thừa số nguyên tố; "
            + "dùng ƯCLN, BCNN giải bài toán chia đều, xếp hàng, lịch trùng nhau."),
    });

    // =====================================================================
    //  CHƯƠNG III — SỐ NGUYÊN
    // =====================================================================
    private static Chapter ChapterIII() => new("c3", "Chương III. Số nguyên", IsFree: false, new List<Lesson>
    {
        L("c3b13", "Bài 13. Tập hợp các số nguyên", free: false, 45, new[]
        {
            H("Bài 13. Tập hợp các số nguyên"),
            T("Nhiệt độ dưới $0^\\circ\\text{C}$, độ cao dưới mực nước biển, số tiền nợ… được biểu thị bằng *số nguyên âm* — "
                + "viết bằng dấu \"$-$\" đứng trước một số tự nhiên khác 0."),
            D("Tập hợp số nguyên",
                "gồm các số nguyên âm, số 0 và các số nguyên dương, kí hiệu $\\mathbb{Z}$."),
            F(@"\mathbb{Z}=\{\ldots;-3;-2;-1;0;1;2;3;\ldots\}"),
            D("Thứ tự trong $\\mathbb{Z}$ và số đối",
                "Trên trục số nằm ngang, điểm biểu diễn số nhỏ nằm bên trái. Mọi số nguyên âm đều nhỏ hơn 0 và nhỏ hơn "
                + "mọi số nguyên dương. Số đối của $a$ là $-a$; số đối của $-a$ là $a$."),
            Ex("Sắp xếp các số $-5;\\ 3;\\ 0;\\ -1;\\ 2$ theo thứ tự tăng dần.",
                "$-5<-1<0<2<3$."),
            N("$-(-7)=7$. Số 0 là số đối của chính nó. $|a|$ là khoảng cách từ điểm $a$ đến gốc 0 trên trục số."),
        }, new[]
        {
            C(@"$\mathbb{Z}$", "Tập hợp các số nguyên âm, 0 và nguyên dương."),
            C("Số nguyên âm", "Số viết với dấu \"−\" trước một số tự nhiên khác 0."),
            C("Số đối của $a$", "$-a$; ví dụ số đối của 4 là $-4$."),
            C("$|a|$", "Giá trị tuyệt đối — khoảng cách từ $a$ đến 0."),
        }),

        L("c3b14", "Bài 14. Phép cộng và phép trừ số nguyên", free: false, 45, new[]
        {
            H("Bài 14. Phép cộng và phép trừ số nguyên"),
            D("Cộng hai số nguyên cùng dấu",
                "Cộng hai số nguyên dương như cộng hai số tự nhiên. Cộng hai số nguyên âm: cộng hai giá trị tuyệt đối "
                + "rồi đặt dấu \"$-$\" trước kết quả."),
            F(@"(-a)+(-b)=-(a+b)\quad (a,b>0)"),
            D("Cộng hai số nguyên khác dấu",
                "lấy giá trị tuyệt đối lớn trừ giá trị tuyệt đối nhỏ, rồi đặt trước kết quả dấu của số có giá trị tuyệt đối lớn hơn. "
                + "Hai số đối nhau có tổng bằng 0."),
            Ex("Tính $(-7)+4$ và $(-3)+(-9)$.",
                "$(-7)+4=-(7-4)=-3$. $(-3)+(-9)=-(3+9)=-12$."),
            D("Phép trừ hai số nguyên",
                "Muốn trừ $a$ cho $b$, ta cộng $a$ với số đối của $b$: $a-b=a+(-b)$."),
            Ex("Nhiệt độ đang là $-4^\\circ\\text{C}$, giảm thêm $6^\\circ\\text{C}$. Nhiệt độ mới là bao nhiêu?",
                "$(-4)-6=(-4)+(-6)=-10$. Nhiệt độ mới là $-10^\\circ\\text{C}$."),
            N("Phép cộng số nguyên có tính giao hoán và kết hợp; $a+0=a$; $a+(-a)=0$."),
        }, new[]
        {
            C("Cộng hai số nguyên âm", "$(-a)+(-b)=-(a+b)$."),
            C("Cộng hai số đối nhau", "Bằng 0."),
            C("Quy tắc trừ", "$a-b=a+(-b)$."),
            C("Cộng khác dấu", "Hiệu hai |giá trị|, lấy dấu số có |giá trị| lớn hơn."),
        }),

        L("c3b15", "Bài 15. Quy tắc dấu ngoặc", free: false, 45, new[]
        {
            H("Bài 15. Quy tắc dấu ngoặc"),
            D("Quy tắc dấu ngoặc",
                "Khi bỏ dấu ngoặc có dấu \"$+$\" đứng trước, giữ nguyên dấu các số hạng trong ngoặc. "
                + "Khi bỏ dấu ngoặc có dấu \"$-$\" đứng trước, đổi dấu tất cả các số hạng trong ngoặc."),
            F(@"a+(b-c)=a+b-c;\qquad a-(b-c)=a-b+c"),
            Ex("Tính hợp lí $(-215)+\\big[(-45)+215+45\\big]$.",
                "$=(-215)+(-45)+215+45=\\big[(-215)+215\\big]+\\big[(-45)+45\\big]=0+0=0$."),
            D("Tổng đại số",
                "là dãy các phép cộng và trừ số nguyên. Trong một tổng đại số, ta có thể đổi chỗ các số hạng kèm dấu "
                + "của chúng, hoặc nhóm các số hạng một cách tuỳ ý."),
            N("Nhờ quy tắc dấu ngoặc, ta thường nhóm các số đối nhau hoặc các số tròn chục để tính nhanh."),
        }, new[]
        {
            C("Bỏ ngoặc sau dấu \"+\"", "Giữ nguyên dấu các số hạng."),
            C("Bỏ ngoặc sau dấu \"−\"", "Đổi dấu tất cả các số hạng."),
            C("Tổng đại số", "Dãy phép cộng, trừ; được đổi chỗ và nhóm tuỳ ý (kèm dấu)."),
        }),

        L("c3b16", "Bài 16. Phép nhân số nguyên", free: false, 45, new[]
        {
            H("Bài 16. Phép nhân số nguyên"),
            D("Nhân hai số nguyên khác dấu",
                "nhân hai giá trị tuyệt đối rồi đặt dấu \"$-$\" trước kết quả."),
            D("Nhân hai số nguyên cùng dấu",
                "nhân hai giá trị tuyệt đối; kết quả mang dấu \"$+$\". Đặc biệt, tích hai số nguyên âm là số nguyên dương."),
            F(@"(-)\cdot(+)=(-);\quad (+)\cdot(+)=(+);\quad (-)\cdot(-)=(+)"),
            Ex("Tính $(-6)\\cdot 5$ và $(-4)\\cdot(-7)$.",
                "$(-6)\\cdot 5=-30$. $(-4)\\cdot(-7)=28$."),
            D("Tính chất của phép nhân số nguyên",
                "giao hoán, kết hợp, nhân với 1, và phân phối đối với phép cộng — tương tự trong $\\mathbb{N}$."),
            N("Tích chứa một số chẵn thừa số nguyên âm là số dương; chứa một số lẻ thừa số nguyên âm là số âm. "
                + "$a\\cdot 0=0$."),
        }, new[]
        {
            C("Khác dấu × ", "Kết quả mang dấu \"−\"."),
            C("Cùng dấu ×", "Kết quả mang dấu \"+\"."),
            C("Âm × Âm", "Bằng số dương."),
            C("Đếm dấu âm", "Chẵn thừa số âm → dương; lẻ → âm."),
        }),

        L("c3b17", "Bài 17. Phép chia hết. Ước và bội của một số nguyên", free: false, 45, new[]
        {
            H("Bài 17. Phép chia hết. Ước và bội của một số nguyên"),
            D("Phép chia hết trong $\\mathbb{Z}$",
                "Cho $a,b\\in\\mathbb{Z}$, $b\\ne 0$. Nếu có $q\\in\\mathbb{Z}$ sao cho $a=b\\cdot q$ thì ta nói $a$ chia hết cho $b$."),
            D("Dấu của thương",
                "Quy tắc dấu của phép chia giống phép nhân: cùng dấu cho thương dương, khác dấu cho thương âm."),
            F(@"(-36):(-4)=9;\qquad (-36):4=-9"),
            D("Ước và bội của một số nguyên",
                "Nếu $a\\ \\vdots\\ b$ thì $b$ là ước của $a$, $a$ là bội của $b$. Các ước của một số nguyên gồm cả ước dương và ước âm."),
            Ex("Tìm tất cả các ước của $-8$.",
                "$Ư(-8)=\\{\\pm 1;\\pm 2;\\pm 4;\\pm 8\\}$."),
            N("Nếu $c$ là ước của $a$ và của $b$ thì $c$ cũng là ước của $a+b$ và $a-b$."),
        }, new[]
        {
            C("Chia hết trong $\\mathbb{Z}$", "$a=bq$ với $q$ nguyên."),
            C("Dấu của thương", "Cùng dấu → \"+\", khác dấu → \"−\"."),
            C("Ước của số nguyên", "Gồm cả ước âm, ví dụ $Ư(6)=\\{\\pm1;\\pm2;\\pm3;\\pm6\\}$."),
        }),

        Review("c3lt", "Luyện tập chung — Chương III",
            "Ôn lại: tập $\\mathbb{Z}$, thứ tự và số đối; cộng, trừ số nguyên; quy tắc dấu ngoặc và tổng đại số; "
            + "nhân, chia số nguyên và quy tắc dấu."),
        Review("c3cc", "Bài tập cuối chương III",
            "Tổng hợp toàn chương: tính nhanh với số nguyên; bài toán thực tế về nhiệt độ, độ cao, thu – chi; "
            + "ước và bội của một số nguyên."),
    });
}
