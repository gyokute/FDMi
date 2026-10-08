using UnityEditor;
using UnityEngine;

namespace FDMi.sync.Editor
{
    [InitializeOnLoad]
    public static class FDMiSyncEditorHooks
    {
        static FDMiSyncEditorHooks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                new FDMiSyncAutoSetup().ExecuteAll();
        }

        [MenuItem("FDMi/Sync/Resolve Sync Wiring")]
        public static void ResolveAll()
        {
            new FDMiSyncAutoSetup().ExecuteAll();
        }
    }
}
