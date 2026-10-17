using FDMi.core.Editor.Application.UseCases;
using UnityEngine;

namespace FDMi.input.Editor
{
    /// <summary>
    /// シーン内の全 FDMiHandInputGroup について、ハンドディスプレイの焼上が未達・陳腐化なら再構築する。
    /// FDMiEditorHooks (ビルド前 / Play 遷移前 / FDMi/Resolve All Data Paths メニュー) から
    /// ExecuteAll が呼ばれ、FDMiBehaviourEditor 系の選択時自動セットアップから Execute が呼ばれる。
    /// </summary>
    public class BuildHandInputDisplaysUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(UnityEngine.Object target)
        {
            if (target is FDMiHandInputGroup group)
                FDMiHandInputDisplayBuilder.Ensure(group);
        }

        public void ExecuteAll()
        {
            // シート配下など Editor 上で非アクティブのままグループに焼く必要があるため、
            // 既存ユースケースの探索 (active のみ) と異なり非アクティブを含める。
            var groups = UnityEngine.Object.FindObjectsByType<FDMiHandInputGroup>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var group in groups)
                FDMiHandInputDisplayBuilder.Ensure(group);
        }
    }
}
