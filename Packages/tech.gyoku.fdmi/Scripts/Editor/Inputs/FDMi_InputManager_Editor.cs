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
    [CustomEditor(typeof(FDMi_InputManager), true)]
    public class FDMi_InputManager_Editor : FDMi_Attributes_editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var tgt = target as FDMi_InputManager;
            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(tgt.Param))
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetParamator(property, FDMi_Util.FindParentComponent<FDMi_SharedParam>(tgt));
                    if (property.name == nameof(tgt.inputGroup))
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetObjectArrayProperty(property, FDMi_Util.FindChildrenComponents<FDMi_InputGroup>(tgt));
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}