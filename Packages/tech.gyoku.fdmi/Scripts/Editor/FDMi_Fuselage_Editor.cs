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
    [CustomEditor(typeof(FDMi_Fuselage), true)]
    public class FDMi_Fuselage_Editor : FDMi_Attributes_editor
    {
        private FDMi_wing mainFoil;
        private Vector3 macTE, mac50, macLE;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var tgt = target as FDMi_Fuselage;
            serializedObject.Update();
            base.FindSharedParam();
            mainFoil = (FDMi_wing)EditorGUILayout.ObjectField("main wing/主翼", mainFoil, typeof(FDMi_wing), true);
            if (mainFoil != null && mac50 == Vector3.zero) getMAC(mainFoil);
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Weight Definition/重量定義");
                FDMi_Util.DrawProperty(serializedObject, "overrideCog");
                if (serializedObject.FindProperty("overrideCog").boolValue)
                {
                    FDMi_Util.DrawProperty(serializedObject, "OEW");
                    FDMi_Util.DrawProperty(serializedObject, "payload");
                    FDMi_Util.DrawProperty(serializedObject, "fuel");
                    FDMi_Util.DrawProperty(serializedObject, "maxFuel");
                }
            }
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Center of Mass Definition/重心定義");
                if (serializedObject.FindProperty("overrideCog").boolValue)
                {
                    FDMi_Util.DrawProperty(serializedObject, "fullLoadCoG");
                    float MACP = (serializedObject.FindProperty("fullLoadCoG").vector3Value.z - macLE.z) / (macTE.z - macLE.z);
                    GUILayout.Label("MAC" + MACP + "%");
                    FDMi_Util.DrawProperty(serializedObject, "minLoadCoG");
                    MACP = (serializedObject.FindProperty("minLoadCoG").vector3Value.z - macLE.z) / (macTE.z - macLE.z);
                    GUILayout.Label("MAC" + MACP + "%");

                }
            }
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Inertia Moment/慣性モーメント");
                FDMi_Util.DrawProperty(serializedObject, "overrideInertia");
                if (serializedObject.FindProperty("overrideInertia").boolValue)
                FDMi_Util.DrawProperty(serializedObject, "inertiaTensor");
                FDMi_Util.DrawProperty(serializedObject, "overrideInertiaRotation");
                if (serializedObject.FindProperty("overrideInertiaRotation").boolValue)
                FDMi_Util.DrawProperty(serializedObject, "inertiaRotation");
                GUILayout.Label("*Consider airplane is synmetrical. ");
                GUILayout.Label(" Only x-Axis (in unity) of Inertia can change.");

            }
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Drag/機体抵抗");
                FDMi_Util.DrawProperty(serializedObject, "Cd_Alpha");
                FDMi_Util.DrawProperty(serializedObject, "bodyArea");
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("parkBrake To fix/パーキングブレーキによる機体固定");
                FDMi_Util.DrawProperty(serializedObject, "parkBrake");
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Operation Data");
                if (serializedObject.FindProperty("overrideCog").boolValue)
                {
                    float weight = serializedObject.FindProperty("OEW").floatValue + serializedObject.FindProperty("payload").floatValue;
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Current Weight");
                        GUILayout.Label((weight + serializedObject.FindProperty("fuel").floatValue).ToString() + "kg");
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Max Weight");
                        GUILayout.Label((weight + serializedObject.FindProperty("maxFuel").floatValue).ToString() + "kg");
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            var tgt = target as FDMi_Fuselage;
            serializedObject.Update();
            using (new Handles.DrawingScope(tgt.transform.localToWorldMatrix))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    Handles.color = Color.yellow;
                    Vector3 minPos = serializedObject.FindProperty("minLoadCoG").vector3Value;
                    Handles.DrawLine(minPos + Vector3.right * mac50.x, minPos - Vector3.right * mac50.x);
                    Handles.ConeHandleCap(0, minPos, Quaternion.LookRotation(Vector3.right), 0.2f, EventType.Repaint);
                    Handles.color = Color.yellow;
                    Vector3 maxPos = serializedObject.FindProperty("fullLoadCoG").vector3Value;
                    Handles.DrawLine(maxPos + Vector3.right * mac50.x, maxPos - Vector3.right * mac50.x);
                    Handles.ConeHandleCap(0, maxPos, Quaternion.LookRotation(Vector3.right), 0.2f, EventType.Repaint);
                }
            }
            if (mainFoil == null) return;
            if (mac50 == Vector3.zero) getMAC(mainFoil);
            using (new Handles.DrawingScope(mainFoil.transform.localToWorldMatrix))
            {
                Handles.color = Color.magenta;
                Handles.DrawLine(macTE, macLE);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void getMAC(FDMi_wing wing)
        {
            Vector3 centerLE, centerTE, centerCE, TipTE, TipLE, TipCE;
            if (wing.wingTip.Length % 2 == 0)
            {
                centerLE = Vector3.Lerp(wing.wingTip[wing.wingTip.Length / 2 - 1], wing.wingTip[wing.wingTip.Length / 2], 0.5f);
                centerTE = Vector3.Lerp(wing.wingToe[wing.wingToe.Length / 2 - 1], wing.wingToe[wing.wingToe.Length / 2], 0.5f);
            }
            else
            {
                centerLE = wing.wingTip[wing.wingTip.Length / 2];
                centerTE = wing.wingToe[wing.wingToe.Length / 2];
            }

            centerLE = Vector3.Project(centerLE, Vector3.forward);
            centerTE = Vector3.Project(centerTE, Vector3.forward);
            centerCE = Vector3.Lerp(centerLE, centerTE, 0.5f);
            TipTE = Vector3.ProjectOnPlane(wing.wingTip[0], Vector3.up);
            TipLE = Vector3.ProjectOnPlane(wing.wingToe[0], Vector3.up);
            TipCE = Vector3.Lerp(TipLE, TipTE, 0.5f);

            Vector3 macTipLE = TipLE + (centerLE - centerTE);
            Vector3 maccenterTE = centerTE + (TipTE - TipLE);

            float a1 = (TipCE.z - centerCE.z) / (TipCE.x - centerCE.x);
            float a3 = (macTipLE.z - maccenterTE.z) / (macTipLE.x - maccenterTE.x);
            mac50 = new Vector3((a1 * centerCE.x - centerCE.z - a3 * maccenterTE.x + maccenterTE.z) / (a1 - a3), 0, 0);
            mac50.z = (TipCE.z - centerCE.z) / (TipCE.x - centerCE.x) * (mac50.x - centerCE.x) + centerCE.z;

            for (int i = 0; i < wing.wingTip.Length - 1; i++)
            {
                if (wing.wingTip[i].x >= mac50.x && wing.wingTip[i + 1].x < mac50.x)
                {
                    Debug.Log(wing.wingTip[i].ToString() + wing.wingTip[i + 1]);
                    macLE = Vector3.Lerp(wing.wingTip[i], wing.wingTip[i + 1], (mac50.x - wing.wingTip[i].x) / (wing.wingTip[i + 1].x - wing.wingTip[i].x));
                }
                if (wing.wingToe[i].x >= mac50.x && wing.wingToe[i + 1].x < mac50.x)
                    macTE = Vector3.Lerp(wing.wingToe[i], wing.wingToe[i + 1], (mac50.x - wing.wingToe[i].x) / (wing.wingToe[i + 1].x - wing.wingToe[i].x));
            }
        }
    }
}