using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Obelune.DataViewer.Editor.Spike1
{
    // 検証 2: 設定の画面。ビューアの隣のタブとして開く（SpikeWindow.Open が一緒に開く）。検証が終わったら捨てる
    public sealed class SpikeSettingsWindow : EditorWindow
    {
        [SerializeField] List<string> openTables = new List<string>();

        SpikeSettingsView view;
        HashSet<string> openSet;

        [MenuItem("Tools/Obelune/Data Viewer/Spike1/Open Settings Tab")]
        // ビューアのウィンドウがあれば、その隣のタブとして開く（Unity のドッキングのタブ）
        public static void Open() => GetWindow<SpikeSettingsWindow>("Data Viewer 設定 (Spike2)", true, typeof(SpikeWindow));

        void CreateGUI()
        {
            openSet = new HashSet<string>(openTables);
            view = new SpikeSettingsView(openSet);
            rootVisualElement.Add(view);
            SpikeSettings.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => SpikeSettings.Changed -= Refresh;

        void Refresh()
        {
            if (view == null) return;
            var settings = SpikeSettings.Load();
            view.Rebuild(settings, SpikeModel.Load(settings));
            openTables = new List<string>(openSet);
        }
    }
}
