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
#if UNITY_EDITOR
    [CustomEditor(typeof(FDMi_Avionics_CADC), true)]
    public class FDMi_CADC_Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var tgt = target as FDMi_Avionics_CADC;
            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.type == "AnimationCurve")
                    {
                        if (FDMi_UI.Button("Load")){
                            property.animationCurveValue = AnimationCurveUtils.LoadCSVToAnimationCurve();
                        } 
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

    }
#endif
}
