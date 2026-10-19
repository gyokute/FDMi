using FDMi.sync.Editor.Application.UseCases;
using UnityEditor;

namespace FDMi.sync.Editor.Hooks
{
    static class ResolveSyncWiringMenu
    {
        [MenuItem("FDMi/Sync/Resolve Sync Wiring")]
        static void ResolveSyncWiring() => new ResolveSyncWiringUseCase().ExecuteAll();
    }
}
