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
    [CustomEditor(typeof(FDMi_Attributes), true)]
    public class FDMi_Attributes_editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var attributes = target as FDMi_Attributes;
            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(attributes.Param))
                    {
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetParamator(property, FDMi_Util.FindParentComponent<FDMi_SharedParam>(attributes));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void FindSharedParam()
        {
            var attributes = target as FDMi_Attributes;
            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (property.name == nameof(attributes.Param))
                    {
                        EditorGUILayout.PropertyField(property, true);
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetParamator(property, FDMi_Util.FindParentComponent<FDMi_SharedParam>(attributes));
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
        private bool foldBase;
        public void rawData()
        {
            foldBase = EditorGUILayout.Foldout(foldBase, "入力データ");
            if (foldBase) base.OnInspectorGUI();
        }
    }
}
