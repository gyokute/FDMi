using System.Collections.Generic;
using System.Linq;
using FDMi.core.Editor.Application.UseCases;
using FDMi.core.Editor.Infrastructure.Repositories;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Inspector
{
    [CustomEditor(typeof(FDMiBehaviour), true)]
    public class FDMiBehaviourEditor : UnityEditor.Editor
    {
        List<IFDMiAutoSetupUseCase> fDMiAutoSetupUseCases;

        void OnEnable()
        {
            //すべてのAssemblyからIFDMiAutoSetupUseCaseを継承するクラスを取得する
            var autoSetupUseCaseTypes = System
                .AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    typeof(IFDMiAutoSetupUseCase).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract
                );
            // 取得したクラスのインスタンスを生成し、Executeメソッドを呼び出す
            fDMiAutoSetupUseCases = autoSetupUseCaseTypes
                .Select(type => (IFDMiAutoSetupUseCase)System.Activator.CreateInstance(type))
                .ToList();
            fDMiAutoSetupUseCases.ForEach(useCase => useCase.Execute(target));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            // Resolve は選択時のみ実行（パス文字列を編集した場合は ResolveAndRegisterAll をメニューから手動実行）
            if (EditorGUI.EndChangeCheck())
                fDMiAutoSetupUseCases.ForEach(useCase => useCase.Execute(target));
        }
    }
}
