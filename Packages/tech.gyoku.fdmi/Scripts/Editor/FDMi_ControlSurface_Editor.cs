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
    [CustomEditor(typeof(FDMi_ControlSurface), true)]
    public class FDMi_ControlSurface_Editor : FDMi_Attributes_editor
    {
        private FDMi_wing wing;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var tgt = target as FDMi_ControlSurface;
            if (!wing) wing = FDMi_Util.FindParentComponent<FDMi_wing>(tgt);
            if (wing) setWingSection(wing.Cl_Alpha.Length);
            serializedObject.Update();
            base.FindSharedParam();
            base.rawData();
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Target angle/操舵目標角");
                FDMi_UI.PropertyField(serializedObject, "actuatorControl");
            }
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("degree per section/各操舵面の入力");
                using (new GUILayout.HorizontalScope())
                {
                    for (int i = 0; i < wing.Cl_Alpha.Length; i++)
                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Label(i.ToString());
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceMul").GetArrayElementAtIndex(i), GUIContent.none, true);
                        }
                }

            }
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Effects per deg/ 翼面に与える効果");
                FDMi_UI.PropertyField(serializedObject, "Alpha_Deg");
                FDMi_UI.PropertyField(serializedObject, "Cl_Deg");
                FDMi_UI.PropertyField(serializedObject, "Cd_Deg");
                FDMi_UI.PropertyField(serializedObject, "Cm_Deg");
                FDMi_UI.PropertyField(serializedObject, "Area_Deg");
            }
            serializedObject.ApplyModifiedProperties();
        }
        public void OnSceneGUI()
        {
            var tgt = target as FDMi_ControlSurface;
            if (!wing) wing = FDMi_Util.FindParentComponent<FDMi_wing>(tgt);
            if (wing) FDMi_wing_Editor.drawPlanform(wing);
        }
        private void setWingSection(int sectorNum)
        {
            serializedObject.Update();
            foreach (var name in new string[] { "surfaceMul", "alpha", "cl", "cd", "cm", "area" })
                serializedObject.FindProperty(name).arraySize = sectorNum;
            var area = serializedObject.FindProperty("area");
            for (int i = 0; i < area.arraySize; i++)
                area.GetArrayElementAtIndex(i).floatValue = 1f;
            serializedObject.ApplyModifiedProperties();
        }

    }
}