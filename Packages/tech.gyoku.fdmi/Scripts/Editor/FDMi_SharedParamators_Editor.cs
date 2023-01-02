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
    [CustomEditor(typeof(FDMi_SharedParam), true)]
    public class FDMi_SharedParamators_Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var tgt = target as FDMi_SharedParam;
            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(tgt.EXT))
                    {
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetObjectArrayProperty(property, FDMi_Util.FindChildrenComponents<FDMi_Attributes>(tgt));
                    }
                    if (property.name == nameof(tgt.EntityControl))
                    {
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetParamator(property, FDMi_Util.FindParentComponent<SaccEntity>(tgt));
                    }
                    if (property.name == nameof(tgt.VehicleRigidbody))
                    {
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetParamator(property, FDMi_Util.FindParentComponent<Rigidbody>(tgt));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }


    }
}