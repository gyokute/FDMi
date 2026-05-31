using UnityEditor;
using UnityEngine;
using tech.gyoku.FDMi.core.Editor.Application.UseCases;
using tech.gyoku.FDMi.core.Editor.Infrastructure.Repositories;

namespace tech.gyoku.FDMi.core.Editor.Hooks
{
    /// <summary>
    /// Menu・Play モード開始前の FDMiDataPath 解決トリガーを登録する静的フック。
    /// [InitializeOnLoad] により Unity Editor 起動時に自動登録される。
    /// </summary>
    [InitializeOnLoad]
    static class FDMiEditorHooks
    {
        static FDMiEditorHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Play モード状態変化ハンドラ。ExitingEditMode 時に全 MonoBehaviour を解決する。
        /// </summary>
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ResolveAll();
        }

        /// <summary>
        /// メニュー "FDMi/Resolve All Data Paths" から手動で解決を実行する。
        /// </summary>
        [MenuItem("FDMi/Resolve All Data Paths")]
        static void ResolveAllMenu() => ResolveAll();

        /// <summary>
        /// シーン上のすべての MonoBehaviour に対して FDMiDataPath を解決する。
        /// FDMiBuildCallback からも呼び出される。
        /// </summary>
        internal static void ResolveAll()
        {
            var useCase = new ResolveDataPathsUseCase(new SceneFDMiDataRepository());
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                useCase.Execute(mb);
        }
    }
}
