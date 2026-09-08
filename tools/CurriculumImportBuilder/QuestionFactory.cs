using System.Globalization;

namespace CurriculumImportBuilder;

/// <summary>
/// Bộ sinh câu hỏi Toán 6 theo *chủ đề* (1–9, đánh số theo KNTT). Xác định (seed cố định) nên
/// mỗi lần chạy cho ra đúng cùng một bộ câu hỏi. Chuyển thể từ <c>DemoQuestionFactory</c>.
/// </summary>
internal static class QuestionFactory
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Sinh <paramref name="count"/> câu cho một chương, xoay vòng qua các generator của các chủ đề chương đó.</summary>
    public static List<QItem> ForChapter(string bankKey, string chapterKey, int chapterIndex, int[] themes, int count, Random rng)
    {
        var gens = themes.SelectMany(Generators).ToList();
        var list = new List<QItem>(count);
        for (var i = 0; i < count; i++)
        {
            var g = gens[i % gens.Count](rng);
            list.Add(new QItem(
                Key: $"{bankKey}-c{chapterIndex}-{i + 1:00}",
                BankKey: bankKey,
                NodeKey: chapterKey,
                Type: g.Type,
                Difficulty: g.Difficulty,
                Text: g.Text,
                CorrectAnswer: g.Correct,
                Explanation: g.Explanation,
                Options: g.Options));
        }
        return list;
    }

    private readonly record struct Gen(string Type, string Difficulty, string Text, string Correct, string Explanation, List<QOption> Options);

    private static List<Func<Random, Gen>> Generators(int theme) => theme switch
    {
        1 => new() { NatArithMc, NatCompareTf, PlaceValueFill, PowMc },
        2 => new() { DivisibleTf, PrimeMc, FactorFill, GcdLcmMc },
        3 => new() { IntArithMc, IntCompareTf, AbsFill, IntWordMc },
        4 => new() { RectAreaMc, ShapePropTf, PerimeterFill, TriangleAreaMc },
        5 => new() { SymAxisMc, SymTf, SymCenterTf, SymCountFill },
        6 => new() { FracAddFill, FracCompareMc, FracMulMc, FracEqualTf },
        7 => new() { DecArithMc, DecRoundFill, PercentMc, DecCompareTf },
        8 => new() { AngleClassifyMc, MidpointFill, PointLineTf, AngleCalcMc },
        9 => new() { ProbFill, FreqMc, CertainEventTf, TableMc },
        _ => new() { NatArithMc, NatCompareTf },
    };

    // ---------------- builders ----------------
    private static Gen Mc(string text, string correct, IEnumerable<string> distractors, string diff, string expl, Random rng)
    {
        var opts = new List<(string t, bool c)> { (correct, true) };
        foreach (var d in distractors.Distinct().Where(d => d != correct).Take(3))
            opts.Add((d, false));
        for (var i = opts.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (opts[i], opts[j]) = (opts[j], opts[i]);
        }
        var options = opts.Select((o, idx) => new QOption(idx + 1, o.t, o.c)).ToList();
        return new Gen("MultipleChoice", diff, text, correct, expl, options);
    }

    private static Gen Tf(string text, bool isTrue, string diff, string expl) => new(
        "TrueFalse", diff, text, isTrue ? "true" : "false", expl,
        new List<QOption> { new(1, "Đúng", isTrue), new(2, "Sai", !isTrue) });

    private static Gen Fill(string text, string answer, string diff, string expl) =>
        new("FillBlank", diff, text, answer, expl, new List<QOption>());

    private static IEnumerable<string> NumDistractors(long correct, Random rng)
    {
        var set = new HashSet<long>();
        while (set.Count < 3)
        {
            long delta = rng.Next(1, 9) * (rng.Next(2) == 0 ? 1 : -1);
            var cand = correct + delta;
            if (cand != correct) set.Add(cand);
        }
        return set.Select(v => v.ToString());
    }

    private static string Diff(Random rng) => new[] { "Easy", "Medium", "Hard" }[rng.Next(3)];
    private static string Dec(double v) => v.ToString("0.##", Inv).Replace('.', ',');

    private static int Gcd(int a, int b)
    {
        a = Math.Abs(a); b = Math.Abs(b);
        while (b != 0) (a, b) = (b, a % b);
        return a == 0 ? 1 : a;
    }

    private static string PrimeFactorString(int n)
    {
        var parts = new List<string>();
        for (var p = 2; p * p <= n; p++)
        {
            var c = 0;
            while (n % p == 0) { n /= p; c++; }
            if (c == 1) parts.Add(p.ToString());
            else if (c > 1) parts.Add($"{p}^{c}");
        }
        if (n > 1) parts.Add(n.ToString());
        return string.Join(" × ", parts);
    }

    // ===== chủ đề 1 — số tự nhiên =====
    private static Gen NatArithMc(Random rng)
    {
        int a = rng.Next(120, 900), b = rng.Next(20, 400), op = rng.Next(3);
        if (op == 1 && b > a) (a, b) = (b, a);
        var (sym, res) = op switch { 0 => ("+", (long)a + b), 1 => ("−", (long)a - b), _ => ("×", (long)a * b) };
        return Mc($"Kết quả của phép tính {a} {sym} {b} là:", res.ToString(), NumDistractors(res, rng), Diff(rng), $"{a} {sym} {b} = {res}.", rng);
    }

    private static Gen NatCompareTf(Random rng)
    {
        int a = rng.Next(1000, 9999), b = rng.Next(1000, 9999);
        var greater = rng.Next(2) == 0;
        var sym = greater ? ">" : "<";
        return Tf($"Khẳng định sau đúng hay sai: {a} {sym} {b}.", greater ? a > b : a < b, Diff(rng),
            $"So sánh: {a} {(a > b ? ">" : a < b ? "<" : "=")} {b}.");
    }

    private static Gen PlaceValueFill(Random rng)
    {
        var n = rng.Next(10000, 99999);
        var digits = n.ToString().Select(c => c - '0').ToArray();
        var pos = rng.Next(digits.Length);
        var place = (int)Math.Pow(10, digits.Length - 1 - pos);
        var val = digits[pos] * (long)place;
        string[] names = { "đơn vị", "chục", "trăm", "nghìn", "chục nghìn" };
        return Fill($"Trong số {n}, chữ số {digits[pos]} ở hàng {names[digits.Length - 1 - pos]} có giá trị bằng bao nhiêu?",
            val.ToString(), Diff(rng), $"Giá trị = {digits[pos]} × {place} = {val}.");
    }

    private static Gen PowMc(Random rng)
    {
        int b = rng.Next(2, 7), e = rng.Next(2, 5);
        var res = (long)Math.Pow(b, e);
        return Mc($"Giá trị của luỹ thừa {b}^{e} là:", res.ToString(),
            new[] { (b * e).ToString(), (res + b).ToString(), (res - b).ToString(), ((long)Math.Pow(b, e - 1)).ToString() },
            Diff(rng), $"{b}^{e} = {string.Join(" × ", Enumerable.Repeat(b, e))} = {res}.", rng);
    }

    // ===== chủ đề 2 — chia hết =====
    private static Gen DivisibleTf(Random rng)
    {
        var d = new[] { 2, 3, 5, 9 }[rng.Next(4)];
        var n = rng.Next(100, 999);
        return Tf($"Khẳng định sau đúng hay sai: {n} chia hết cho {d}.", n % d == 0, Diff(rng),
            $"{n} : {d} {(n % d == 0 ? "là phép chia hết" : $"dư {n % d}")}.");
    }

    private static Gen PrimeMc(Random rng)
    {
        int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };
        var p = primes[rng.Next(primes.Length)];
        var comps = new[] { 4, 6, 8, 9, 10, 12, 15, 21, 25, 27 }.OrderBy(_ => rng.Next()).Take(3).Select(x => x.ToString());
        return Mc("Số nào sau đây là số nguyên tố?", p.ToString(), comps, Diff(rng),
            $"{p} chỉ có hai ước là 1 và {p} nên là số nguyên tố.", rng);
    }

    private static Gen FactorFill(Random rng)
    {
        int[] samples = { 12, 18, 24, 36, 45, 60, 72, 84, 90, 100 };
        var n = samples[rng.Next(samples.Length)];
        var f = PrimeFactorString(n);
        return Fill($"Phân tích số {n} ra thừa số nguyên tố (viết dạng tích luỹ thừa, ví dụ 2^2 × 3):", f, "Medium", $"{n} = {f}.");
    }

    private static Gen GcdLcmMc(Random rng)
    {
        int a = rng.Next(6, 24), b = rng.Next(6, 24);
        var gcd = rng.Next(2) == 0;
        long res = gcd ? Gcd(a, b) : a / Gcd(a, b) * (long)b;
        var what = gcd ? $"ƯCLN({a}, {b})" : $"BCNN({a}, {b})";
        return Mc($"Giá trị của {what} là:", res.ToString(), NumDistractors(res, rng), "Medium", $"{what} = {res}.", rng);
    }

    // ===== chủ đề 3 — số nguyên =====
    private static Gen IntArithMc(Random rng)
    {
        int a = rng.Next(-30, 30), b = rng.Next(-30, 30), op = rng.Next(3);
        var (sym, res) = op switch { 0 => ("+", (long)a + b), 1 => ("−", (long)a - b), _ => ("×", (long)a * b) };
        return Mc($"Kết quả của phép tính ({a}) {sym} ({b}) là:", res.ToString(), NumDistractors(res, rng), Diff(rng),
            $"({a}) {sym} ({b}) = {res}.", rng);
    }

    private static Gen IntCompareTf(Random rng)
    {
        int a = rng.Next(-50, 0), b = rng.Next(-50, 0);
        return Tf($"Khẳng định sau đúng hay sai: {a} > {b}.", a > b, Diff(rng),
            $"Trên trục số, {Math.Max(a, b)} nằm bên phải {Math.Min(a, b)}.");
    }

    private static Gen AbsFill(Random rng)
    {
        var a = rng.Next(-99, -1);
        return Fill($"Giá trị tuyệt đối |{a}| bằng bao nhiêu?", (-a).ToString(), "Easy",
            $"|{a}| = {-a} (khoảng cách từ điểm {a} đến gốc 0).");
    }

    private static Gen IntWordMc(Random rng)
    {
        int t0 = rng.Next(-8, 3), change = rng.Next(3, 12);
        var up = rng.Next(2) == 0;
        long res = up ? t0 + change : t0 - change;
        return Mc($"Nhiệt độ lúc đầu là {t0}°C, sau đó {(up ? "tăng" : "giảm")} {change}°C. Nhiệt độ lúc sau là:",
            $"{res}°C", new[] { $"{t0}°C", $"{(up ? t0 - change : t0 + change)}°C", $"{res + 1}°C" }, Diff(rng),
            $"{t0} {(up ? "+" : "−")} {change} = {res}.", rng);
    }

    // ===== chủ đề 4 — hình phẳng thực tiễn =====
    private static Gen RectAreaMc(Random rng)
    {
        int a = rng.Next(4, 20), b = rng.Next(3, 15);
        long res = (long)a * b;
        return Mc($"Hình chữ nhật dài {a} cm, rộng {b} cm. Diện tích của nó là:", $"{res} cm²",
            new[] { $"{2 * (a + b)} cm²", $"{res + a} cm²", $"{a + b} cm²" }, Diff(rng), $"S = {a} × {b} = {res} (cm²).", rng);
    }

    private static Gen ShapePropTf(Random rng)
    {
        var facts = new (string, bool)[]
        {
            ("Hình vuông có bốn cạnh bằng nhau và bốn góc vuông.", true),
            ("Hình thoi có bốn góc vuông.", false),
            ("Hình bình hành có hai cặp cạnh đối song song.", true),
            ("Hình thang cân có hai đường chéo bằng nhau.", true),
            ("Hình chữ nhật có bốn cạnh luôn bằng nhau.", false),
            ("Hình lục giác đều có sáu cạnh bằng nhau.", true),
        };
        var f = facts[rng.Next(facts.Length)];
        return Tf($"Khẳng định sau đúng hay sai: {f.Item1}", f.Item2, Diff(rng),
            f.Item2 ? "Đây là tính chất đúng của hình đó." : "Khẳng định này sai với định nghĩa.");
    }

    private static Gen PerimeterFill(Random rng)
    {
        int a = rng.Next(5, 25), b = rng.Next(4, 20);
        long p = 2L * (a + b);
        return Fill($"Chu vi hình chữ nhật dài {a} m, rộng {b} m bằng bao nhiêu mét?", p.ToString(), "Easy",
            $"P = 2 × ({a} + {b}) = {p} (m).");
    }

    private static Gen TriangleAreaMc(Random rng)
    {
        int b = rng.Next(4, 20) * 2, h = rng.Next(3, 15);
        long res = (long)b * h / 2;
        return Mc($"Tam giác có đáy {b} cm, chiều cao {h} cm. Diện tích của tam giác là:", $"{res} cm²",
            new[] { $"{b * h} cm²", $"{res + h} cm²", $"{b + h} cm²" }, "Medium",
            $"S = (đáy × chiều cao) : 2 = ({b} × {h}) : 2 = {res} (cm²).", rng);
    }

    // ===== chủ đề 5 — đối xứng =====
    private static Gen SymAxisMc(Random rng)
    {
        var items = new (string, int)[]
        {
            ("hình vuông", 4), ("hình chữ nhật (không phải hình vuông)", 2),
            ("tam giác đều", 3), ("hình tròn", 0), ("hình thoi (không phải hình vuông)", 2),
        };
        var it = items[rng.Next(items.Length)];
        var correct = it.Item2 == 0 ? "Vô số" : it.Item2.ToString();
        return Mc($"Số trục đối xứng của {it.Item1} là:", correct,
            new[] { "1", "2", "3", "4", "Vô số" }.Where(x => x != correct), Diff(rng),
            $"{it.Item1} có {(it.Item2 == 0 ? "vô số" : it.Item2.ToString())} trục đối xứng.", rng);
    }

    private static Gen SymTf(Random rng)
    {
        var letters = new (char, bool)[] { ('A', true), ('B', true), ('H', true), ('F', false), ('G', false), ('T', true), ('N', false) };
        var l = letters[rng.Next(letters.Length)];
        return Tf($"Khẳng định sau đúng hay sai: chữ in hoa \"{l.Item1}\" có trục đối xứng.", l.Item2, Diff(rng),
            l.Item2 ? $"Chữ {l.Item1} có ít nhất một trục đối xứng." : $"Chữ {l.Item1} không có trục đối xứng.");
    }

    private static Gen SymCenterTf(Random rng)
    {
        var shapes = new (string, bool)[]
        {
            ("Hình bình hành", true), ("Hình chữ nhật", true), ("Tam giác đều", false),
            ("Hình tròn", true), ("Hình thang cân", false), ("Hình lục giác đều", true),
        };
        var s = shapes[rng.Next(shapes.Length)];
        return Tf($"Khẳng định sau đúng hay sai: {s.Item1} có tâm đối xứng.", s.Item2, Diff(rng),
            s.Item2 ? "Quay nửa vòng quanh tâm thì hình trùng chính nó." : "Hình này không có tâm đối xứng.");
    }

    private static Gen SymCountFill(Random rng)
    {
        var items = new (string, int)[] { ("hình vuông", 4), ("tam giác đều", 3), ("hình chữ nhật", 2), ("hình thang cân", 1) };
        var it = items[rng.Next(items.Length)];
        return Fill($"Số trục đối xứng của {it.Item1} là bao nhiêu?", it.Item2.ToString(), "Easy", $"{it.Item1} có {it.Item2} trục đối xứng.");
    }

    // ===== chủ đề 6 — phân số =====
    private static Gen FracAddFill(Random rng)
    {
        int b = rng.Next(2, 9), d = rng.Next(2, 9), a = rng.Next(1, b), c = rng.Next(1, d);
        int num = a * d + c * b, den = b * d, g = Gcd(num, den);
        var ans = $"{num / g}/{den / g}";
        return Fill($"Tính và rút gọn: {a}/{b} + {c}/{d} = ? (viết phân số tối giản dạng a/b)", ans, "Medium",
            $"{a}/{b} + {c}/{d} = {num}/{den} = {ans}.");
    }

    private static Gen FracCompareMc(Random rng)
    {
        int b = rng.Next(3, 10), d = rng.Next(3, 10), a = rng.Next(1, b), c = rng.Next(1, d);
        double x = (double)a / b, y = (double)c / d;
        var correct = Math.Abs(x - y) < 1e-9 ? "Bằng nhau" : x > y ? $"{a}/{b}" : $"{c}/{d}";
        return Mc($"Phân số nào lớn hơn: {a}/{b} hay {c}/{d}?", correct,
            new[] { $"{a}/{b}", $"{c}/{d}", "Bằng nhau" }.Where(o => o != correct), Diff(rng),
            $"Quy đồng: {a}/{b} = {a * d}/{b * d}; {c}/{d} = {c * b}/{b * d}.", rng);
    }

    private static Gen FracMulMc(Random rng)
    {
        int b = rng.Next(2, 8), d = rng.Next(2, 8), a = rng.Next(1, b + 2), c = rng.Next(1, d + 2);
        int num = a * c, den = b * d, g = Gcd(num, den);
        var ans = $"{num / g}/{den / g}";
        return Mc($"Kết quả của {a}/{b} × {c}/{d} (rút gọn) là:", ans,
            new[] { $"{num}/{den}", $"{a + c}/{b + d}", $"{a * d}/{b * c}" }, "Medium", $"{a}/{b} × {c}/{d} = {num}/{den} = {ans}.", rng);
    }

    private static Gen FracEqualTf(Random rng)
    {
        int a = rng.Next(1, 6), b = rng.Next(2, 7), m = rng.Next(2, 5);
        var truth = rng.Next(2) == 0;
        int c = a * m, d = truth ? b * m : b * m + 1;
        return Tf($"Khẳng định sau đúng hay sai: {a}/{b} = {c}/{d}.", truth, Diff(rng),
            truth ? $"Nhân cả tử và mẫu của {a}/{b} với {m} được {c}/{d}." : $"{a}/{b} ≠ {c}/{d} vì tích chéo khác nhau.");
    }

    // ===== chủ đề 7 — số thập phân =====
    private static Gen DecArithMc(Random rng)
    {
        double a = rng.Next(15, 200) / 10.0, b = rng.Next(10, 120) / 10.0;
        var add = rng.Next(2) == 0;
        if (!add && b > a) (a, b) = (b, a);
        var res = add ? a + b : a - b;
        return Mc($"Kết quả của phép tính {Dec(a)} {(add ? "+" : "−")} {Dec(b)} là:", Dec(res),
            new[] { Dec(res + 0.1), Dec(res - 1), Dec(res + 1) }, Diff(rng), $"{Dec(a)} {(add ? "+" : "−")} {Dec(b)} = {Dec(res)}.", rng);
    }

    private static Gen DecRoundFill(Random rng)
    {
        double x = rng.Next(1000, 9999) / 100.0;
        var r = Math.Round(x, 1, MidpointRounding.AwayFromZero);
        return Fill($"Làm tròn số {x.ToString("0.00", Inv).Replace('.', ',')} đến hàng phần mười.", Dec(r), "Easy",
            $"Nhìn chữ số hàng phần trăm: {x.ToString("0.00", Inv).Replace('.', ',')} ≈ {Dec(r)}.");
    }

    private static Gen PercentMc(Random rng)
    {
        var p = new[] { 10, 20, 25, 50, 5, 12 }[rng.Next(6)];
        var whole = rng.Next(4, 40) * 5;
        var res = whole * p / 100.0;
        return Mc($"{p}% của {whole} là:", Dec(res), new[] { Dec(res + p), Dec(res * 2), Dec(res + 1) }, Diff(rng),
            $"{whole} × {p}/100 = {Dec(res)}.", rng);
    }

    private static Gen DecCompareTf(Random rng)
    {
        double a = rng.Next(100, 999) / 100.0, b = rng.Next(100, 999) / 100.0;
        return Tf($"Khẳng định sau đúng hay sai: {Dec(a)} > {Dec(b)}.", a > b, Diff(rng),
            $"So sánh phần nguyên rồi phần thập phân: {Dec(a)} {(a > b ? ">" : "<")} {Dec(b)}.");
    }

    // ===== chủ đề 8 — hình học cơ bản =====
    private static Gen AngleClassifyMc(Random rng)
    {
        var deg = rng.Next(1, 180);
        var kind = deg < 90 ? "Góc nhọn" : deg == 90 ? "Góc vuông" : "Góc tù";
        return Mc($"Góc có số đo {deg}° là góc gì?", kind,
            new[] { "Góc nhọn", "Góc vuông", "Góc tù", "Góc bẹt" }.Where(o => o != kind), Diff(rng),
            $"{deg}° {(deg < 90 ? "< 90° → góc nhọn" : deg == 90 ? "= 90° → góc vuông" : "trong (90°; 180°) → góc tù")}.", rng);
    }

    private static Gen MidpointFill(Random rng)
    {
        var ab = rng.Next(2, 20) * 2;
        return Fill($"Cho đoạn thẳng AB = {ab} cm, M là trung điểm của AB. Độ dài MA bằng bao nhiêu cm?",
            (ab / 2).ToString(), "Easy", $"MA = MB = AB : 2 = {ab} : 2 = {ab / 2} (cm).");
    }

    private static Gen PointLineTf(Random rng)
    {
        var facts = new (string, bool)[]
        {
            ("Qua hai điểm phân biệt có duy nhất một đường thẳng.", true),
            ("Qua một điểm có duy nhất một đường thẳng.", false),
            ("Hai đường thẳng phân biệt có nhiều nhất một điểm chung.", true),
            ("Mỗi đoạn thẳng có hai trung điểm.", false),
            ("Trung điểm chia đoạn thẳng thành hai phần bằng nhau.", true),
        };
        var f = facts[rng.Next(facts.Length)];
        return Tf($"Khẳng định sau đúng hay sai: {f.Item1}", f.Item2, Diff(rng),
            f.Item2 ? "Đây là tính chất hình học đúng." : "Khẳng định này sai.");
    }

    private static Gen AngleCalcMc(Random rng)
    {
        var part = rng.Next(20, 70);
        var comp = rng.Next(2) == 0;
        var total = comp ? 90 : 180;
        var res = total - part;
        return Mc($"Hai góc {(comp ? "phụ nhau" : "bù nhau")}, một góc có số đo {part}°. Góc còn lại có số đo:",
            $"{res}°", new[] { $"{part}°", $"{res + 10}°", $"{total}°" }, Diff(rng),
            $"Tổng hai góc {(comp ? "phụ nhau" : "bù nhau")} bằng {total}° nên góc còn lại = {total}° − {part}° = {res}°.", rng);
    }

    // ===== chủ đề 9 — dữ liệu & xác suất =====
    private static Gen ProbFill(Random rng)
    {
        int total = rng.Next(10, 40), hit = rng.Next(1, total), g = Gcd(hit, total);
        var ans = $"{hit / g}/{total / g}";
        return Fill($"Gieo một con xúc xắc {total} lần thì có {hit} lần xuất hiện mặt 6 chấm. " +
                    "Xác suất thực nghiệm của sự kiện \"mặt 6 chấm\" là bao nhiêu? (viết phân số tối giản)",
            ans, "Medium", $"Xác suất thực nghiệm = {hit}/{total} = {ans}.");
    }

    private static Gen FreqMc(Random rng)
    {
        var data = Enumerable.Range(0, 5).Select(_ => rng.Next(2, 12)).ToArray();
        var sum = data.Sum();
        return Mc($"Bảng số học sinh yêu thích 5 môn thể thao: {string.Join("; ", data)}. Tổng số học sinh được khảo sát là:",
            sum.ToString(), new[] { (sum + data.Max()).ToString(), (sum - 2).ToString(), data.Max().ToString() }, "Easy",
            $"Cộng các giá trị: {string.Join(" + ", data)} = {sum}.", rng);
    }

    private static Gen CertainEventTf(Random rng)
    {
        var facts = new (string, bool)[]
        {
            ("Khi gieo một con xúc xắc, sự kiện \"số chấm không vượt quá 6\" là sự kiện chắc chắn.", true),
            ("Khi gieo một con xúc xắc, sự kiện \"xuất hiện mặt 7 chấm\" là sự kiện không thể.", true),
            ("Xác suất thực nghiệm của một sự kiện luôn lớn hơn 1.", false),
            ("Khi tung đồng xu, khả năng ra mặt sấp và mặt ngửa là như nhau.", true),
            ("Sự kiện ngẫu nhiên là sự kiện luôn xảy ra.", false),
        };
        var f = facts[rng.Next(facts.Length)];
        return Tf($"Khẳng định sau đúng hay sai: {f.Item1}", f.Item2, Diff(rng),
            f.Item2 ? "Khẳng định đúng theo định nghĩa." : "Khẳng định này sai.");
    }

    private static Gen TableMc(Random rng)
    {
        string[] names = { "Toán", "Văn", "Anh", "Khoa học", "Thể dục" };
        var data = names.Select(_ => rng.Next(3, 15)).ToArray();
        var idx = Array.IndexOf(data, data.Max());
        return Mc($"Bảng số học sinh đạt điểm giỏi: {string.Join("; ", names.Zip(data, (n, d) => $"{n}: {d}"))}. " +
                 "Môn nào có nhiều học sinh đạt điểm giỏi nhất?", names[idx],
            names.Where((_, i) => i != idx).OrderBy(_ => rng.Next()).Take(3), "Easy",
            $"Giá trị lớn nhất là {data[idx]} ứng với môn {names[idx]}.", rng);
    }
}
