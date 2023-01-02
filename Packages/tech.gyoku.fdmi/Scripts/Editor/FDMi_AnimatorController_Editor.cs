using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using SaccFlight_FDMi;

namespace Saccflight_FDMi.Editor
{
    [CustomEditor(typeof(FDMi_AnimatorController))]
    public class FDMi_AnimatorController_Editor : UnityEditor.Editor
    {
        private bool foldOut = true, debugFoldOut = false;
        private SerializedProperty paramator, attribute, mul, offset, type, cs, so, Engine, Wheel;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var animatorController = target as FDMi_AnimatorController;
            paramator = serializedObject.FindProperty("paramator");
            attribute = serializedObject.FindProperty("animateAttribute");
            mul = serializedObject.FindProperty("mul");
            offset = serializedObject.FindProperty("offset");
            type = serializedObject.FindProperty("attributeType");
            so = serializedObject.FindProperty("so");
            cs = serializedObject.FindProperty("cs");
            Engine = serializedObject.FindProperty("Engine");
            Wheel = serializedObject.FindProperty("Wheel");
            serializedObject.Update();
            debugFoldOut = EditorGUILayout.Foldout(debugFoldOut, "入力後データ");
            if (debugFoldOut) base.OnInspectorGUI();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Param"), true);
                if (FDMi_UI.Button("Find")) SetParamator(serializedObject.FindProperty("Param"), FDMi_Util.FindParentComponent<FDMi_SharedParam>(animatorController));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animator"), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                foldOut = EditorGUILayout.Foldout(foldOut, "Paramator Manupulate / 動作させるアニメータ");
                if (FDMi_UI.Button("Add Paramator")) AddParamator();
            }
            if (foldOut) for (int i = 0; i < paramator.arraySize; i++) animatorSetup(i);

            serializedObject.ApplyModifiedProperties();
        }

        private void animatorSetup(int i)
        {
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.black;
            GUIStyle oneLetter = new GUIStyle();
            oneLetter.stretchWidth = false;
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.backgroundColor = defaultColor;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (FDMi_UI.Button("X")) RemoveParamator(i);
                    AnimatorParamaterType t = (AnimatorParamaterType)EditorGUILayout.EnumPopup("Attribute Type", (AnimatorParamaterType)type.GetArrayElementAtIndex(i).intValue);
                    type.GetArrayElementAtIndex(i).intValue = (int)t;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(paramator.GetArrayElementAtIndex(i), GUIContent.none, true);
                    EditorGUILayout.LabelField("=", GUILayout.Width(10));
                    switch ((AnimatorParamaterType)type.GetArrayElementAtIndex(i).intValue)
                    {
                        case AnimatorParamaterType.AirData:
                            AirData sf = (AirData)EditorGUILayout.EnumPopup("", (AirData)attribute.GetArrayElementAtIndex(i).intValue, GUILayout.Width(100));
                            attribute.GetArrayElementAtIndex(i).intValue = (int)sf;
                            break;
                        case AnimatorParamaterType.SyncObject:
                            EditorGUILayout.PropertyField(so.GetArrayElementAtIndex(i), GUIContent.none, true);
                            break;
                        case AnimatorParamaterType.ControlSurface:
                            EditorGUILayout.PropertyField(cs.GetArrayElementAtIndex(i), GUIContent.none, true);
                            break;
                        case AnimatorParamaterType.Engine:
                            EditorGUILayout.PropertyField(Engine.GetArrayElementAtIndex(i), GUIContent.none, true);
                            TFAttribute tfa = (TFAttribute)EditorGUILayout.EnumPopup("", (TFAttribute)attribute.GetArrayElementAtIndex(i).intValue, GUILayout.Width(100));
                            attribute.GetArrayElementAtIndex(i).intValue = (int)tfa;
                            break;
                        case AnimatorParamaterType.Wheel:
                            EditorGUILayout.PropertyField(Wheel.GetArrayElementAtIndex(i), GUIContent.none, true);
                            break;
                    }
                    EditorGUILayout.LabelField("*", GUILayout.Width(10));
                    EditorGUILayout.PropertyField(mul.GetArrayElementAtIndex(i), GUIContent.none, true);
                    EditorGUILayout.LabelField("+", GUILayout.Width(10));
                    EditorGUILayout.PropertyField(offset.GetArrayElementAtIndex(i), GUIContent.none, true);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    switch ((AnimatorParamaterType)type.GetArrayElementAtIndex(i).intValue)
                    {
                        case AnimatorParamaterType.AirData:
                            if (FDMi_UI.Button("paramator->attribute"))
                            {
                                AirData sf = (AirData)Enum.Parse(typeof(AirData), paramator.GetArrayElementAtIndex(i).stringValue);
                                attribute.GetArrayElementAtIndex(i).intValue = (int)sf;
                            }
                            if (FDMi_UI.Button("paramator<-attribute"))
                                paramator.GetArrayElementAtIndex(i).stringValue = attribute.GetArrayElementAtIndex(i).enumNames[attribute.GetArrayElementAtIndex(i).enumValueIndex];
                            if (FDMi_UI.Button("Autofill attribute"))
                                attribute.GetArrayElementAtIndex(i).enumValueIndex = attribute.GetArrayElementAtIndex(i - 1).enumValueIndex + 1;
                            break;
                        case AnimatorParamaterType.SyncObject:
                            if (FDMi_UI.Button("paramator<-attribute"))
                                paramator.GetArrayElementAtIndex(i).stringValue = so.GetArrayElementAtIndex(i).objectReferenceValue.name;
                            break;
                        case AnimatorParamaterType.ControlSurface:
                            if (FDMi_UI.Button("paramator<-attribute"))
                                paramator.GetArrayElementAtIndex(i).stringValue = cs.GetArrayElementAtIndex(i).objectReferenceValue.name;
                            break;
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void AddParamator()
        {
            int newArrSize = paramator.arraySize + 1;
            foreach (SerializedProperty sp in new SerializedProperty[] { paramator, attribute, mul, offset, type, so, cs, Engine, Wheel })
                sp.arraySize = newArrSize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        private void RemoveParamator(int i)
        {
            foreach (SerializedProperty sp in new SerializedProperty[] { paramator, attribute, mul, offset, type, so, cs, Engine, Wheel })
                sp.DeleteArrayElementAtIndex(i);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        public static void SetParamator<T>(SerializedProperty property, T target) where T : UnityEngine.Object
        {
            property.objectReferenceValue = target;
        }
    }
}
