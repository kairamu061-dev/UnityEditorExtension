using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Obelune.DataViewer.Editor.Spike1
{
    // 検証 1 用の単純な実装。見え方を確かめるためだけのもので、性能は作り込まない（docs/data-viewer/tasks.md）
    public sealed class SpikeModel
    {
        public readonly List<SpikeTable> Tables = new List<SpikeTable>();
        public readonly List<string> Problems = new List<string>();
        public double LoadAssetMs, ExtractMs, IndexMs;

        // (参照先テーブル, 行) → 参照元の (テーブル, 列, 行)
        readonly Dictionary<long, List<(int table, int column, int row)>> sources = new Dictionary<long, List<(int, int, int)>>();

        public SpikeTable Find(string path) => Tables.FirstOrDefault(t => t.Path == path);

        public IReadOnlyList<(int table, int column, int row)> GetSources(int table, int row)
            => sources.TryGetValue(((long)table << 32) | (uint)row, out var l) ? l : (IReadOnlyList<(int, int, int)>)Array.Empty<(int, int, int)>();

        public static SpikeModel Load(SpikeSettings settings)
        {
            var m = new SpikeModel();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var root = UnityEditor.AssetDatabase.LoadMainAssetAtPath(settings.assetPath);
            m.LoadAssetMs = sw.Elapsed.TotalMilliseconds;
            if (root == null) { m.Problems.Add($"アセットがない: {settings.assetPath}"); return m; }

            sw.Restart();
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            m.Discover(root, root.GetType(), "", 0, visited, null);
            m.ExtractMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            m.BuildIndex(settings);
            m.IndexMs = sw.Elapsed.TotalMilliseconds;
            return m;
        }

        // ---- テーブルの発見と取り出し（Unity のシリアライズの規則でフィールドを選ぶ） ----

        static IEnumerable<FieldInfo> SerializedFields(Type type)
        {
            var list = new List<FieldInfo>();
            for (var t = type; t != null && t != typeof(object) && t != typeof(ScriptableObject) && t != typeof(MonoBehaviour) && t != typeof(UnityEngine.Object); t = t.BaseType)
            {
                var fs = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                // 基底クラスのフィールドを先に並べる
                list.InsertRange(0, fs.Where(f =>
                    !f.IsInitOnly && !f.IsLiteral &&
                    !f.IsDefined(typeof(NonSerializedAttribute), false) &&
                    (f.IsPublic || f.IsDefined(typeof(SerializeField), false))));
            }
            return list;
        }

        static Type ElementType(Type t)
        {
            if (t.IsArray && t.GetArrayRank() == 1) return t.GetElementType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return t.GetGenericArguments()[0];
            return null;
        }

        static bool IsRowType(Type t)
            => t != null && !t.IsPrimitive && !t.IsEnum && t != typeof(string) && !typeof(UnityEngine.Object).IsAssignableFrom(t)
               && SerializedFields(t).Any();

        // group: ルートの直下のオブジェクトのうち、自分ではテーブルを持たないもの（データクラスをまとめる入れ物）。null = グループなし
        void Discover(object owner, Type ownerType, string prefix, int depth, HashSet<object> visited, string group)
        {
            if (owner == null || depth > 4) return;
            if (!ownerType.IsValueType && !visited.Add(owner)) return;

            var fields = SerializedFields(ownerType).ToList();
            var tableFields = fields.Where(f => IsRowType(ElementType(f.FieldType))).ToList();
            foreach (var f in fields)
            {
                var path = prefix.Length == 0 ? f.Name : prefix + "." + f.Name;
                var value = f.GetValue(owner);
                if (value == null) continue;
                var elem = ElementType(f.FieldType);
                if (IsRowType(elem))
                {
                    var name = tableFields.Count == 1 && depth > 0 ? TrimSuffix(ownerType.Name) : ownerType.Name + "." + f.Name;
                    var table = Extract(path, name, elem, (IList)value);
                    table.Group = group;
                    Tables.Add(table);
                }
                else if (!f.FieldType.IsPrimitive && !f.FieldType.IsEnum && f.FieldType != typeof(string) && elem == null
                         && !typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                {
                    var childGroup = group;
                    if (depth == 0 && !SerializedFields(f.FieldType).Any(ff => IsRowType(ElementType(ff.FieldType))))
                        childGroup = TrimSuffix(f.FieldType.Name);
                    Discover(value, f.FieldType, path, depth + 1, visited, childGroup);
                }
            }
        }

        // 末尾の Data・Collection・Table・Group を、なくなるまで繰り返し除く（CharacterAbilityCollectionData → CharacterAbility）
        static string TrimSuffix(string n)
        {
            var trimmed = true;
            while (trimmed)
            {
                trimmed = false;
                foreach (var s in new[] { "Data", "Collection", "Table", "Group" })
                    if (n.Length > s.Length && n.EndsWith(s, StringComparison.Ordinal)) { n = n.Substring(0, n.Length - s.Length); trimmed = true; }
            }
            return n;
        }

        static SpikeTable Extract(string path, string name, Type rowType, IList rows)
        {
            var t = new SpikeTable { Path = path, Name = name, RowCount = rows.Count };
            foreach (var f in SerializedFields(rowType))
            {
                var c = new SpikeColumn { Name = f.Name, Type = f.FieldType, Display = new string[rows.Count], Integers = new long[rows.Count] };
                c.IsInteger = IsIntegerLike(f.FieldType);
                for (int i = 0; i < rows.Count; i++)
                {
                    var v = f.GetValue(rows[i]);
                    if (c.IsInteger) c.Integers[i] = Convert.ToInt64(v, CultureInfo.InvariantCulture);
                    c.Display[i] = Format(v);
                }
                t.Columns.Add(c);
            }
            return t;
        }

        static bool IsIntegerLike(Type t)
            => t.IsEnum || t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) || t == typeof(uint) || t == typeof(ushort) || t == typeof(sbyte);

        static string Format(object v)
        {
            switch (v)
            {
                case null: return "";
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case IFormattable fm when !(v is Enum): return fm.ToString(null, CultureInfo.InvariantCulture);
                case IList l when !(v is string): return $"[{l.Count}]";
                default: return v.ToString();
            }
        }

        // ---- 主キー・参照・補助情報 ----

        void BuildIndex(SpikeSettings s)
        {
            foreach (var pk in s.primaryKeys)
            {
                var t = Find(pk.table);
                var ci = t?.Columns.FindIndex(c => c.Name == pk.column) ?? -1;
                if (t == null || ci < 0) { Problems.Add($"主キーの設定の先がない: {pk.table}.{pk.column}"); continue; }
                t.PrimaryKey = ci;
                var col = t.Columns[ci];
                for (int r = 0; r < t.RowCount; r++)
                {
                    var key = col.Display[r];
                    if (t.RowByKey.ContainsKey(key)) Problems.Add($"{t.Name}: 主キー {key} が重複");
                    else t.RowByKey[key] = r;
                }
            }

            foreach (var rs in s.references)
            {
                var t = Find(rs.table);
                var ci = t?.Columns.FindIndex(c => c.Name == rs.column) ?? -1;
                var target = Find(rs.target);
                if (t == null || ci < 0 || target == null || target.PrimaryKey < 0) { Problems.Add($"参照の設定の先がない: {rs.table}.{rs.column} → {rs.target}"); continue; }
                var col = t.Columns[ci];
                var ti = Tables.IndexOf(target);
                var tableIndex = Tables.IndexOf(t);
                col.RefTarget = ti;
                col.RefRow = new int[t.RowCount];
                for (int r = 0; r < t.RowCount; r++)
                {
                    var v = col.Display[r];
                    if (rs.hasNullValue && v == rs.nullValue) { col.RefRow[r] = -2; continue; }
                    if (target.RowByKey.TryGetValue(v, out var tr))
                    {
                        col.RefRow[r] = tr;
                        var key = ((long)ti << 32) | (uint)tr;
                        if (!sources.TryGetValue(key, out var list)) sources[key] = list = new List<(int, int, int)>();
                        list.Add((tableIndex, ci, r));
                    }
                    else
                    {
                        col.RefRow[r] = -1;
                        t.DanglingCount++;
                        Problems.Add($"{t.Name} の {r + 1} 行目: {col.Name} = {v} の参照先が {target.Name} にない");
                    }
                }
            }

            var sets = new Dictionary<string, Dictionary<string, (string label, string note)>>();
            foreach (var a in s.annotationSets)
            {
                if (!File.Exists(a.csv)) { Problems.Add($"補助情報の CSV がない: {a.csv}"); continue; }
                var map = new Dictionary<string, (string, string)>();
                foreach (var line in File.ReadAllLines(a.csv, Encoding.UTF8).Skip(1))
                {
                    if (line.Length == 0) continue;
                    var p = line.Split(',');
                    if (p.Length < 2) continue;
                    if (map.ContainsKey(p[0])) { Problems.Add($"{a.name}: 値 {p[0]} が重複"); continue; }
                    map[p[0]] = (p[1], p.Length > 2 ? p[2] : "");
                }
                sets[a.name] = map;
            }

            foreach (var b in s.annotationBindings)
            {
                var t = Find(b.table);
                if (t == null || !sets.TryGetValue(b.set, out var map)) { Problems.Add($"補助情報の結び付けの先がない: {b.set} → {b.table}.{b.column}"); continue; }
                if (string.IsNullOrEmpty(b.column))
                {
                    if (t.PrimaryKey < 0) { Problems.Add($"補助情報 {b.set}: {t.Name} に主キーがない"); continue; }
                    t.Columns[t.PrimaryKey].Annotation = map;
                    t.Columns[t.PrimaryKey].AnnotationName = b.set;
                    var ti = Tables.IndexOf(t);
                    // 主キーを指す参照の列にも伝える
                    foreach (var other in Tables)
                        foreach (var c in other.Columns)
                            if (c.RefTarget == ti && c.Annotation == null) { c.Annotation = map; c.AnnotationName = b.set; }
                }
                else
                {
                    var c = t.Columns.FirstOrDefault(x => x.Name == b.column);
                    if (c == null) { Problems.Add($"補助情報の結び付けの先がない: {b.set} → {b.table}.{b.column}"); continue; }
                    c.Annotation = map;
                    c.AnnotationName = b.set;
                }
            }
        }

        sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
        }
    }

    public sealed class SpikeTable
    {
        public string Path, Name;
        public string Group;            // null = グループなし
        public int RowCount;
        public int PrimaryKey = -1;
        public int DanglingCount;
        public readonly List<SpikeColumn> Columns = new List<SpikeColumn>();
        public readonly Dictionary<string, int> RowByKey = new Dictionary<string, int>();
    }

    public sealed class SpikeColumn
    {
        public string Name;
        public Type Type;
        public bool IsInteger;
        public long[] Integers;
        public string[] Display;
        public int RefTarget = -1;      // 参照先のテーブルの番号
        public int[] RefRow;            // -1 = 参照切れ、-2 = 参照なし
        public Dictionary<string, (string label, string note)> Annotation;
        public string AnnotationName;

        public bool TryGetLabel(int row, out string label, out string note)
        {
            label = note = null;
            if (Annotation == null || !Annotation.TryGetValue(Display[row], out var a)) return false;
            label = a.label; note = a.note;
            return true;
        }

        public string TypeName => Type.IsEnum ? Type.Name : Type == typeof(int) ? "int" : Type == typeof(float) ? "float" : Type == typeof(string) ? "string" : Type.Name;
    }
}
