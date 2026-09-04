using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Data.Seed
{
    /// <summary>
    /// Deterministic templated generator for the demo Question Bank — produces realistic Toán 6
    /// items (MultipleChoice / TrueFalse / FillBlank) per KNTT chapter.
    /// </summary>
    internal static class DemoQuestionFactory
    {
        public sealed record GenOption(string Text, bool IsCorrect);

        public sealed record GenQuestion(
            string Text,
            QuestionType Type,
            DifficultyLevel Difficulty,
            string? CorrectAnswer,
            string Explanation,
            IReadOnlyList<GenOption> Options);

        /// <summary>Generate <paramref name="count"/> questions for KNTT chapter <paramref name="chapterNumber"/> (1..9).</summary>
        public static List<GenQuestion> ForChapter(int chapterNumber, int count, Random rng)
        {
            var gens = Generators(chapterNumber);
            var list = new List<GenQuestion>(count);
            for (int i = 0; i < count; i++)
                list.Add(gens[i % gens.Count](rng));
            return list;
        }

        private static List<Func<Random, GenQuestion>> Generators(int chapter) => chapter switch
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
            _ => new() { NatArithMc, NatCompareTf, PlaceValueFill, PowMc },
        };

        // ---------------------------------------------------------------
        //  builders
        // ---------------------------------------------------------------
        private static GenQuestion Mc(string text, string correct, string[] distractors, DifficultyLevel diff, string expl, Random rng)
        {
            var opts = new List<GenOption> { new(correct, true) };
            foreach (var d in distractors.Distinct().Where(d => d != correct).Take(3))
                opts.Add(new GenOption(d, false));
            // deterministic shuffle
            for (int i = opts.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (opts[i], opts[j]) = (opts[j], opts[i]);
            }
            return new GenQuestion(text, QuestionType.MultipleChoice, diff, correct, expl, opts);
        }

        private static GenQuestion Tf(string text, bool isTrue, DifficultyLevel diff, string expl) => new(
            text, QuestionType.TrueFalse, diff, isTrue ? "true" : "false", expl,
            new List<GenOption> { new("Đúng", isTrue), new("Sai", !isTrue) });

        private static GenQuestion Fill(string text, string answer, DifficultyLevel diff, string expl) =>
            new(text, QuestionType.FillBlank, diff, answer, expl, Array.Empty<GenOption>());

        private static string[] NumDistractors(long correct, Random rng)
        {
            var set = new HashSet<long>();
            while (set.Count < 3)
            {
                long delta = rng.Next(1, 9) * (rng.Next(2) == 0 ? 1 : -1);
                long cand = correct + delta;
                if (cand != correct && cand > long.MinValue) set.Add(cand);
            }
            return set.Select(v => v.ToString()).ToArray();
        }

        private static DifficultyLevel Rot(Random rng) =>
            (DifficultyLevel)rng.Next(0, 3);

        // ---------------------------------------------------------------
        //  chapter 1 — số tự nhiên
        // ---------------------------------------------------------------
        private static GenQuestion NatArithMc(Random rng)
        {
            int a = rng.Next(120, 900), b = rng.Next(20, 400);
            int op = rng.Next(3);
            (string sym, long res) = op switch
            {
                0 => ("+", (long)a + b),
                1 => ("-", (long)a - b),
                _ => ("×", (long)a * b),
            };
            if (op == 1 && b > a) (a, b) = (b, a);
            res = op switch { 0 => (long)a + b, 1 => (long)a - b, _ => (long)a * b };
            return Mc($"Kết quả của phép tính {a} {sym} {b} là:", res.ToString(), NumDistractors(res, rng),
                Rot(rng), $"{a} {sym} {b} = {res}.", rng);
        }

        private static GenQuestion NatCompareTf(Random rng)
        {
            int a = rng.Next(1000, 9999), b = rng.Next(1000, 9999);
            bool claimGreater = rng.Next(2) == 0;
            bool truth = claimGreater ? a > b : a < b;
            string sym = claimGreater ? ">" : "<";
            return Tf($"Khẳng định sau đúng hay sai: {a} {sym} {b}.", truth, Rot(rng),
                $"So sánh: {a} {(a > b ? ">" : a < b ? "<" : "=")} {b}.");
        }

        private static GenQuestion PlaceValueFill(Random rng)
        {
            int n = rng.Next(10000, 99999);
            int[] digits = n.ToString().Select(c => c - '0').ToArray();
            int pos = rng.Next(digits.Length);
            int place = (int)Math.Pow(10, digits.Length - 1 - pos);
            long val = digits[pos] * (long)place;
            string[] names = { "đơn vị", "chục", "trăm", "nghìn", "chục nghìn" };
            return Fill($"Trong số {n}, chữ số {digits[pos]} ở hàng {names[digits.Length - 1 - pos]} có giá trị bằng bao nhiêu?",
                val.ToString(), Rot(rng), $"Giá trị = {digits[pos]} × {place} = {val}.");
        }

        private static GenQuestion PowMc(Random rng)
        {
            int b = rng.Next(2, 7), e = rng.Next(2, 5);
            long res = (long)Math.Pow(b, e);
            return Mc($"Giá trị của luỹ thừa {b}^{e} là:", res.ToString(),
                new[] { (b * e).ToString(), (res + b).ToString(), (res - b).ToString(), ((long)Math.Pow(b, e - 1)).ToString() },
                Rot(rng), $"{b}^{e} = {string.Join(" × ", Enumerable.Repeat(b, e))} = {res}.", rng);
        }

        // ---------------------------------------------------------------
        //  chapter 2 — chia hết
        // ---------------------------------------------------------------
        private static GenQuestion DivisibleTf(Random rng)
        {
            int d = new[] { 2, 3, 5, 9 }[rng.Next(4)];
            int n = rng.Next(100, 999);
            bool truth = n % d == 0;
            return Tf($"Khẳng định sau đúng hay sai: {n} chia hết cho {d}.", truth, Rot(rng),
                $"{n} : {d} = {n / (double)d:0.##} nên {n} {(truth ? "chia hết" : "không chia hết")} cho {d}.");
        }

        private static GenQuestion PrimeMc(Random rng)
        {
            int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };
            int p = primes[rng.Next(primes.Length)];
            var comps = new[] { 4, 6, 8, 9, 10, 12, 15, 21, 25, 27 };
            var distractors = comps.OrderBy(_ => rng.Next()).Take(3).Select(x => x.ToString()).ToArray();
            return Mc("Số nào sau đây là số nguyên tố?", p.ToString(), distractors, Rot(rng),
                $"{p} chỉ có hai ước là 1 và {p} nên là số nguyên tố.", rng);
        }

        private static GenQuestion FactorFill(Random rng)
        {
            int[] samples = { 12, 18, 24, 36, 45, 60, 72, 84, 90, 100 };
            int n = samples[rng.Next(samples.Length)];
            string factor = PrimeFactorString(n);
            return Fill($"Phân tích số {n} ra thừa số nguyên tố (viết dạng tích các luỹ thừa, ví dụ 2^2 × 3):",
                factor, DifficultyLevel.Medium, $"{n} = {factor}.");
        }

        private static GenQuestion GcdLcmMc(Random rng)
        {
            int a = rng.Next(6, 24), b = rng.Next(6, 24);
            bool gcd = rng.Next(2) == 0;
            long res = gcd ? Gcd(a, b) : a / Gcd(a, b) * b;
            string what = gcd ? $"ƯCLN({a}, {b})" : $"BCNN({a}, {b})";
            return Mc($"Giá trị của {what} là:", res.ToString(), NumDistractors(res, rng), DifficultyLevel.Medium,
                $"{what} = {res}.", rng);
        }

        // ---------------------------------------------------------------
        //  chapter 3 — số nguyên
        // ---------------------------------------------------------------
        private static GenQuestion IntArithMc(Random rng)
        {
            int a = rng.Next(-30, 30), b = rng.Next(-30, 30);
            int op = rng.Next(3);
            (string sym, long res) = op switch
            {
                0 => ("+", (long)a + b),
                1 => ("-", (long)a - b),
                _ => ("×", (long)a * b),
            };
            return Mc($"Kết quả của phép tính ({a}) {sym} ({b}) là:", res.ToString(), NumDistractors(res, rng),
                Rot(rng), $"({a}) {sym} ({b}) = {res}.", rng);
        }

        private static GenQuestion IntCompareTf(Random rng)
        {
            int a = rng.Next(-50, 0), b = rng.Next(-50, 0);
            bool truth = a > b;
            return Tf($"Khẳng định sau đúng hay sai: {a} > {b}.", truth, Rot(rng),
                $"Trên trục số, {Math.Max(a, b)} nằm bên phải {Math.Min(a, b)} nên {Math.Max(a, b)} > {Math.Min(a, b)}.");
        }

        private static GenQuestion AbsFill(Random rng)
        {
            int a = rng.Next(-99, -1);
            return Fill($"Giá trị tuyệt đối |{a}| bằng bao nhiêu?", (-a).ToString(), DifficultyLevel.Easy,
                $"|{a}| = {-a} vì giá trị tuyệt đối là khoảng cách từ điểm đó đến gốc 0.");
        }

        private static GenQuestion IntWordMc(Random rng)
        {
            int t0 = rng.Next(-8, 3), change = rng.Next(3, 12);
            bool up = rng.Next(2) == 0;
            long res = up ? t0 + change : t0 - change;
            string verb = up ? $"tăng thêm {change}" : $"giảm đi {change}";
            return Mc($"Nhiệt độ lúc đầu là {t0}°C, sau đó {verb}°C. Nhiệt độ lúc sau là:",
                $"{res}°C", new[] { $"{t0}°C", $"{(up ? t0 - change : t0 + change)}°C", $"{res + 1}°C" },
                Rot(rng), $"{t0} {(up ? "+" : "-")} {change} = {res}.", rng);
        }

        // ---------------------------------------------------------------
        //  chapter 4 — hình phẳng thực tiễn
        // ---------------------------------------------------------------
        private static GenQuestion RectAreaMc(Random rng)
        {
            int a = rng.Next(4, 20), b = rng.Next(3, 15);
            long res = (long)a * b;
            return Mc($"Hình chữ nhật có chiều dài {a} cm và chiều rộng {b} cm. Diện tích của nó là:",
                $"{res} cm²", new[] { $"{2 * (a + b)} cm²", $"{res + a} cm²", $"{a + b} cm²" }, Rot(rng),
                $"S = {a} × {b} = {res} (cm²).", rng);
        }

        private static GenQuestion ShapePropTf(Random rng)
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
            return Tf($"Khẳng định sau đúng hay sai: {f.Item1}", f.Item2, Rot(rng),
                f.Item2 ? "Đây là tính chất đúng của hình đó." : "Khẳng định này sai với định nghĩa của hình.");
        }

        private static GenQuestion PerimeterFill(Random rng)
        {
            int a = rng.Next(5, 25), b = rng.Next(4, 20);
            long p = 2L * (a + b);
            return Fill($"Chu vi hình chữ nhật có chiều dài {a} m và chiều rộng {b} m bằng bao nhiêu mét?",
                p.ToString(), DifficultyLevel.Easy, $"P = 2 × ({a} + {b}) = {p} (m).");
        }

        private static GenQuestion TriangleAreaMc(Random rng)
        {
            int baseLen = rng.Next(4, 20) * 2, h = rng.Next(3, 15);
            long res = (long)baseLen * h / 2;
            return Mc($"Tam giác có độ dài đáy {baseLen} cm và chiều cao {h} cm. Diện tích của tam giác là:",
                $"{res} cm²", new[] { $"{baseLen * h} cm²", $"{res + h} cm²", $"{baseLen + h} cm²" }, DifficultyLevel.Medium,
                $"S = (đáy × chiều cao) : 2 = ({baseLen} × {h}) : 2 = {res} (cm²).", rng);
        }

        // ---------------------------------------------------------------
        //  chapter 5 — đối xứng
        // ---------------------------------------------------------------
        private static GenQuestion SymAxisMc(Random rng)
        {
            var items = new (string, int)[]
            {
                ("hình vuông", 4), ("hình chữ nhật (không phải hình vuông)", 2),
                ("tam giác đều", 3), ("hình tròn", 0), ("hình thoi (không phải hình vuông)", 2),
            };
            var it = items[rng.Next(items.Length)];
            string correct = it.Item2 == 0 ? "Vô số" : it.Item2.ToString();
            return Mc($"Số trục đối xứng của {it.Item1} là:", correct,
                new[] { "1", "2", "3", "4", "Vô số" }.Where(x => x != correct).OrderBy(_ => rng.Next()).Take(3).ToArray(),
                Rot(rng), $"{it.Item1} có {(it.Item2 == 0 ? "vô số" : it.Item2.ToString())} trục đối xứng.", rng);
        }

        private static GenQuestion SymTf(Random rng)
        {
            var letters = new (char, bool)[] { ('A', true), ('B', true), ('H', true), ('F', false), ('G', false), ('T', true), ('N', false) };
            var l = letters[rng.Next(letters.Length)];
            return Tf($"Khẳng định sau đúng hay sai: chữ cái in hoa \"{l.Item1}\" có trục đối xứng.", l.Item2, Rot(rng),
                l.Item2 ? $"Chữ {l.Item1} có ít nhất một trục đối xứng." : $"Chữ {l.Item1} không có trục đối xứng.");
        }

        private static GenQuestion SymCenterTf(Random rng)
        {
            var shapes = new (string, bool)[]
            {
                ("Hình bình hành", true), ("Hình chữ nhật", true), ("Tam giác đều", false),
                ("Hình tròn", true), ("Hình thang cân", false), ("Hình lục giác đều", true),
            };
            var s = shapes[rng.Next(shapes.Length)];
            return Tf($"Khẳng định sau đúng hay sai: {s.Item1} có tâm đối xứng.", s.Item2, Rot(rng),
                s.Item2 ? "Khi quay nửa vòng quanh tâm, hình trùng với chính nó." : "Hình này không có tâm đối xứng.");
        }

        private static GenQuestion SymCountFill(Random rng)
        {
            var items = new (string, int)[] { ("hình vuông", 4), ("tam giác đều", 3), ("hình chữ nhật", 2), ("hình thang cân", 1) };
            var it = items[rng.Next(items.Length)];
            return Fill($"Số trục đối xứng của {it.Item1} là bao nhiêu?", it.Item2.ToString(), DifficultyLevel.Easy,
                $"{it.Item1} có {it.Item2} trục đối xứng.");
        }

        // ---------------------------------------------------------------
        //  chapter 6 — phân số
        // ---------------------------------------------------------------
        private static GenQuestion FracAddFill(Random rng)
        {
            int b = rng.Next(2, 9), d = rng.Next(2, 9);
            int a = rng.Next(1, b), c = rng.Next(1, d);
            int num = a * d + c * b, den = b * d;
            int g = Gcd(num, den);
            string ans = $"{num / g}/{den / g}";
            return Fill($"Tính và rút gọn: {a}/{b} + {c}/{d} = ? (viết dạng phân số tối giản a/b)", ans,
                DifficultyLevel.Medium, $"{a}/{b} + {c}/{d} = {num}/{den} = {ans}.");
        }

        private static GenQuestion FracCompareMc(Random rng)
        {
            int b = rng.Next(3, 10), d = rng.Next(3, 10);
            int a = rng.Next(1, b), c = rng.Next(1, d);
            double x = (double)a / b, y = (double)c / d;
            string correct = Math.Abs(x - y) < 1e-9 ? "Bằng nhau" : x > y ? $"{a}/{b}" : $"{c}/{d}";
            return Mc($"Phân số nào lớn hơn: {a}/{b} hay {c}/{d}?", correct,
                new[] { $"{a}/{b}", $"{c}/{d}", "Bằng nhau" }.Where(o => o != correct).ToArray(), Rot(rng),
                $"Quy đồng: {a}/{b} = {a * d}/{b * d}; {c}/{d} = {c * b}/{b * d}.", rng);
        }

        private static GenQuestion FracMulMc(Random rng)
        {
            int b = rng.Next(2, 8), d = rng.Next(2, 8);
            int a = rng.Next(1, b + 2), c = rng.Next(1, d + 2);
            int num = a * c, den = b * d, g = Gcd(num, den);
            string ans = $"{num / g}/{den / g}";
            return Mc($"Kết quả của phép nhân {a}/{b} × {c}/{d} (rút gọn) là:", ans,
                new[] { $"{num}/{den}", $"{a + c}/{b + d}", $"{a * d}/{b * c}" }, DifficultyLevel.Medium,
                $"{a}/{b} × {c}/{d} = {num}/{den} = {ans}.", rng);
        }

        private static GenQuestion FracEqualTf(Random rng)
        {
            int a = rng.Next(1, 6), b = rng.Next(2, 7), m = rng.Next(2, 5);
            bool truth = rng.Next(2) == 0;
            int c = a * m, d = truth ? b * m : b * m + 1;
            return Tf($"Khẳng định sau đúng hay sai: {a}/{b} = {c}/{d}.", truth, Rot(rng),
                truth ? $"Nhân cả tử và mẫu của {a}/{b} với {m} được {c}/{d}." : $"{a}/{b} ≠ {c}/{d} vì tích chéo khác nhau.");
        }

        // ---------------------------------------------------------------
        //  chapter 7 — số thập phân
        // ---------------------------------------------------------------
        private static GenQuestion DecArithMc(Random rng)
        {
            double a = rng.Next(15, 200) / 10.0, b = rng.Next(10, 120) / 10.0;
            bool add = rng.Next(2) == 0;
            double res = add ? a + b : a - b;
            if (!add && b > a) (a, b) = (b, a);
            res = add ? a + b : a - b;
            string r = res.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            string sa = a.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            string sb = b.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            return Mc($"Kết quả của phép tính {sa} {(add ? "+" : "-")} {sb} là:", r,
                new[] { (res + 0.1).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','),
                        (res - 1).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','),
                        (res + 1).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',') },
                Rot(rng), $"{sa} {(add ? "+" : "-")} {sb} = {r}.", rng);
        }

        private static GenQuestion DecRoundFill(Random rng)
        {
            double x = rng.Next(1000, 9999) / 100.0;
            double r = Math.Round(x, 1, MidpointRounding.AwayFromZero);
            string sx = x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            string sr = r.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            return Fill($"Làm tròn số {sx} đến hàng phần mười.", sr, DifficultyLevel.Easy,
                $"Chữ số hàng phần trăm quyết định: {sx} ≈ {sr}.");
        }

        private static GenQuestion PercentMc(Random rng)
        {
            int[] pcts = { 10, 20, 25, 50, 5, 12 };
            int p = pcts[rng.Next(pcts.Length)];
            int whole = rng.Next(4, 40) * 5;
            double res = whole * p / 100.0;
            string r = res.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            return Mc($"{p}% của {whole} là:", r,
                new[] { (res + p).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','),
                        (res * 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','),
                        (res + 1).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',') },
                Rot(rng), $"{whole} × {p}/100 = {r}.", rng);
        }

        private static GenQuestion DecCompareTf(Random rng)
        {
            double a = rng.Next(100, 999) / 100.0, b = rng.Next(100, 999) / 100.0;
            bool truth = a > b;
            string sa = a.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            string sb = b.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            return Tf($"Khẳng định sau đúng hay sai: {sa} > {sb}.", truth, Rot(rng),
                $"So sánh phần nguyên rồi đến phần thập phân: {sa} {(a > b ? ">" : "<")} {sb}.");
        }

        // ---------------------------------------------------------------
        //  chapter 8 — hình học cơ bản
        // ---------------------------------------------------------------
        private static GenQuestion AngleClassifyMc(Random rng)
        {
            int deg = rng.Next(1, 180);
            string kind = deg < 90 ? "Góc nhọn" : deg == 90 ? "Góc vuông" : deg < 180 ? "Góc tù" : "Góc bẹt";
            return Mc($"Góc có số đo {deg}° là góc gì?", kind,
                new[] { "Góc nhọn", "Góc vuông", "Góc tù", "Góc bẹt" }.Where(o => o != kind).ToArray(),
                Rot(rng), $"{deg}° {(deg < 90 ? "< 90° nên là góc nhọn" : deg == 90 ? "= 90° nên là góc vuông" : deg < 180 ? "trong khoảng 90°–180° nên là góc tù" : "= 180° nên là góc bẹt")}.", rng);
        }

        private static GenQuestion MidpointFill(Random rng)
        {
            int ab = rng.Next(2, 20) * 2;
            return Fill($"Cho đoạn thẳng AB = {ab} cm, M là trung điểm của AB. Độ dài MA bằng bao nhiêu cm?",
                (ab / 2).ToString(), DifficultyLevel.Easy, $"MA = MB = AB : 2 = {ab} : 2 = {ab / 2} (cm).");
        }

        private static GenQuestion PointLineTf(Random rng)
        {
            var facts = new (string, bool)[]
            {
                ("Qua hai điểm phân biệt có duy nhất một đường thẳng.", true),
                ("Qua một điểm có duy nhất một đường thẳng.", false),
                ("Hai đường thẳng phân biệt có nhiều nhất một điểm chung.", true),
                ("Mỗi đoạn thẳng có hai trung điểm.", false),
                ("Trung điểm của đoạn thẳng chia đoạn thẳng thành hai phần bằng nhau.", true),
            };
            var f = facts[rng.Next(facts.Length)];
            return Tf($"Khẳng định sau đúng hay sai: {f.Item1}", f.Item2, Rot(rng),
                f.Item2 ? "Đây là tính chất hình học đúng." : "Khẳng định này sai.");
        }

        private static GenQuestion AngleCalcMc(Random rng)
        {
            int part = rng.Next(20, 70);
            bool comp = rng.Next(2) == 0;
            int total = comp ? 90 : 180;
            int res = total - part;
            string what = comp ? "phụ nhau" : "bù nhau";
            return Mc($"Hai góc {what}, một góc có số đo {part}°. Góc còn lại có số đo:", $"{res}°",
                new[] { $"{part}°", $"{res + 10}°", $"{total}°" }, Rot(rng),
                $"Tổng hai góc {what} bằng {total}° nên góc còn lại = {total}° − {part}° = {res}°.", rng);
        }

        // ---------------------------------------------------------------
        //  chapter 9 — dữ liệu & xác suất
        // ---------------------------------------------------------------
        private static GenQuestion ProbFill(Random rng)
        {
            int total = rng.Next(10, 40), hit = rng.Next(1, total);
            int g = Gcd(hit, total);
            string ans = $"{hit / g}/{total / g}";
            return Fill($"Gieo một con xúc xắc {total} lần thì có {hit} lần xuất hiện mặt 6 chấm. " +
                        $"Xác suất thực nghiệm của sự kiện \"mặt 6 chấm\" là bao nhiêu? (viết dạng phân số tối giản)",
                ans, DifficultyLevel.Medium, $"Xác suất thực nghiệm = {hit}/{total} = {ans}.");
        }

        private static GenQuestion FreqMc(Random rng)
        {
            int[] data = Enumerable.Range(0, 5).Select(_ => rng.Next(2, 12)).ToArray();
            int sum = data.Sum();
            int max = data.Max();
            return Mc($"Một bảng thống kê số học sinh yêu thích 5 môn thể thao có các giá trị: {string.Join("; ", data)}. " +
                     $"Tổng số học sinh được khảo sát là:", sum.ToString(),
                new[] { (sum + max).ToString(), (sum - 2).ToString(), max.ToString() }, DifficultyLevel.Easy,
                $"Cộng các giá trị: {string.Join(" + ", data)} = {sum}.", rng);
        }

        private static GenQuestion CertainEventTf(Random rng)
        {
            var facts = new (string, bool)[]
            {
                ("Khi gieo một con xúc xắc, sự kiện \"số chấm không vượt quá 6\" là sự kiện chắc chắn.", true),
                ("Khi gieo một con xúc xắc, sự kiện \"xuất hiện mặt 7 chấm\" là sự kiện không thể.", true),
                ("Xác suất thực nghiệm của một sự kiện luôn lớn hơn 1.", false),
                ("Khi tung đồng xu, khả năng xuất hiện mặt sấp và mặt ngửa là như nhau.", true),
                ("Sự kiện ngẫu nhiên là sự kiện luôn xảy ra.", false),
            };
            var f = facts[rng.Next(facts.Length)];
            return Tf($"Khẳng định sau đúng hay sai: {f.Item1}", f.Item2, Rot(rng),
                f.Item2 ? "Khẳng định đúng theo định nghĩa." : "Khẳng định này sai.");
        }

        private static GenQuestion TableMc(Random rng)
        {
            string[] names = { "Toán", "Văn", "Anh", "Khoa học", "Thể dục" };
            int[] data = names.Select(_ => rng.Next(3, 15)).ToArray();
            int idx = Array.IndexOf(data, data.Max());
            return Mc($"Bảng số học sinh đạt điểm giỏi các môn: {string.Join("; ", names.Zip(data, (n, d) => $"{n}: {d}"))}. " +
                     $"Môn nào có nhiều học sinh đạt điểm giỏi nhất?", names[idx],
                names.Where((_, i) => i != idx).OrderBy(_ => rng.Next()).Take(3).ToArray(), DifficultyLevel.Easy,
                $"Giá trị lớn nhất là {data[idx]} ứng với môn {names[idx]}.", rng);
        }

        // ---------------------------------------------------------------
        //  math helpers
        // ---------------------------------------------------------------
        private static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) (a, b) = (b, a % b);
            return a == 0 ? 1 : a;
        }

        private static string PrimeFactorString(int n)
        {
            var parts = new List<string>();
            for (int p = 2; p * p <= n; p++)
            {
                int c = 0;
                while (n % p == 0) { n /= p; c++; }
                if (c == 1) parts.Add(p.ToString());
                else if (c > 1) parts.Add($"{p}^{c}");
            }
            if (n > 1) parts.Add(n.ToString());
            return string.Join(" × ", parts);
        }
    }
}
