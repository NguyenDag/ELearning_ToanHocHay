using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// A3/P2 — chuyển bộ file CSV import thành một <see cref="ImportPlan"/> đã kiểm tra cấu trúc
    /// (không đụng CSDL). Kiểm tra tiếp theo — trạng thái version, luật lồng node theo môn, trùng
    /// slug… — nằm ở <see cref="Implementations.ContentImportService"/>.
    /// </summary>
    public static class ContentImportParser
    {
        private static readonly string[] NodeHeaders = { "NodeKey", "ParentKey", "NodeType", "Title", "OrderIndex" };
        private static readonly string[] BlockHeaders = { "NodeKey", "OrderIndex", "BlockType", "ContentText" };
        private static readonly string[] CardHeaders = { "NodeKey", "DeckTitle", "FrontText", "BackText" };
        private static readonly string[] ResHeaders = { "NodeKey", "Title", "ResourceType" };
        private static readonly string[] CourseHeaders = { "Slug", "Title", "SubjectCode", "GradeCode" };
        private static readonly string[] BankHeaders = { "BankKey", "BankName" };
        private static readonly string[] QuestionHeaders = { "QuestionKey", "QuestionType", "QuestionText" };
        private static readonly string[] OptionHeaders = { "QuestionKey", "OptionText", "IsCorrect" };
        private static readonly string[] ExerciseHeaders = { "ExerciseKey", "ExerciseName", "ExerciseType", "Tier" };
        private static readonly string[] ExQuestionHeaders = { "ExerciseKey", "QuestionKey" };

        private static readonly HashSet<QuestionType> ChoiceQuestionTypes = new()
        {
            QuestionType.MultipleChoice, QuestionType.TrueFalse
        };

        private static readonly HashSet<LessonBlockType> TextBlockTypes = new()
        {
            LessonBlockType.Heading, LessonBlockType.Text, LessonBlockType.Definition,
            LessonBlockType.Example, LessonBlockType.Note, LessonBlockType.Formula
        };

        private static readonly HashSet<LessonBlockType> MediaBlockTypes = new()
        {
            LessonBlockType.Image, LessonBlockType.Video, LessonBlockType.Animation,
            LessonBlockType.Embed, LessonBlockType.Audio, LessonBlockType.Pdf
        };

        public static ImportPlan Parse(
            string? courseCsv,
            string? nodesCsv,
            string? blocksCsv,
            string? flashcardsCsv,
            string? resourcesCsv,
            int maxTreeDepth = 4,
            string? questionBankCsv = null,
            string? questionsCsv = null,
            string? questionOptionsCsv = null,
            string? exercisesCsv = null,
            string? exerciseQuestionsCsv = null)
        {
            var plan = new ImportPlan();

            ParseCourse(courseCsv, plan);

            if (string.IsNullOrWhiteSpace(nodesCsv))
            {
                plan.Add("nodes", null, null, "MISSING_FILE", ImportIssueSeverity.Error,
                    "Thiếu file nodes.csv — đây là file bắt buộc.");
                return plan;
            }

            ParseNodes(nodesCsv!, plan, maxTreeDepth);
            ParseBlocks(blocksCsv, plan);
            ParseFlashcards(flashcardsCsv, plan);
            ParseResources(resourcesCsv, plan);

            ParseAssessment(plan, questionBankCsv, questionsCsv, questionOptionsCsv, exercisesCsv, exerciseQuestionsCsv);

            return plan;
        }

        // ---------------------------------------------------------------
        //  course.csv
        // ---------------------------------------------------------------
        private static void ParseCourse(string? csv, ImportPlan plan)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;

            if (!CheckHeaders("course", csv!, CourseHeaders, plan)) return;

            var rows = CsvReader.Read(csv!);
            if (rows.Count == 0)
            {
                plan.Add("course", null, null, "EMPTY_FILE", ImportIssueSeverity.Error, "course.csv không có dòng dữ liệu nào.");
                return;
            }

            var r = rows[0];
            var header = new CourseHeader
            {
                Slug = r.Get("Slug"),
                Title = r.Get("Title"),
                SubjectCode = r.Get("SubjectCode"),
                GradeCode = r.Get("GradeCode"),
                FrameworkCode = r.Get("FrameworkCode"),
                FrameworkName = r.Get("FrameworkName"),
                Publisher = r.Get("Publisher"),
                VersionLabel = r.Get("VersionLabel"),
                Description = r.GetRaw("Description")
            };

            if (string.IsNullOrWhiteSpace(header.Slug))
                plan.Add("course", 1, null, "COURSE_SLUG_REQUIRED", ImportIssueSeverity.Error, "Thiếu Slug của khoá học.");
            else if (header.Slug.Length > 255)
                plan.Add("course", 1, null, "COURSE_SLUG_TOO_LONG", ImportIssueSeverity.Error, "Slug vượt quá 255 ký tự.");

            if (string.IsNullOrWhiteSpace(header.Title))
                plan.Add("course", 1, null, "COURSE_TITLE_REQUIRED", ImportIssueSeverity.Error, "Thiếu Title của khoá học.");
            else if (header.Title.Length > 255)
                plan.Add("course", 1, null, "COURSE_TITLE_TOO_LONG", ImportIssueSeverity.Error, "Title vượt quá 255 ký tự.");

            if (string.IsNullOrWhiteSpace(header.SubjectCode))
                plan.Add("course", 1, null, "COURSE_SUBJECT_REQUIRED", ImportIssueSeverity.Error, "Thiếu SubjectCode.");
            if (string.IsNullOrWhiteSpace(header.GradeCode))
                plan.Add("course", 1, null, "COURSE_GRADE_REQUIRED", ImportIssueSeverity.Error, "Thiếu GradeCode.");

            var priceRaw = r.Get("ListPrice");
            if (!string.IsNullOrWhiteSpace(priceRaw))
            {
                if (decimal.TryParse(priceRaw, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var price) && price >= 0)
                    header.ListPrice = price;
                else
                    plan.Add("course", 1, null, "BAD_LIST_PRICE", ImportIssueSeverity.Error,
                        $"ListPrice không hợp lệ: '{priceRaw}'.");
            }

            if (!string.IsNullOrWhiteSpace(header.FrameworkCode) && header.FrameworkCode.Length > 20)
                plan.Add("course", 1, null, "FRAMEWORK_CODE_TOO_LONG", ImportIssueSeverity.Error,
                    "FrameworkCode vượt quá 20 ký tự.");

            if (rows.Count > 1)
                plan.Add("course", 2, null, "COURSE_EXTRA_ROWS", ImportIssueSeverity.Warning,
                    "course.csv có nhiều hơn 1 dòng dữ liệu — chỉ dòng đầu được dùng.");

            plan.Course = header;
        }

        // ---------------------------------------------------------------
        //  nodes.csv
        // ---------------------------------------------------------------
        private static void ParseNodes(string csv, ImportPlan plan, int maxTreeDepth)
        {
            if (!CheckHeaders("nodes", csv, NodeHeaders, plan)) return;

            var rows = CsvReader.Read(csv);
            if (rows.Count == 0)
            {
                plan.Add("nodes", null, null, "EMPTY_FILE", ImportIssueSeverity.Error, "nodes.csv không có dòng dữ liệu nào.");
                return;
            }

            var byKey = new Dictionary<string, PlanNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                var key = r.Get("NodeKey");
                if (string.IsNullOrWhiteSpace(key))
                {
                    plan.Add("nodes", r.RowNumber, null, "MISSING_NODE_KEY", ImportIssueSeverity.Error, "Thiếu NodeKey.");
                    continue;
                }
                if (byKey.ContainsKey(key))
                {
                    plan.Add("nodes", r.RowNumber, key, "DUP_NODE_KEY", ImportIssueSeverity.Error,
                        $"NodeKey '{key}' bị trùng.");
                    continue;
                }

                var node = new PlanNode { Key = key, SourceRow = r.RowNumber };

                var parentKey = r.Get("ParentKey");
                node.ParentKey = string.IsNullOrWhiteSpace(parentKey) ? null : parentKey;

                var typeRaw = r.Get("NodeType");
                if (Enum.TryParse<NodeType>(typeRaw, ignoreCase: true, out var nodeType))
                    node.Type = nodeType;
                else
                    plan.Add("nodes", r.RowNumber, key, "BAD_NODE_TYPE", ImportIssueSeverity.Error,
                        $"NodeType không hợp lệ: '{typeRaw}'. Cho phép: Chapter, Topic, SubTopic, Lesson.");

                node.Title = r.Get("Title");
                if (string.IsNullOrWhiteSpace(node.Title))
                    plan.Add("nodes", r.RowNumber, key, "MISSING_TITLE", ImportIssueSeverity.Error, "Thiếu Title.");
                else if (node.Title.Length > 255)
                    plan.Add("nodes", r.RowNumber, key, "TITLE_TOO_LONG", ImportIssueSeverity.Error,
                        "Title vượt quá 255 ký tự.");

                var slug = r.Get("Slug");
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    if (slug.Length > 255)
                        plan.Add("nodes", r.RowNumber, key, "SLUG_TOO_LONG", ImportIssueSeverity.Error,
                            "Slug vượt quá 255 ký tự.");
                    else if (!IsSlug(slug))
                        plan.Add("nodes", r.RowNumber, key, "SLUG_INVALID", ImportIssueSeverity.Warning,
                            $"Slug '{slug}' nên chỉ gồm chữ thường, số và dấu gạch nối.");
                    node.Slug = slug;
                }

                var orderRaw = r.Get("OrderIndex");
                if (string.IsNullOrWhiteSpace(orderRaw))
                    node.OrderIndex = null;
                else if (int.TryParse(orderRaw, out var order))
                    node.OrderIndex = order;
                else
                    plan.Add("nodes", r.RowNumber, key, "BAD_ORDER_INDEX", ImportIssueSeverity.Error,
                        $"OrderIndex không phải số nguyên: '{orderRaw}'.");

                node.IsFree = ParseBool(r.Get("IsFree"), "nodes", r.RowNumber, key, "IsFree", plan, defaultValue: false);

                var durRaw = r.Get("DurationMinutes");
                if (!string.IsNullOrWhiteSpace(durRaw))
                {
                    if (int.TryParse(durRaw, out var dur) && dur >= 0)
                        node.DurationMinutes = dur;
                    else
                        plan.Add("nodes", r.RowNumber, key, "BAD_DURATION", ImportIssueSeverity.Error,
                            $"DurationMinutes không hợp lệ: '{durRaw}'.");
                }

                byKey[key] = node;
                plan.Nodes.Add(node);
            }

            // ----- resolve parents, detect cycles, compute depth -----
            foreach (var node in plan.Nodes)
            {
                if (node.ParentKey != null && !byKey.ContainsKey(node.ParentKey))
                {
                    plan.Add("nodes", node.SourceRow, node.Key, "PARENT_NOT_FOUND", ImportIssueSeverity.Error,
                        $"ParentKey '{node.ParentKey}' không có trong nodes.csv.");
                    node.ParentKey = null;
                }
            }

            foreach (var node in plan.Nodes)
            {
                var depth = 0;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { node.Key };
                var cur = node;
                var cyclic = false;
                while (cur.ParentKey != null && byKey.TryGetValue(cur.ParentKey, out var parent))
                {
                    if (!seen.Add(parent.Key))
                    {
                        cyclic = true;
                        break;
                    }
                    depth++;
                    cur = parent;
                    if (depth > 64) { cyclic = true; break; }
                }

                if (cyclic)
                {
                    plan.Add("nodes", node.SourceRow, node.Key, "PARENT_CYCLE", ImportIssueSeverity.Error,
                        $"Node '{node.Key}' nằm trong một vòng lặp cha–con.");
                    node.Depth = 0;
                }
                else
                {
                    node.Depth = depth;
                    if (depth + 1 > maxTreeDepth)
                        plan.Add("nodes", node.SourceRow, node.Key, "DEPTH_EXCEEDED", ImportIssueSeverity.Error,
                            $"Node '{node.Key}' ở độ sâu {depth + 1}, vượt giới hạn {maxTreeDepth}.");
                }
            }

            // ----- order children so parents are inserted first -----
            plan.Nodes.Sort((a, b) => a.Depth != b.Depth
                ? a.Depth.CompareTo(b.Depth)
                : a.SourceRow.CompareTo(b.SourceRow));

            // ----- assign a stable OrderIndex per sibling group when missing -----
            foreach (var group in plan.Nodes.GroupBy(n => n.ParentKey ?? "", StringComparer.OrdinalIgnoreCase))
            {
                var i = 0;
                foreach (var n in group.OrderBy(n => n.OrderIndex ?? int.MaxValue).ThenBy(n => n.SourceRow))
                    n.ResolvedOrderIndex = n.OrderIndex ?? i++;
            }

            plan.NodeKeys = byKey;
        }

        // ---------------------------------------------------------------
        //  blocks.csv
        // ---------------------------------------------------------------
        private static void ParseBlocks(string? csv, ImportPlan plan)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;
            if (!CheckHeaders("blocks", csv!, BlockHeaders, plan)) return;

            foreach (var r in CsvReader.Read(csv!))
            {
                var nodeKey = r.Get("NodeKey");
                if (string.IsNullOrWhiteSpace(nodeKey))
                {
                    plan.Add("blocks", r.RowNumber, null, "MISSING_NODE_KEY", ImportIssueSeverity.Error, "Thiếu NodeKey.");
                    continue;
                }

                var target = plan.NodeKeys.GetValueOrDefault(nodeKey);
                if (target == null)
                {
                    plan.Add("blocks", r.RowNumber, nodeKey, "BLOCK_NODE_NOT_FOUND", ImportIssueSeverity.Error,
                        $"NodeKey '{nodeKey}' không có trong nodes.csv.");
                    continue;
                }
                if (target.Type != NodeType.Lesson)
                    plan.Add("blocks", r.RowNumber, nodeKey, "BLOCK_NODE_NOT_LESSON", ImportIssueSeverity.Warning,
                        $"Node '{nodeKey}' là {target.Type}, không phải Lesson — block thường gắn vào bài học.");

                var block = new PlanBlock { NodeKey = nodeKey, SourceRow = r.RowNumber };

                var typeRaw = r.Get("BlockType");
                if (Enum.TryParse<LessonBlockType>(typeRaw, ignoreCase: true, out var blockType))
                    block.BlockType = blockType;
                else
                {
                    plan.Add("blocks", r.RowNumber, nodeKey, "BAD_BLOCK_TYPE", ImportIssueSeverity.Error,
                        $"BlockType không hợp lệ: '{typeRaw}'.");
                    continue;
                }

                block.ContentText = r.GetRaw("ContentText");
                block.ContentUrl = r.Get("ContentUrl");
                block.MetadataJson = r.GetRaw("MetadataJson").Trim();

                if (int.TryParse(r.Get("OrderIndex"), out var oi)) block.OrderIndex = oi;
                else block.OrderIndex = null;

                if (TextBlockTypes.Contains(blockType) && string.IsNullOrWhiteSpace(block.ContentText))
                    plan.Add("blocks", r.RowNumber, nodeKey, "BLOCK_TEXT_REQUIRED", ImportIssueSeverity.Error,
                        $"Block {blockType} cần ContentText.");

                if (MediaBlockTypes.Contains(blockType) && string.IsNullOrWhiteSpace(block.ContentUrl))
                    plan.Add("blocks", r.RowNumber, nodeKey, "BLOCK_URL_REQUIRED", ImportIssueSeverity.Warning,
                        $"Block {blockType} thường cần ContentUrl.");

                if (!string.IsNullOrEmpty(block.ContentUrl) && block.ContentUrl.Length > 500)
                    plan.Add("blocks", r.RowNumber, nodeKey, "BLOCK_URL_TOO_LONG", ImportIssueSeverity.Error,
                        "ContentUrl vượt quá 500 ký tự.");

                if (!string.IsNullOrEmpty(block.MetadataJson) && !IsJson(block.MetadataJson))
                    plan.Add("blocks", r.RowNumber, nodeKey, "BAD_METADATA_JSON", ImportIssueSeverity.Warning,
                        "MetadataJson không phải JSON hợp lệ — sẽ được lưu nguyên văn.");

                plan.Blocks.Add(block);
            }
        }

        // ---------------------------------------------------------------
        //  flashcards.csv
        // ---------------------------------------------------------------
        private static void ParseFlashcards(string? csv, ImportPlan plan)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;
            if (!CheckHeaders("flashcards", csv!, CardHeaders, plan)) return;

            // gộp theo (NodeKey, DeckTitle)
            var deckIndex = new Dictionary<string, PlanDeck>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in CsvReader.Read(csv!))
            {
                var nodeKey = r.Get("NodeKey");
                if (string.IsNullOrWhiteSpace(nodeKey))
                {
                    plan.Add("flashcards", r.RowNumber, null, "MISSING_NODE_KEY", ImportIssueSeverity.Error, "Thiếu NodeKey.");
                    continue;
                }
                if (!plan.NodeKeys.ContainsKey(nodeKey))
                {
                    plan.Add("flashcards", r.RowNumber, nodeKey, "CARD_NODE_NOT_FOUND", ImportIssueSeverity.Error,
                        $"NodeKey '{nodeKey}' không có trong nodes.csv.");
                    continue;
                }

                var deckTitle = r.Get("DeckTitle");
                if (string.IsNullOrWhiteSpace(deckTitle))
                {
                    plan.Add("flashcards", r.RowNumber, nodeKey, "DECK_TITLE_REQUIRED", ImportIssueSeverity.Error,
                        "Thiếu DeckTitle.");
                    continue;
                }
                if (deckTitle.Length > 255)
                {
                    plan.Add("flashcards", r.RowNumber, nodeKey, "DECK_TITLE_TOO_LONG", ImportIssueSeverity.Error,
                        "DeckTitle vượt quá 255 ký tự.");
                    continue;
                }

                var front = r.GetRaw("FrontText").Trim();
                var back = r.GetRaw("BackText").Trim();
                if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
                {
                    plan.Add("flashcards", r.RowNumber, nodeKey, "CARD_TEXT_REQUIRED", ImportIssueSeverity.Error,
                        "Thẻ cần cả FrontText và BackText.");
                    continue;
                }

                var deckKey = nodeKey + " " + deckTitle;
                if (!deckIndex.TryGetValue(deckKey, out var deck))
                {
                    deck = new PlanDeck { NodeKey = nodeKey, Title = deckTitle, SourceRow = r.RowNumber };
                    deckIndex[deckKey] = deck;
                    plan.Decks.Add(deck);
                }

                int.TryParse(r.Get("CardOrder"), out var cardOrder);
                deck.Cards.Add(new PlanCard
                {
                    Order = cardOrder,
                    Front = front,
                    Back = back,
                    Hint = string.IsNullOrWhiteSpace(r.GetRaw("Hint")) ? null : r.GetRaw("Hint").Trim(),
                    SourceRow = r.RowNumber
                });
            }
        }

        // ---------------------------------------------------------------
        //  resources.csv
        // ---------------------------------------------------------------
        private static void ParseResources(string? csv, ImportPlan plan)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;
            if (!CheckHeaders("resources", csv!, ResHeaders, plan)) return;

            foreach (var r in CsvReader.Read(csv!))
            {
                var nodeKey = r.Get("NodeKey");
                if (string.IsNullOrWhiteSpace(nodeKey))
                {
                    plan.Add("resources", r.RowNumber, null, "MISSING_NODE_KEY", ImportIssueSeverity.Error, "Thiếu NodeKey.");
                    continue;
                }
                if (!plan.NodeKeys.ContainsKey(nodeKey))
                {
                    plan.Add("resources", r.RowNumber, nodeKey, "RES_NODE_NOT_FOUND", ImportIssueSeverity.Error,
                        $"NodeKey '{nodeKey}' không có trong nodes.csv.");
                    continue;
                }

                var res = new PlanResource { NodeKey = nodeKey, SourceRow = r.RowNumber };

                res.Title = r.Get("Title");
                if (string.IsNullOrWhiteSpace(res.Title))
                    plan.Add("resources", r.RowNumber, nodeKey, "RES_TITLE_REQUIRED", ImportIssueSeverity.Error, "Thiếu Title.");
                else if (res.Title.Length > 255)
                    plan.Add("resources", r.RowNumber, nodeKey, "RES_TITLE_TOO_LONG", ImportIssueSeverity.Error,
                        "Title vượt quá 255 ký tự.");

                var typeRaw = r.Get("ResourceType");
                if (Enum.TryParse<ResourceType>(typeRaw, ignoreCase: true, out var resType))
                    res.ResourceType = resType;
                else
                {
                    plan.Add("resources", r.RowNumber, nodeKey, "BAD_RESOURCE_TYPE", ImportIssueSeverity.Error,
                        $"ResourceType không hợp lệ: '{typeRaw}'. Cho phép: Pdf, Slide, Doc, Sheet, ExternalLink.");
                    continue;
                }

                res.ExternalUrl = r.Get("ExternalUrl");
                if (string.IsNullOrWhiteSpace(res.ExternalUrl))
                    plan.Add("resources", r.RowNumber, nodeKey, "RES_URL_REQUIRED", ImportIssueSeverity.Error,
                        "Thiếu ExternalUrl (import chưa hỗ trợ tải file trực tiếp).");
                else if (res.ExternalUrl.Length > 1000)
                    plan.Add("resources", r.RowNumber, nodeKey, "RES_URL_TOO_LONG", ImportIssueSeverity.Error,
                        "ExternalUrl vượt quá 1000 ký tự.");

                res.IsDownloadable = ParseBool(r.Get("IsDownloadable"), "resources", r.RowNumber, nodeKey,
                    "IsDownloadable", plan, defaultValue: true);

                int.TryParse(r.Get("OrderIndex"), out var oi);
                res.OrderIndex = oi;

                plan.Resources.Add(res);
            }
        }

        // ---------------------------------------------------------------
        //  question-bank / questions / question-options / exercises / exercise-questions
        // ---------------------------------------------------------------
        private static void ParseAssessment(ImportPlan plan, string? bankCsv, string? questionsCsv,
            string? optionsCsv, string? exercisesCsv, string? exQuestionsCsv)
        {
            var hasAny = new[] { bankCsv, questionsCsv, optionsCsv, exercisesCsv, exQuestionsCsv }
                .Any(s => !string.IsNullOrWhiteSpace(s));
            if (!hasAny) return;

            // ----- bank -----
            if (string.IsNullOrWhiteSpace(questionsCsv) && string.IsNullOrWhiteSpace(exercisesCsv))
            {
                plan.Add("questions", null, null, "MISSING_FILE", ImportIssueSeverity.Error,
                    "Có file phần đánh giá nhưng thiếu cả questions.csv lẫn exercises.csv.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(bankCsv) && CheckHeaders("question-bank", bankCsv!, BankHeaders, plan))
            {
                var rows = CsvReader.Read(bankCsv!);
                if (rows.Count == 0)
                    plan.Add("question-bank", null, null, "EMPTY_FILE", ImportIssueSeverity.Error, "question-bank.csv không có dòng dữ liệu.");
                else
                {
                    var r = rows[0];
                    var key = r.Get("BankKey");
                    var name = r.Get("BankName");
                    if (string.IsNullOrWhiteSpace(key))
                        plan.Add("question-bank", 1, null, "BANK_KEY_REQUIRED", ImportIssueSeverity.Error, "Thiếu BankKey.");
                    if (string.IsNullOrWhiteSpace(name))
                        plan.Add("question-bank", 1, null, "BANK_NAME_REQUIRED", ImportIssueSeverity.Error, "Thiếu BankName.");
                    else if (name.Length > 255)
                        plan.Add("question-bank", 1, null, "BANK_NAME_TOO_LONG", ImportIssueSeverity.Error, "BankName vượt quá 255 ký tự.");
                    plan.Bank = new PlanBank { Key = key, Name = name, Description = r.GetRaw("Description").Trim() };
                    if (rows.Count > 1)
                        plan.Add("question-bank", 2, null, "BANK_EXTRA_ROWS", ImportIssueSeverity.Warning,
                            "question-bank.csv có nhiều hơn 1 dòng — chỉ dòng đầu được dùng.");
                }
            }

            ParseQuestions(plan, questionsCsv);
            ParseQuestionOptions(plan, optionsCsv);
            ParseExercises(plan, exercisesCsv);
            ParseExerciseQuestions(plan, exQuestionsCsv);

            // ----- cross-checks -----
            if ((plan.Questions.Count > 0 || plan.Exercises.Count > 0) && plan.Bank == null)
                plan.Add("question-bank", null, null, "MISSING_FILE", ImportIssueSeverity.Error,
                    "Có câu hỏi / bài tập nhưng thiếu question-bank.csv.");

            foreach (var q in plan.Questions.Values)
            {
                if (ChoiceQuestionTypes.Contains(q.Type))
                {
                    if (q.Options.Count < 2)
                        plan.Add("questions", q.SourceRow, q.Key, "OPTIONS_TOO_FEW", ImportIssueSeverity.Error,
                            $"Câu {q.Type} cần ít nhất 2 phương án trong question-options.csv.");
                    if (q.Options.Count > 0 && !q.Options.Any(o => o.IsCorrect))
                        plan.Add("questions", q.SourceRow, q.Key, "NO_CORRECT_OPTION", ImportIssueSeverity.Error,
                            "Không có phương án nào được đánh dấu đúng.");
                }
            }

            foreach (var ex in plan.Exercises.Values)
                if (ex.QuestionKeys.Count == 0)
                    plan.Add("exercises", ex.SourceRow, null, "EXERCISE_NO_QUESTIONS", ImportIssueSeverity.Error,
                        $"Bài tập '{ex.Key}' chưa gán câu hỏi nào trong exercise-questions.csv.");
        }

        private static void ParseQuestions(ImportPlan plan, string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;
            if (!CheckHeaders("questions", csv!, QuestionHeaders, plan)) return;

            foreach (var r in CsvReader.Read(csv!))
            {
                var key = r.Get("QuestionKey");
                if (string.IsNullOrWhiteSpace(key))
                {
                    plan.Add("questions", r.RowNumber, null, "QUESTION_KEY_REQUIRED", ImportIssueSeverity.Error, "Thiếu QuestionKey.");
                    continue;
                }
                if (plan.Questions.ContainsKey(key))
                {
                    plan.Add("questions", r.RowNumber, key, "DUP_QUESTION_KEY", ImportIssueSeverity.Error, $"QuestionKey '{key}' bị trùng.");
                    continue;
                }

                var q = new PlanQuestion { Key = key, SourceRow = r.RowNumber };

                if (Enum.TryParse<QuestionType>(r.Get("QuestionType"), true, out var qt)) q.Type = qt;
                else
                {
                    plan.Add("questions", r.RowNumber, key, "BAD_QUESTION_TYPE", ImportIssueSeverity.Error,
                        $"QuestionType không hợp lệ: '{r.Get("QuestionType")}'. Cho phép: MultipleChoice, TrueFalse, FillBlank, Essay.");
                    continue;
                }

                q.Difficulty = Enum.TryParse<DifficultyLevel>(r.Get("Difficulty"), true, out var d) ? d : DifficultyLevel.Medium;
                if (!string.IsNullOrWhiteSpace(r.Get("Difficulty")) && !Enum.TryParse<DifficultyLevel>(r.Get("Difficulty"), true, out _))
                    plan.Add("questions", r.RowNumber, key, "BAD_DIFFICULTY", ImportIssueSeverity.Warning,
                        $"Difficulty '{r.Get("Difficulty")}' không rõ — dùng Medium.");

                q.Text = r.GetRaw("QuestionText").Trim();
                if (string.IsNullOrWhiteSpace(q.Text))
                    plan.Add("questions", r.RowNumber, key, "QUESTION_TEXT_REQUIRED", ImportIssueSeverity.Error, "Thiếu QuestionText.");

                q.CorrectAnswer = r.GetRaw("CorrectAnswer").Trim();
                if (q.Type != QuestionType.Essay && string.IsNullOrWhiteSpace(q.CorrectAnswer)
                    && q.Type != QuestionType.MultipleChoice)
                    plan.Add("questions", r.RowNumber, key, "CORRECT_ANSWER_REQUIRED", ImportIssueSeverity.Error,
                        $"Câu {q.Type} cần CorrectAnswer.");

                q.Explanation = r.GetRaw("Explanation").Trim();

                var nodeKey = r.Get("NodeKey");
                if (!string.IsNullOrWhiteSpace(nodeKey))
                {
                    if (plan.NodeKeys.ContainsKey(nodeKey)) q.NodeKey = nodeKey;
                    else
                        plan.Add("questions", r.RowNumber, key, "QUESTION_NODE_NOT_FOUND", ImportIssueSeverity.Warning,
                            $"NodeKey '{nodeKey}' không có trong nodes.csv — câu hỏi sẽ không gắn vào node nào.");
                }

                if (!string.IsNullOrWhiteSpace(r.Get("BankKey")) && plan.Bank != null
                    && !string.Equals(r.Get("BankKey"), plan.Bank.Key, StringComparison.OrdinalIgnoreCase))
                    plan.Add("questions", r.RowNumber, key, "QUESTION_BANK_MISMATCH", ImportIssueSeverity.Warning,
                        $"BankKey '{r.Get("BankKey")}' khác với question-bank.csv ('{plan.Bank.Key}').");

                plan.Questions[key] = q;
            }
        }

        private static void ParseQuestionOptions(ImportPlan plan, string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;
            if (!CheckHeaders("question-options", csv!, OptionHeaders, plan)) return;

            foreach (var r in CsvReader.Read(csv!))
            {
                var qk = r.Get("QuestionKey");
                if (string.IsNullOrWhiteSpace(qk)) continue;
                if (!plan.Questions.TryGetValue(qk, out var q))
                {
                    plan.Add("question-options", r.RowNumber, qk, "OPTION_QUESTION_NOT_FOUND", ImportIssueSeverity.Error,
                        $"QuestionKey '{qk}' không có trong questions.csv.");
                    continue;
                }

                var text = r.GetRaw("OptionText").Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    plan.Add("question-options", r.RowNumber, qk, "OPTION_TEXT_REQUIRED", ImportIssueSeverity.Error, "Thiếu OptionText.");
                    continue;
                }

                int.TryParse(r.Get("OrderIndex"), out var oi);
                var isCorrect = ParseBool(r.Get("IsCorrect"), "question-options", r.RowNumber, qk, "IsCorrect", plan, false);
                q.Options.Add(new PlanOption { Order = oi, Text = text, IsCorrect = isCorrect, SourceRow = r.RowNumber });
            }
        }

        private static void ParseExercises(ImportPlan plan, string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;
            if (!CheckHeaders("exercises", csv!, ExerciseHeaders, plan)) return;

            foreach (var r in CsvReader.Read(csv!))
            {
                var key = r.Get("ExerciseKey");
                if (string.IsNullOrWhiteSpace(key))
                {
                    plan.Add("exercises", r.RowNumber, null, "EXERCISE_KEY_REQUIRED", ImportIssueSeverity.Error, "Thiếu ExerciseKey.");
                    continue;
                }
                if (plan.Exercises.ContainsKey(key))
                {
                    plan.Add("exercises", r.RowNumber, null, "DUP_EXERCISE_KEY", ImportIssueSeverity.Error, $"ExerciseKey '{key}' bị trùng.");
                    continue;
                }

                var ex = new PlanExercise { Key = key, SourceRow = r.RowNumber };

                ex.Name = r.Get("ExerciseName");
                if (string.IsNullOrWhiteSpace(ex.Name))
                    plan.Add("exercises", r.RowNumber, null, "EXERCISE_NAME_REQUIRED", ImportIssueSeverity.Error, "Thiếu ExerciseName.");
                else if (ex.Name.Length > 255)
                    plan.Add("exercises", r.RowNumber, null, "EXERCISE_NAME_TOO_LONG", ImportIssueSeverity.Error, "ExerciseName vượt quá 255 ký tự.");

                if (Enum.TryParse<ExerciseType>(r.Get("ExerciseType"), true, out var et)) ex.Type = et;
                else
                {
                    plan.Add("exercises", r.RowNumber, null, "BAD_EXERCISE_TYPE", ImportIssueSeverity.Error,
                        $"ExerciseType không hợp lệ: '{r.Get("ExerciseType")}'. Cho phép: Practice, Quiz, Test, Exam.");
                    continue;
                }

                if (Enum.TryParse<AccessTier>(r.Get("Tier"), true, out var tier)) ex.Tier = tier;
                else
                {
                    plan.Add("exercises", r.RowNumber, null, "BAD_TIER", ImportIssueSeverity.Error,
                        $"Tier không hợp lệ: '{r.Get("Tier")}'. Cho phép: Free, Standard, Premium.");
                    continue;
                }

                var nodeKey = r.Get("NodeKey");
                if (!string.IsNullOrWhiteSpace(nodeKey))
                {
                    if (plan.NodeKeys.ContainsKey(nodeKey)) ex.NodeKey = nodeKey;
                    else
                        plan.Add("exercises", r.RowNumber, nodeKey, "EXERCISE_NODE_NOT_FOUND", ImportIssueSeverity.Error,
                            $"NodeKey '{nodeKey}' không có trong nodes.csv.");
                }

                if (int.TryParse(r.Get("DurationMinutes"), out var dur) && dur > 0) ex.DurationMinutes = dur;
                if (int.TryParse(r.Get("MaxAttempts"), out var ma) && ma > 0) ex.MaxAttempts = ma;

                var pctRaw = r.Get("PassingPercent");
                if (string.IsNullOrWhiteSpace(pctRaw)) ex.PassingPercent = 50;
                else if (int.TryParse(pctRaw, out var pct) && pct is >= 0 and <= 100) ex.PassingPercent = pct;
                else
                    plan.Add("exercises", r.RowNumber, null, "BAD_PASSING_PERCENT", ImportIssueSeverity.Error,
                        $"PassingPercent phải là số 0–100: '{pctRaw}'.");

                plan.Exercises[key] = ex;
            }
        }

        private static void ParseExerciseQuestions(ImportPlan plan, string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;
            if (!CheckHeaders("exercise-questions", csv!, ExQuestionHeaders, plan)) return;

            foreach (var r in CsvReader.Read(csv!))
            {
                var ek = r.Get("ExerciseKey");
                var qk = r.Get("QuestionKey");
                if (string.IsNullOrWhiteSpace(ek) || string.IsNullOrWhiteSpace(qk)) continue;

                if (!plan.Exercises.TryGetValue(ek, out var ex))
                {
                    plan.Add("exercise-questions", r.RowNumber, null, "LINK_EXERCISE_NOT_FOUND", ImportIssueSeverity.Error,
                        $"ExerciseKey '{ek}' không có trong exercises.csv.");
                    continue;
                }
                if (!plan.Questions.ContainsKey(qk))
                {
                    plan.Add("exercise-questions", r.RowNumber, qk, "LINK_QUESTION_NOT_FOUND", ImportIssueSeverity.Error,
                        $"QuestionKey '{qk}' không có trong questions.csv.");
                    continue;
                }
                if (ex.QuestionKeys.Contains(qk))
                {
                    plan.Add("exercise-questions", r.RowNumber, qk, "DUP_LINK", ImportIssueSeverity.Warning,
                        $"Câu '{qk}' đã có trong bài tập '{ek}' — bỏ qua dòng lặp.");
                    continue;
                }

                var score = double.TryParse(r.Get("Score"), System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var s) && s > 0 ? s : 1.0;
                ex.QuestionKeys.Add(qk);
                ex.Scores.Add(score);
            }
        }

        // ---------------------------------------------------------------
        //  helpers
        // ---------------------------------------------------------------
        private static bool CheckHeaders(string file, string csv, string[] required, ImportPlan plan)
        {
            var headers = CsvReader.Headers(csv);
            if (headers.Count == 0)
            {
                plan.Add(file, null, null, "EMPTY_FILE", ImportIssueSeverity.Error, $"{file}.csv rỗng.");
                return false;
            }

            var set = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);
            var missing = required.Where(h => !set.Contains(h)).ToList();
            if (missing.Count > 0)
            {
                plan.Add(file, null, null, "MISSING_HEADER", ImportIssueSeverity.Error,
                    $"{file}.csv thiếu cột bắt buộc: {string.Join(", ", missing)}.");
                return false;
            }
            return true;
        }

        private static bool ParseBool(string raw, string file, int row, string? nodeKey, string column,
            ImportPlan plan, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "true" or "1" or "yes" or "y" or "x": return true;
                case "false" or "0" or "no" or "n": return false;
                default:
                    plan.Add(file, row, nodeKey, "BAD_BOOL", ImportIssueSeverity.Warning,
                        $"{column} không rõ giá trị '{raw}' — hiểu là {defaultValue}.");
                    return defaultValue;
            }
        }

        private static bool IsSlug(string s)
        {
            foreach (var c in s)
                if (!(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
                    return false;
            return s.Length > 0;
        }

        private static bool IsJson(string s)
        {
            try { using var _ = JsonDocument.Parse(s); return true; }
            catch { return false; }
        }
    }

    // ===================================================================
    //  Plan model — kết quả parse, dùng chung cho báo cáo & ghi CSDL
    // ===================================================================

    public sealed class ImportPlan
    {
        public CourseHeader? Course { get; set; }
        public List<PlanNode> Nodes { get; } = new();
        public List<PlanBlock> Blocks { get; } = new();
        public List<PlanDeck> Decks { get; } = new();
        public List<PlanResource> Resources { get; } = new();
        public List<ImportIssueDto> Issues { get; } = new();

        // ----- phần đánh giá -----
        public PlanBank? Bank { get; set; }
        public Dictionary<string, PlanQuestion> Questions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, PlanExercise> Exercises { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool HasAssessment => Bank != null || Questions.Count > 0 || Exercises.Count > 0;

        /// <summary>Map NodeKey → node, không phân biệt hoa/thường (gán trong lúc parse nodes).</summary>
        public Dictionary<string, PlanNode> NodeKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public int ErrorCount => Issues.Count(i => i.Severity == ImportIssueSeverity.Error);
        public int WarningCount => Issues.Count(i => i.Severity == ImportIssueSeverity.Warning);
        public bool HasErrors => ErrorCount > 0;

        public int TotalRows => Nodes.Count + Blocks.Count + Decks.Sum(d => d.Cards.Count) + Resources.Count
            + Questions.Count + Questions.Values.Sum(q => q.Options.Count)
            + Exercises.Count + Exercises.Values.Sum(e => e.QuestionKeys.Count);

        public void Add(string file, int? row, string? nodeKey, string code, ImportIssueSeverity severity, string message)
            => Issues.Add(new ImportIssueDto
            {
                File = file, Row = row, NodeKey = nodeKey, Code = code, Severity = severity, Message = message
            });
    }

    public sealed class CourseHeader
    {
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string GradeCode { get; set; } = "";
        public string FrameworkCode { get; set; } = "";
        public string FrameworkName { get; set; } = "";
        public string Publisher { get; set; } = "";
        public decimal ListPrice { get; set; }
        public string VersionLabel { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public sealed class PlanNode
    {
        public string Key { get; set; } = "";
        public string? ParentKey { get; set; }
        public NodeType Type { get; set; }
        public string Title { get; set; } = "";
        public string? Slug { get; set; }
        public int? OrderIndex { get; set; }
        public int ResolvedOrderIndex { get; set; }
        public bool IsFree { get; set; }
        public int? DurationMinutes { get; set; }
        public int Depth { get; set; }
        public int SourceRow { get; set; }

        /// <summary>NodeId thật sau khi ghi CSDL.</summary>
        public int PersistedId { get; set; }

        /// <summary>Thực thể EF tương ứng trong lúc commit (nội bộ service).</summary>
        public Data.Entities.ContentNode? Entity { get; set; }
    }

    public sealed class PlanBlock
    {
        public string NodeKey { get; set; } = "";
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public string? MetadataJson { get; set; }
        public int? OrderIndex { get; set; }
        public int SourceRow { get; set; }
    }

    public sealed class PlanDeck
    {
        public string NodeKey { get; set; } = "";
        public string Title { get; set; } = "";
        public int SourceRow { get; set; }
        public List<PlanCard> Cards { get; } = new();
    }

    public sealed class PlanCard
    {
        public int Order { get; set; }
        public string Front { get; set; } = "";
        public string Back { get; set; } = "";
        public string? Hint { get; set; }
        public int SourceRow { get; set; }
    }

    public sealed class PlanResource
    {
        public string NodeKey { get; set; } = "";
        public string Title { get; set; } = "";
        public ResourceType ResourceType { get; set; }
        public string ExternalUrl { get; set; } = "";
        public bool IsDownloadable { get; set; }
        public int OrderIndex { get; set; }
        public int SourceRow { get; set; }
    }

    public sealed class PlanBank
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int PersistedId { get; set; }
    }

    public sealed class PlanQuestion
    {
        public string Key { get; set; } = "";
        public string? NodeKey { get; set; }
        public QuestionType Type { get; set; }
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
        public string Text { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public string Explanation { get; set; } = "";
        public List<PlanOption> Options { get; } = new();
        public int SourceRow { get; set; }
        public int PersistedId { get; set; }
        public Data.Entities.Question? PersistedEntity { get; set; }
    }

    public sealed class PlanOption
    {
        public int Order { get; set; }
        public string Text { get; set; } = "";
        public bool IsCorrect { get; set; }
        public int SourceRow { get; set; }
    }

    public sealed class PlanExercise
    {
        public string Key { get; set; } = "";
        public string? NodeKey { get; set; }
        public string Name { get; set; } = "";
        public ExerciseType Type { get; set; }
        public AccessTier Tier { get; set; }
        public int? DurationMinutes { get; set; }
        public int? MaxAttempts { get; set; }
        public int PassingPercent { get; set; } = 50;
        public List<string> QuestionKeys { get; } = new();
        public List<double> Scores { get; } = new();
        public int SourceRow { get; set; }
        public Data.Entities.Exercise? PersistedEntity { get; set; }
    }
}
