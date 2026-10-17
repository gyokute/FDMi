using UnityEditor;
using UnityEngine;
using FDMi.core;

namespace FDMi.core.Editor.Inspector
{
    /// <summary>
    /// FDMiAnimationDriver 派生クラスのインスペクタ。
    /// valuePathHash は [HideInInspector] のため既定描画に出ない。焼き込み確認のため読み取り専用で表示する。
    /// </summary>
    [CustomEditor(typeof(FDMiAnimationDriver), true)]
    public class FDMiAnimationDriverEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("valuePathHash", ((FDMiAnimationDriver)target).valuePathHash.ToString());
        }
    }
}
