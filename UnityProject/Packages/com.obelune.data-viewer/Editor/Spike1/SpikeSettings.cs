using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Obelune.DataViewer.Editor.Spike1
{
    // 検証 1 用。形は docs/data-viewer/settings/design.md と annotations/design.md に合わせ、
    // 補助情報の集まり（annotationSets）だけを足してある
    [Serializable]
    public sealed class SpikeSettings
    {
        public const string Path = "ProjectSettings/Obelune.DataViewer.json";

        public string sourceKind;
        public string assetPath;
        public List<PrimaryKeySetting> primaryKeys = new List<PrimaryKeySetting>();
        public List<ReferenceSetting> references = new List<ReferenceSetting>();
        public List<AnnotationSetSetting> annotationSets = new List<AnnotationSetSetting>();
        public List<AnnotationBinding> annotationBindings = new List<AnnotationBinding>();

        // 設定が保存されたときに呼ばれる。開いているウィンドウはこれを受けて読み直す
        public static event Action Changed;

        public static SpikeSettings Load()
        {
            if (!File.Exists(Path)) return new SpikeSettings { sourceKind = "asset", assetPath = "" };
            return JsonUtility.FromJson<SpikeSettings>(File.ReadAllText(Path));
        }

        public void Save()
        {
            // 差分が読みやすいよう、並びを固定してから書く
            primaryKeys.Sort((a, b) => string.CompareOrdinal(a.table + "\n" + a.column, b.table + "\n" + b.column));
            references.Sort((a, b) => string.CompareOrdinal(a.table + "\n" + a.column, b.table + "\n" + b.column));
            annotationSets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            annotationBindings.Sort((a, b) => string.CompareOrdinal(a.table + "\n" + a.column + "\n" + a.set, b.table + "\n" + b.column + "\n" + b.set));
            File.WriteAllText(Path, JsonUtility.ToJson(this, true) + "\n");
            Changed?.Invoke();
        }
    }

    [Serializable] public sealed class PrimaryKeySetting { public string table; public string column; }

    [Serializable]
    public sealed class ReferenceSetting
    {
        public string table;
        public string column;
        public string target;
        public bool hasNullValue;
        public string nullValue;
    }

    [Serializable] public sealed class AnnotationSetSetting { public string name; public string csv; }

    [Serializable] public sealed class AnnotationBinding { public string set; public string table; public string column; }
}
