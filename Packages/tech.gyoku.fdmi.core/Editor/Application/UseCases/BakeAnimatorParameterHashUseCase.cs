using UnityEditor;
using UnityEngine;
using FDMi.core;

namespace FDMi.core.Editor.Application.UseCases
{
    /// <summary>
    /// FDMiAnimationDriver 派生クラスの valuePathHash に Animator.StringToHash(parameterName) を焼き込むユースケース。
    /// IFDMiAutoSetupUseCase 実装により FDMiBehaviourEditor / FDMiEditorHooks から ResolveDataPathsUseCase と同じタイミングで実行される。
    /// </summary>
    public class BakeAnimatorParameterHashUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(UnityEngine.Object target)
        {
            if (!(target is FDMiAnimationDriver))
                return;

            var so = new SerializedObject(target);
            var nameProp = so.FindProperty("parameterName");
            var hashProp = so.FindProperty("valuePathHash");
            if (nameProp == null || hashProp == null || string.IsNullOrEmpty(nameProp.stringValue))
                return;

            var hash = Animator.StringToHash(nameProp.stringValue);
            if (hashProp.intValue == hash)
                return;

            hashProp.intValue = hash;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void ExecuteAll()
        {
            var useCase = new BakeAnimatorParameterHashUseCase();
            // コールバック駆動の派生クラスはコンポーネント無効でも FDMiData 経由で発火するため、非アクティブも含めて焼き込む
            foreach (var driver in UnityEngine.Object.FindObjectsByType<FDMiAnimationDriver>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                useCase.Execute(driver);
        }
    }
}
