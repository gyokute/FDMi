
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    [CustomEditor(typeof(FDMiAnimationDriverArray), true)]
    public class FDMiAnimationDriverArrayEditor : UnityEditor.Editor
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty Input, paramator, outputValue;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var animDriver = target as FDMiAnimationDriverArray;
            Input = serializedObject.FindProperty("Input");
            paramator = serializedObject.FindProperty("paramator");
            outputValue = serializedObject.FindProperty("outputValue");
            serializedObject.Update();
            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "入力後データ");
            if (debugFoldOut) base.OnInspectorGUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("animator"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateMode"), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                foldOut = EditorGUILayout.Foldout(foldOut, "Paramator Manupulate / 動作させるアニメータ");
                if (FDMiEditorUI.Button(false, "Add Paramator")) AddParamator();
            }
            if (foldOut) for (int i = 0; i < paramator.arraySize; i++) animatorSetup(i);

            serializedObject.ApplyModifiedProperties();
        }

        private void animatorSetup(int i)
        {
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            GUIStyle oneLetter = new GUIStyle();
            oneLetter.stretchWidth = false;
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.backgroundColor = defaultColor;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (FDMiEditorUI.Button(false, "X")) RemoveParamator(i);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("Paramator", GUILayout.Width(70));
                        EditorGUILayout.PropertyField(paramator.GetArrayElementAtIndex(i), GUIContent.none, true);
                    }
                    EditorGUILayout.LabelField("=", GUILayout.Width(10));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("Curve", GUILayout.Width(50));
                        outputValue.GetArrayElementAtIndex(i).animationCurveValue = EditorGUILayout.CurveField(GUIContent.none, outputValue.GetArrayElementAtIndex(i).animationCurveValue, GUILayout.MaxWidth(70));
                    }
                    EditorGUILayout.LabelField("*", GUILayout.Width(10));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("FDMiFloat", GUILayout.Width(70));
                        EditorGUILayout.PropertyField(Input.GetArrayElementAtIndex(i), GUIContent.none, true);
                    }
                    // EditorGUILayout.LabelField("+", GUILayout.Width(10));
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void AddParamator()
        {
            int newArrSize = paramator.arraySize + 1;
            foreach (SerializedProperty sp in new SerializedProperty[] { Input, paramator, outputValue })
                sp.arraySize = newArrSize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        private void RemoveParamator(int i)
        {
            foreach (SerializedProperty sp in new SerializedProperty[] { Input, paramator, outputValue })
            {
                if (sp.GetArrayElementAtIndex(i).objectReferenceValue != null)
                    sp.DeleteArrayElementAtIndex(i);
                sp.DeleteArrayElementAtIndex(i);
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        public static void SetParamator<T>(SerializedProperty property, T target) where T : UnityEngine.Object
        {
            property.objectReferenceValue = target;
        }

    }
}