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

        public static SpikeSettings Load()
        {
            if (!File.Exists(Path)) return null;
            return JsonUtility.FromJson<SpikeSettings>(File.ReadAllText(Path));
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
