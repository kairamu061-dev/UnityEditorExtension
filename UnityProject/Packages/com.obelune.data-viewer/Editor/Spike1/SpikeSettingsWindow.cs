using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Obelune.DataViewer.Editor.Spike1
{
    // 検証 2: 設定の画面を別のウィンドウで開く形（ビューアの中に置く形と見比べる）。検証が終わったら捨てる
    public sealed class SpikeSettingsWindow : EditorWindow
    {
        [SerializeField] SpikeSettingsView.Layout layout = SpikeSettingsView.Layout.ByTable;
        [SerializeField] List<string> openTables = new List<string>();

        SpikeSettingsView view;
        HashSet<string> openSet;

        [MenuItem("Tools/Obelune/Data Viewer/Spike1/Open Settings Window")]
        public static void Open() => GetWindow<SpikeSettingsWindow>("Data Viewer 設定 (Spike2)");

        void CreateGUI()
        {
            openSet = new HashSet<string>(openTables);
            view = new SpikeSettingsView(layout, openSet);
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
            layout = view.CurrentLayout;
            openTables = new List<string>(openSet);
        }
    }
}
