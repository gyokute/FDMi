using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FDMi.core.Editor.Preview
{
    // プレビュー中にシーン保存・Play 移行・ドメインリロードが起きても、
    // 書き換えた Transform を残さないためのフック。
    [InitializeOnLoad]
    static class FDMiPreviewSessionHooks
    {
        static FDMiPreviewSessionHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                FDMiPreviewSession.Stop();
        }

        static void OnSceneSaving(Scene scene, string path)
        {
            FDMiPreviewSession.Stop();
        }

        static void OnBeforeAssemblyReload()
        {
            FDMiPreviewSession.Stop();
        }
    }
}
