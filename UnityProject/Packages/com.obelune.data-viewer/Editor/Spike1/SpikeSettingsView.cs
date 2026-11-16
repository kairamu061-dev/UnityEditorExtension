using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obelune.DataViewer.Editor.Spike1
{
    // 検証 2: 設定の画面（docs/data-viewer/tasks.md）。ビューアの隣のタブ（SpikeSettingsWindow）に置く。検証が終わったら捨てる
    public sealed class SpikeSettingsView : VisualElement
    {
        const string None = "（なし）";

        SpikeSettings settings;
        SpikeModel model;
        readonly ScrollView scroll;
        readonly HashSet<string> openTables;   // 開いているテーブルのパス（作り直しても保つ）

        public SpikeSettingsView(HashSet<string> openTables)
        {
            this.openTables = openTables;
            style.flexGrow = 1;
            scroll = new ScrollView { style = { flexGrow = 1, paddingLeft = 8, paddingRight = 8, paddingTop = 6 } };
            Add(scroll);
        }

        // 設定とデータから画面を作り直す。スクロールの位置は保つ
        public void Rebuild(SpikeSettings settings, SpikeModel model)
        {
            this.settings = settings;
            this.model = model;
            var offset = scroll.scrollOffset;
            scroll.Clear();

            BuildSource();
            BuildMismatches();
            BuildAnnotationSets();

            BuildByTable();

            scroll.schedule.Execute(() => scroll.scrollOffset = offset);
        }

        void Save() => settings.Save();   // 保存すると SpikeSettings.Changed が呼ばれ、ウィンドウが読み直して Rebuild する

        static VisualElement Row() => new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };

        static Label Heading(string text) => new Label(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10, marginBottom = 4 } };

        static Label Muted(string text, float width = 0)
        {
            var l = new Label(text) { style = { color = new Color(0.6f, 0.6f, 0.6f), unityTextAlign = TextAnchor.MiddleLeft } };
            if (width > 0) l.style.width = width;
            return l;
        }

        // ---- ソース ----

        void BuildSource()
        {
            scroll.Add(Heading("ソース"));
            var row = Row();
            var current = string.IsNullOrEmpty(settings.assetPath) ? null : AssetDatabase.LoadMainAssetAtPath(settings.assetPath);
            var field = new ObjectField("アセット") { objectType = typeof(UnityEngine.Object), allowSceneObjects = false, value = current, style = { flexGrow = 1 } };
            field.RegisterValueChangedCallback(e =>
            {
                settings.sourceKind = "asset";
                settings.assetPath = e.newValue != null ? AssetDatabase.GetAssetPath(e.newValue) : "";
                Save();
            });
            row.Add(field);
            scroll.Add(row);
            if (model != null)
                scroll.Add(Muted($"見つかったテーブル: {model.Tables.Count}（{string.Join(", ", model.Tables.Select(t => t.Name))}）"));
        }

        // ---- 設定とデータの食い違い ----

        void BuildMismatches()
        {
            var items = new List<(string text, Action remove)>();
            foreach (var pk in settings.primaryKeys.ToList())
                if (FindColumn(pk.table, pk.column) == null)
                    items.Add(($"主キー {pk.table}.{pk.column}: テーブルか列がデータにない", () => settings.primaryKeys.Remove(pk)));
            foreach (var r in settings.references.ToList())
            {
                var col = FindColumn(r.table, r.column);
                var target = model?.Find(r.target);
                if (col == null || target == null)
                    items.Add(($"参照 {r.table}.{r.column} → {r.target}: テーブルか列がデータにない", () => settings.references.Remove(r)));
                else if (target.PrimaryKey < 0)
                    items.Add(($"参照 {r.table}.{r.column} → {r.target}: 参照先に主キーが設定されていない", null));
                else if (col.IsInteger != target.Columns[target.PrimaryKey].IsInteger)
                    items.Add(($"参照 {r.table}.{r.column} → {r.target}: 型の種類が合わない（{col.TypeName} と {target.Columns[target.PrimaryKey].TypeName}）", () => settings.references.Remove(r)));
            }
            foreach (var b in settings.annotationBindings.ToList())
            {
                var ok = string.IsNullOrEmpty(b.column) ? model?.Find(b.table) != null : FindColumn(b.table, b.column) != null;
                if (!ok) items.Add(($"名前の結び付け {b.set} → {b.table}.{b.column}: テーブルか列がデータにない", () => settings.annotationBindings.Remove(b)));
                else if (settings.annotationSets.All(s => s.name != b.set))
                    items.Add(($"名前の結び付け {b.set} → {b.table}.{b.column}: 名前の集まりがない", () => settings.annotationBindings.Remove(b)));
            }
            foreach (var a in settings.annotationSets)
                if (!string.IsNullOrEmpty(a.csv) && !System.IO.File.Exists(a.csv))
                    items.Add(($"名前の集まり {a.name}: CSV がない（{a.csv}）", null));

            if (items.Count == 0) return;
            scroll.Add(Heading($"⚠ 設定とデータの食い違い（{items.Count}）"));
            foreach (var (text, remove) in items)
            {
                var row = Row();
                row.Add(new Label(text) { style = { color = new Color(0.95f, 0.55f, 0.3f), flexGrow = 1, whiteSpace = WhiteSpace.Normal } });
                if (remove != null) row.Add(new Button(() => { remove(); Save(); }) { text = "この設定を消す" });
                scroll.Add(row);
            }
        }

        SpikeColumn FindColumn(string table, string column) => model?.Find(table)?.Columns.FirstOrDefault(c => c.Name == column);

        // ---- 名前の集まり（CSV） ----

        void BuildAnnotationSets()
        {
            scroll.Add(Heading("名前の集まり（CSV: value,label,note）"));
            foreach (var a in settings.annotationSets.ToList())
            {
                var set = a;
                var row = Row();
                var name = new TextField { value = set.name, isDelayed = true, style = { width = 160 } };
                name.RegisterValueChangedCallback(e =>
                {
                    var newName = (e.newValue ?? "").Trim();
                    if (newName.Length == 0 || settings.annotationSets.Any(s => s != set && s.name == newName)) { name.SetValueWithoutNotify(set.name); return; }
                    foreach (var b in settings.annotationBindings.Where(b => b.set == set.name)) b.set = newName;
                    set.name = newName;
                    Save();
                });
                row.Add(name);
                var csv = new ObjectField { objectType = typeof(TextAsset), allowSceneObjects = false, style = { flexGrow = 1 },
                    value = string.IsNullOrEmpty(set.csv) ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(set.csv) };
                csv.RegisterValueChangedCallback(e => { set.csv = e.newValue != null ? AssetDatabase.GetAssetPath(e.newValue) : ""; Save(); });
                row.Add(csv);
                var uses = settings.annotationBindings.Count(b => b.set == set.name);
                row.Add(Muted($"結び付け {uses}", 70));
                row.Add(new Button(() =>
                {
                    settings.annotationSets.Remove(set);
                    settings.annotationBindings.RemoveAll(b => b.set == set.name);
                    Save();
                }) { text = "×" });
                scroll.Add(row);
            }
            scroll.Add(new Button(() =>
            {
                var n = 1;
                while (settings.annotationSets.Any(s => s.name == $"新しい名前 {n}")) n++;
                settings.annotationSets.Add(new AnnotationSetSetting { name = $"新しい名前 {n}", csv = "" });
                Save();
            }) { text = "+ 名前の集まりを足す", style = { alignSelf = Align.FlexStart } });
        }

        List<string> SetChoices() => new[] { None }.Concat(settings.annotationSets.Select(s => s.name)).ToList();

        // ---- 参照の設定: テーブルごと ----

        void BuildByTable()
        {
            if (model == null || model.Tables.Count == 0) { scroll.Add(Muted("テーブルがない（ソースを選んでください）")); return; }
            foreach (var g in model.Tables.GroupBy(t => t.Group))
            {
                if (g.Key != null) scroll.Add(new Label(g.Key) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6, color = new Color(0.6f, 0.6f, 0.6f) } });
                foreach (var t in g) scroll.Add(TableSection(t));
            }
        }

        VisualElement TableSection(SpikeTable t)
        {
            var pkSetting = settings.primaryKeys.FirstOrDefault(p => p.table == t.Path);
            var refCount = settings.references.Count(r => r.table == t.Path);
            var fold = new Foldout
            {
                text = $"{t.Name}   主キー: {pkSetting?.column ?? "なし"}   参照: {refCount}",
                value = openTables.Contains(t.Path),
            };
            fold.RegisterValueChangedCallback(e =>
            {
                if (e.target != fold) return;
                if (e.newValue) openTables.Add(t.Path); else openTables.Remove(t.Path);
            });
            fold.Add(Muted(t.Path));

            // 主キー
            var pkRow = Row();
            pkRow.Add(Muted("主キー", 90));
            var pkChoices = new[] { None }.Concat(t.Columns.Where(c => c.IsKeyCandidate).Select(c => c.Name)).ToList();
            var pkField = new PopupField<string>(pkChoices, pkSetting != null && pkChoices.Contains(pkSetting.column) ? pkSetting.column : None) { style = { width = 160 } };
            pkField.RegisterValueChangedCallback(e =>
            {
                settings.primaryKeys.RemoveAll(p => p.table == t.Path);
                if (e.newValue != None) settings.primaryKeys.Add(new PrimaryKeySetting { table = t.Path, column = e.newValue });
                Save();
            });
            pkRow.Add(pkField);
            pkRow.Add(Muted("名前", 40));
            var pkBinding = settings.annotationBindings.FirstOrDefault(b => b.table == t.Path && string.IsNullOrEmpty(b.column));
            var setChoices = SetChoices();
            var pkSet = new PopupField<string>(setChoices, pkBinding != null && setChoices.Contains(pkBinding.set) ? pkBinding.set : None) { style = { width = 160 } };
            pkSet.SetEnabled(pkSetting != null);
            pkSet.tooltip = "主キーに結び付けた名前は、この主キーを指す参照の列にも出る";
            pkSet.RegisterValueChangedCallback(e =>
            {
                settings.annotationBindings.RemoveAll(b => b.table == t.Path && string.IsNullOrEmpty(b.column));
                if (e.newValue != None) settings.annotationBindings.Add(new AnnotationBinding { set = e.newValue, table = t.Path, column = "" });
                Save();
            });
            pkRow.Add(pkSet);
            fold.Add(pkRow);

            // 列ごと
            var header = Row();
            header.style.marginTop = 4;
            header.Add(Muted("列", 170));
            header.Add(Muted("参照先", 170));
            header.Add(Muted("参照なしの値", 90));
            header.Add(Muted("名前", 170));
            fold.Add(header);

            var targets = model.Tables.Where(x => x.PrimaryKey >= 0).ToList();
            foreach (var c in t.Columns)
            {
                var col = c;
                var row = Row();
                var isPk = t.PrimaryKey >= 0 && t.Columns[t.PrimaryKey] == col;
                row.Add(new Label((isPk ? "🔑 " : "") + $"{col.Name}  ({col.TypeName})") { style = { width = 170, unityTextAlign = TextAnchor.MiddleLeft } });

                var refSetting = settings.references.FirstOrDefault(r => r.table == t.Path && r.column == col.Name);
                if (isPk || !col.IsKeyCandidate)
                {
                    row.Add(Muted(isPk ? "（主キー）" : "（参照にできない型）", 170));
                    row.Add(Muted("", 90));
                }
                else
                {
                    // 型の種類が合う参照先だけを並べる
                    var compatible = targets.Where(x => x != t && x.Columns[x.PrimaryKey].IsInteger == col.IsInteger).ToList();
                    var choices = new[] { None }.Concat(compatible.Select(x => x.Path)).ToList();
                    var current = refSetting != null && choices.Contains(refSetting.target) ? refSetting.target : None;
                    var refField = new PopupField<string>(choices, current,
                        p => p == None ? None : model.Find(p)?.Name ?? p,
                        p => p == None ? None : model.Find(p)?.Name ?? p) { style = { width = 170 } };
                    refField.RegisterValueChangedCallback(e =>
                    {
                        var keep = settings.references.FirstOrDefault(r => r.table == t.Path && r.column == col.Name);
                        settings.references.RemoveAll(r => r.table == t.Path && r.column == col.Name);
                        if (e.newValue != None)
                            settings.references.Add(new ReferenceSetting
                            {
                                table = t.Path, column = col.Name, target = e.newValue,
                                hasNullValue = keep?.hasNullValue ?? col.IsInteger,
                                nullValue = keep?.nullValue ?? (col.IsInteger ? "0" : ""),
                            });
                        Save();
                    });
                    row.Add(refField);

                    var nullField = new TextField { value = refSetting != null && refSetting.hasNullValue ? refSetting.nullValue : "", isDelayed = true, style = { width = 90 } };
                    nullField.tooltip = "この値は「参照なし」とみなし、参照切れに数えない。空なら指定しない";
                    nullField.SetEnabled(refSetting != null);
                    nullField.RegisterValueChangedCallback(e =>
                    {
                        if (refSetting == null) return;
                        refSetting.hasNullValue = !string.IsNullOrEmpty(e.newValue);
                        refSetting.nullValue = e.newValue ?? "";
                        Save();
                    });
                    row.Add(nullField);
                }

                // 名前: 参照の列は参照先の主キーから引き継ぐ。区分の列などは直接結び付ける
                if (isPk)
                {
                    row.Add(Muted("（上の「名前」）", 170));
                }
                else if (col.RefTarget >= 0 && col.Annotation != null && settings.annotationBindings.All(b => !(b.table == t.Path && b.column == col.Name)))
                {
                    row.Add(Muted($"（参照先から: {col.AnnotationName}）", 170));
                }
                else
                {
                    var binding = settings.annotationBindings.FirstOrDefault(b => b.table == t.Path && b.column == col.Name);
                    var setField = new PopupField<string>(setChoices, binding != null && setChoices.Contains(binding.set) ? binding.set : None) { style = { width = 170 } };
                    setField.RegisterValueChangedCallback(e =>
                    {
                        settings.annotationBindings.RemoveAll(b => b.table == t.Path && b.column == col.Name);
                        if (e.newValue != None) settings.annotationBindings.Add(new AnnotationBinding { set = e.newValue, table = t.Path, column = col.Name });
                        Save();
                    });
                    row.Add(setField);
                }
                fold.Add(row);
            }
            return fold;
        }
    }
}
