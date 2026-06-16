using UnityEditor;
using UnityEngine;
using FDMi.core.Editor.Application.UseCases;
using FDMi.core.Editor.Infrastructure.Repositories;

namespace FDMi.core.Editor.Hooks
{
    [InitializeOnLoad]
    static class FDMiEditorHooks
    {
        // OnHierarchyChanged でスケジュール済みの delayCall があるかどうかを追跡する
        static bool _pendingResolve;

        static FDMiEditorHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged     += OnHierarchyChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ResolveAndRegisterAll();
        }

        // hierarchyChanged は短時間に複数回発火するため delayCall で集約する
        static void OnHierarchyChanged()
        {
            if (_pendingResolve) return;
            _pendingResolve = true;
            EditorApplication.delayCall += () =>
            {
                _pendingResolve = false;
                ResolveAndRegisterAll();
            };
        }

        [MenuItem("FDMi/Resolve All Data Paths")]
        static void ResolveAllMenu() => ResolveAndRegisterAll();

        internal static void ResolveAll()
        {
            var useCase = new ResolveDataPathsUseCase(new SceneFDMiDataRepository());
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                useCase.Execute(mb);
        }

        internal static void ResolveAndRegisterAll()
        {
            ResolveAll();
            RegisterCallbacksUseCase.RegisterAll();
        }
    }
}
