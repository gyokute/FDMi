using UnityEditor;
using UnityEngine;
using tech.gyoku.FDMi.core.Editor.Application.UseCases;
using tech.gyoku.FDMi.core.Editor.Infrastructure.Repositories;

namespace tech.gyoku.FDMi.core.Editor.Inspector
{
    /// <summary>
    /// すべての MonoBehaviour に適用される汎用 CustomEditor。
    /// [FDMiDataPathAttribute] 付きフィールドを OnEnable 時に解決する。
    /// 将来の他ドメインロジックもここに追加する。
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class FDMiBehaviourEditor : UnityEditor.Editor
    {
        ResolveDataPathsUseCase _useCase;

        /// <summary>
        /// 選択変更時に呼ばれる。UseCase を初期化して FDMiDataPath を解決する。
        /// </summary>
        void OnEnable()
        {
            _useCase = new ResolveDataPathsUseCase(new SceneFDMiDataRepository());
            _useCase.Execute(target);
        }

        /// <summary>
        /// Inspector の描画。デフォルト UI を維持する。
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
