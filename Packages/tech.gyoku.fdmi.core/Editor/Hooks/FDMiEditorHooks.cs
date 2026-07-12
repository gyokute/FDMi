using System;
using System.Linq;
using System.Reflection;
using FDMi.core.Editor.Application.UseCases;
using FDMi.core.Editor.Infrastructure.Repositories;
using UnityEditor;
using UnityEngine;

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
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ResolveAll();
        }

        [MenuItem("FDMi/Resolve All Data Paths")]
        static void ResolveAllMenu() => ResolveAll();

        internal static void ResolveAll()
        {
            // 全てのAssemblyから、IFDMiAutoSetupUseCase を実装するクラスを探して ExecuteAll() を呼び出す
            var useCaseTypes = AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t =>
                    typeof(IFDMiAutoSetupUseCase).IsAssignableFrom(t)
                    && !t.IsAbstract
                    && t.GetConstructor(Type.EmptyTypes) != null
                );

            // var useCase = new ResolveDataPathsUseCase(new SceneFDMiDataRepository());
            foreach (var useCaseType in useCaseTypes)
            {
                var useCase = (IFDMiAutoSetupUseCase)Activator.CreateInstance(useCaseType);
                useCase.ExecuteAll();
            }
        }
    }
}
