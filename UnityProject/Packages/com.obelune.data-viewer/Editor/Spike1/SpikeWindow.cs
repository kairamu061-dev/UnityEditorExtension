using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obelune.DataViewer.Editor.Spike1
{
    // 検証 1: 見え方を確かめるためのウィンドウ（docs/data-viewer/tasks.md）。検証が終わったら捨てる
    public sealed class SpikeWindow : EditorWindow
    {
        // ドメインリロードを越えて保つ状態
        [SerializeField] string tablePath;
        [SerializeField] int selectedRow = -1;
        [SerializeField] string search = "";
        [SerializeField] bool searchLabels = true;
        [SerializeField] List<string> expandedGroups = new List<string>();   // 既定は全部畳む

        SpikeModel model;
        SpikeTable table;
        readonly List<int> items = new List<int>();
        readonly List<(string table, int row)> back = new List<(string, int)>();
        readonly List<(string table, int row)> forward = new List<(string, int)>();

        ScrollView tableList;
        readonly Dictionary<string, Label> tableLabels = new Dictionary<string, Label>();
        readonly Dictionary<string, Foldout> groupFoldouts = new Dictionary<string, Foldout>();
        MultiColumnListView rowList;
        ScrollView detail;
        Label status;
        Label rowCount;
        bool suppressSelection;
        bool detailWidthSet;

        static readonly Color DanglingColor = new Color(0.95f, 0.35f, 0.35f);
        static readonly Color SelectedBackground = new Color(0.24f, 0.48f, 0.90f, 0.45f);

        [MenuItem("Tools/Obelune/Data Viewer/Spike1/Open Window")]
        public static void Open() => GetWindow<SpikeWindow>("Data Viewer (Spike1)");

        void CreateGUI()
        {
            var root = rootVisualElement;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(Reload) { text = "再読込" });
            toolbar.Add(new ToolbarButton(GoBack) { text = "◀ 戻る" });
            toolbar.Add(new ToolbarButton(GoForward) { text = "進む ▶" });
            toolbar.Add(new ToolbarSpacer { flex = true });
            var searchField = new ToolbarSearchField { value = search, style = { width = 240 } };
            searchField.RegisterValueChangedCallback(e => { search = e.newValue ?? ""; ApplySearch(); });
            toolbar.Add(searchField);
            // オンとオフが見て分かるように、チェックボックスにする
            var labelsToggle = new Toggle { text = "名前も検索する", value = searchLabels, style = { marginLeft = 6, marginRight = 6, alignSelf = Align.Center } };
            labelsToggle.RegisterValueChangedCallback(e => { searchLabels = e.newValue; ApplySearch(); });
            toolbar.Add(labelsToggle);
            root.Add(toolbar);

            var outer = new TwoPaneSplitView(0, 220, TwoPaneSplitViewOrientation.Horizontal) { style = { flexGrow = 1 } };
            var inner = new TwoPaneSplitView(1, 480, TwoPaneSplitViewOrientation.Horizontal);
            // 行の詳細を、最初から行の一覧と半分ずつにする（あとは利用者が境目を動かせる）
            inner.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (detailWidthSet || e.newRect.width < 100) return;
                detailWidthSet = true;
                inner.fixedPane.style.width = e.newRect.width / 2;
            });

            tableList = new ScrollView { style = { paddingTop = 2 } };

            var center = new VisualElement { style = { flexGrow = 1 } };
            rowCount = new Label { style = { paddingLeft = 6, paddingTop = 2, paddingBottom = 2 } };
            rowList = new MultiColumnListView
            {
                fixedItemHeight = 18,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.Single,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                style = { flexGrow = 1 },
            };
            rowList.selectionChanged += _ =>
            {
                if (suppressSelection || rowList.selectedIndex < 0) return;
                var row = items[rowList.selectedIndex];
                if (row == selectedRow) return;
                Record();
                selectedRow = row;
                ShowDetail();
            };
            center.Add(rowCount);
            center.Add(rowList);

            detail = new ScrollView { style = { paddingLeft = 6, paddingRight = 6, paddingTop = 4 } };

            inner.Add(center);
            inner.Add(detail);
            outer.Add(tableList);
            outer.Add(inner);
            root.Add(outer);

            status = new Label { style = { paddingLeft = 6, paddingTop = 2, paddingBottom = 2, borderTopWidth = 1, borderTopColor = new Color(0, 0, 0, 0.3f) } };
            root.Add(status);

            Reload();
        }

        void Reload()
        {
            var settings = SpikeSettings.Load();
            if (settings == null)
            {
                status.text = $"設定がない: {SpikeSettings.Path}";
                return;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            model = SpikeModel.Load(settings);
            var total = sw.Elapsed.TotalMilliseconds;
            status.text = $"アセットの読み込み {model.LoadAssetMs:F0} ms（読み込み済みなら 0） ・ 取り出し {model.ExtractMs:F0} ms ・ 索引と名前 {model.IndexMs:F0} ms ・ 合計 {total:F0} ms" +
                          $"（{model.Tables.Count} テーブル, {model.Tables.Sum(t => t.RowCount):N0} 行）" +
                          (model.Problems.Count > 0 ? $"   ⚠ 問題 {model.Problems.Count}" : "");
            status.tooltip = string.Join("\n", model.Problems.Take(30));

            BuildTableList();
            var path = model.Find(tablePath) != null ? tablePath : model.Tables.FirstOrDefault()?.Path;
            if (path != null) SelectTable(path, selectedRow, record: false);
        }

        // ---- テーブルの一覧（グループごとに畳める） ----

        void BuildTableList()
        {
            tableList.Clear();
            tableLabels.Clear();
            groupFoldouts.Clear();

            // グループのないテーブルを先に、グループは見つけた順に
            foreach (var t in model.Tables.Where(t => t.Group == null))
                tableList.Add(MakeTableItem(t, indent: 6));

            foreach (var g in model.Tables.Where(t => t.Group != null).GroupBy(t => t.Group))
            {
                var name = g.Key;
                var dangling = g.Sum(t => t.DanglingCount);
                var fold = new Foldout
                {
                    text = dangling > 0 ? $"{name}  ⚠ {dangling}" : name,
                    value = expandedGroups.Contains(name),
                };
                fold.RegisterValueChangedCallback(e =>
                {
                    if (e.target != fold) return;
                    if (e.newValue) { if (!expandedGroups.Contains(name)) expandedGroups.Add(name); }
                    else expandedGroups.Remove(name);
                });
                foreach (var t in g) fold.Add(MakeTableItem(t, indent: 0));
                groupFoldouts[name] = fold;
                tableList.Add(fold);
            }
        }

        VisualElement MakeTableItem(SpikeTable t, int indent)
        {
            var l = new Label(t.DanglingCount > 0 ? $"{t.Name}  ({t.RowCount:N0})  ⚠ {t.DanglingCount}" : $"{t.Name}  ({t.RowCount:N0})")
            {
                tooltip = t.Path,
                style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = indent, height = 20 },
            };
            l.RegisterCallback<ClickEvent>(_ => SelectTable(t.Path, -1, record: true));
            tableLabels[t.Path] = l;
            return l;
        }

        void HighlightTable()
        {
            foreach (var kv in tableLabels)
                kv.Value.style.backgroundColor = kv.Key == tablePath ? new StyleColor(SelectedBackground) : new StyleColor(StyleKeyword.Null);
        }

        // ---- 移動 ----

        public void Reveal(string path, int row) => SelectTable(path, row, record: true, expandGroup: true);

        void SelectTable(string path, int row, bool record, bool expandGroup = false)
        {
            if (record && table != null && selectedRow >= 0) Record();
            var t = model.Find(path);
            if (t == null) return;
            var changed = t != table;
            table = t;
            tablePath = path;
            selectedRow = row;

            // 参照・戻る・進むで畳まれたグループのテーブルへ移ったときは、そのグループだけを開いて選択を見せる。
            // 読み込み直後の選択では開かない（既定は全部畳む）
            if (expandGroup && t.Group != null && groupFoldouts.TryGetValue(t.Group, out var fold) && !fold.value) fold.value = true;
            HighlightTable();

            if (changed) BuildColumns();
            ApplySearch();
            ShowDetail();
        }

        void Record()
        {
            if (table == null) return;
            back.Add((table.Path, selectedRow));
            forward.Clear();
        }

        void GoBack()
        {
            if (back.Count == 0) return;
            var p = back[back.Count - 1];
            back.RemoveAt(back.Count - 1);
            forward.Add((tablePath, selectedRow));
            SelectTable(p.table, p.row, record: false, expandGroup: true);
        }

        void GoForward()
        {
            if (forward.Count == 0) return;
            var p = forward[forward.Count - 1];
            forward.RemoveAt(forward.Count - 1);
            back.Add((tablePath, selectedRow));
            SelectTable(p.table, p.row, record: false, expandGroup: true);
        }

        // ---- 行の一覧 ----

        static string WithLabel(string value, string label) => $"{value}（{label}）";

        void BuildColumns()
        {
            if (table == null) return;
            rowList.columns.Clear();
            foreach (var c in table.Columns)
            {
                var col = c;
                var isKey = table.PrimaryKey >= 0 && table.Columns[table.PrimaryKey] == col;
                var refName = col.RefTarget >= 0 ? " → " + model.Tables[col.RefTarget].Name : "";
                rowList.columns.Add(new Column
                {
                    name = col.Name,
                    title = (isKey ? "🔑 " : "") + col.Name + refName,
                    width = col.Annotation != null ? 200 : 110,
                    resizable = true,
                    makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 } },
                    bindCell = (e, i) => BindCell((Label)e, col, items[i]),
                });
            }
            rowList.Rebuild();
        }

        void BindCell(Label l, SpikeColumn col, int row)
        {
            // セルは使い回されるので、設定する値はすべて毎回上書きする
            var value = col.Display[row];
            var hasLabel = col.TryGetLabel(row, out var label, out var note);
            var dangling = col.RefRow != null && col.RefRow[row] == -1;

            l.text = hasLabel ? WithLabel(value, label) : value;
            l.tooltip = dangling ? "参照先が見つからない" : hasLabel && !string.IsNullOrEmpty(note) ? note : "";
            l.style.color = dangling ? new StyleColor(DanglingColor) : new StyleColor(StyleKeyword.Null);
        }

        void ApplySearch()
        {
            if (table == null) return;
            items.Clear();
            var q = search.Trim();
            if (q.Length == 0)
            {
                for (int r = 0; r < table.RowCount; r++) items.Add(r);
            }
            else
            {
                for (int r = 0; r < table.RowCount; r++)
                {
                    foreach (var c in table.Columns)
                    {
                        if (c.Display[r].IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                            || (searchLabels && c.TryGetLabel(r, out var label, out _) && label.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            items.Add(r);
                            break;
                        }
                    }
                }
            }
            rowList.itemsSource = items;
            rowList.RefreshItems();
            var title = table.Group != null ? $"{table.Group} / {table.Name}" : table.Name;
            rowCount.text = q.Length == 0
                ? $"{title}（{table.Path}）  {table.RowCount:N0} 行"
                : $"{title}（{table.Path}）  {table.RowCount:N0} 行中 {items.Count:N0} 行（{(searchLabels ? "値と名前" : "値だけ")}を検索）";

            var index = selectedRow >= 0 ? items.IndexOf(selectedRow) : -1;
            suppressSelection = true;
            if (index >= 0)
            {
                rowList.SetSelectionWithoutNotify(new[] { index });
                rowList.ScrollToItem(index);
            }
            else rowList.ClearSelection();
            suppressSelection = false;
        }

        // ---- 行の詳細 ----

        void ShowDetail()
        {
            detail.Clear();
            if (table == null || selectedRow < 0 || selectedRow >= table.RowCount)
            {
                detail.Add(new Label("行を選ぶと、ここに詳細が出る"));
                return;
            }
            var ti = model.Tables.IndexOf(table);
            detail.Add(new Label($"{table.Name}  {selectedRow + 1} 行目") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });

            foreach (var c in table.Columns)
            {
                var line = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 1 } };
                line.Add(new Label(c.Name) { style = { width = 120, color = new Color(0.6f, 0.6f, 0.6f) } });
                var value = c.Display[selectedRow];
                var hasLabel = c.TryGetLabel(selectedRow, out var label, out var note);
                var text = hasLabel ? WithLabel(value, label) : value;
                if (c.RefRow != null && c.RefRow[selectedRow] >= 0)
                {
                    var target = model.Tables[c.RefTarget];
                    var targetRow = c.RefRow[selectedRow];
                    line.Add(new Button(() => Reveal(target.Path, targetRow)) { text = $"→ {target.Name}: {text}" });
                }
                else if (c.RefRow != null && c.RefRow[selectedRow] == -1)
                {
                    line.Add(new Label($"{text}  ⚠ 参照先がない") { style = { color = DanglingColor } });
                }
                else
                {
                    var l = new Label(text);
                    if (hasLabel && !string.IsNullOrEmpty(note)) l.tooltip = note;
                    line.Add(l);
                }
                detail.Add(line);
            }

            var sources = model.GetSources(ti, selectedRow);
            detail.Add(new Label($"参照元（{sources.Count} 件）") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8, marginBottom = 2 } });
            foreach (var g in sources.GroupBy(s => (s.table, s.column)))
            {
                var st = model.Tables[g.Key.table];
                var fold = new Foldout { text = $"{st.Name}.{st.Columns[g.Key.column].Name}  {g.Count()} 件", value = g.Count() <= 10 };
                foreach (var s in g.Take(50))
                {
                    var src = s;
                    var desc = string.Join(", ", st.Columns.Where((_, k) => k != src.column).Select(c =>
                        c.TryGetLabel(src.row, out var lb, out _) ? $"{c.Name}={WithLabel(c.Display[src.row], lb)}" : $"{c.Name}={c.Display[src.row]}"));
                    fold.Add(new Button(() => Reveal(st.Path, src.row)) { text = $"{src.row + 1} 行目: {desc}", style = { unityTextAlign = TextAnchor.MiddleLeft } });
                }
                if (g.Count() > 50) fold.Add(new Label($"…ほか {g.Count() - 50} 件"));
                detail.Add(fold);
            }
        }
    }
}
