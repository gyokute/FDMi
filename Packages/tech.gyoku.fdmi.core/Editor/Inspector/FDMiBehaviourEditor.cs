using UnityEditor;
using UnityEngine;
using FDMi.core.Editor.Application.UseCases;
using FDMi.core.Editor.Infrastructure.Repositories;

namespace FDMi.core.Editor.Inspector
{
    [CustomEditor(typeof(FDMiBehaviour), true)]
    public class FDMiBehaviourEditor : UnityEditor.Editor
    {
        ResolveDataPathsUseCase  _resolveUseCase;
        RegisterCallbacksUseCase _registerUseCase;

        void OnEnable()
        {
            _resolveUseCase  = new ResolveDataPathsUseCase(new SceneFDMiDataRepository());
            _registerUseCase = new RegisterCallbacksUseCase();

            _resolveUseCase.Execute(target);
            _registerUseCase.Execute(target as MonoBehaviour);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            // Resolve は選択時のみ実行（パス文字列を編集した場合は ResolveAndRegisterAll をメニューから手動実行）
            if (EditorGUI.EndChangeCheck())
                _registerUseCase?.Execute(target as MonoBehaviour);
        }
    }
}
