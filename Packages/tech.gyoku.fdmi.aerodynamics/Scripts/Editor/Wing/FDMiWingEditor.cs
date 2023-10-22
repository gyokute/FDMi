using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using tech.gyoku.FDMi.core.editor;
using tech.gyoku.FDMi.aerodynamics;

namespace tech.gyoku.FDMi.aerodynamics.editor
{
    [CustomEditor(typeof(FDMiWing), true)]
    public class FDMiWingEditor : FDMiAttributeEditor
    {
        private FDMiWing wing;
        private SerializedProperty spL, spR, spRL, spanNormal, planfNormal, chordNormal, controlPoint;
        private SerializedProperty cpChordLength, cpSpanLength, cpArea, Qij;
        private SerializedProperty Cl_Alpha, Cd_Alpha, Cl_Mach, Cd_Mach;
        private float totalArea;

        private void OnEnable()
        {
            wing = (FDMiWing)target;

            spL = serializedObject.FindProperty("spL");
            spR = serializedObject.FindProperty("spR");
            spRL = serializedObject.FindProperty("spRL");
            spanNormal = serializedObject.FindProperty("spanNormal");
            planfNormal = serializedObject.FindProperty("planfNormal");
            chordNormal = serializedObject.FindProperty("chordNormal");
            controlPoint = serializedObject.FindProperty("controlPoint");
            cpChordLength = serializedObject.FindProperty("cpChordLength");
            cpSpanLength = serializedObject.FindProperty("cpSpanLength");
            cpArea = serializedObject.FindProperty("cpArea");
            Qij = serializedObject.FindProperty("Qij");
            Cl_Alpha = serializedObject.FindProperty("Cl_Alpha");
            Cd_Alpha = serializedObject.FindProperty("Cd_Alpha");
            Cl_Mach = serializedObject.FindProperty("Cl_Mach");
            Cd_Mach = serializedObject.FindProperty("Cd_Mach");
        }
        public void OnSceneGUI()
        {
            drawPlanform(wing);
        }

        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.type == Cl_Alpha.type)
                SetAnimatorCurve(property);
            if (property.name == nameof(wing.SectionL) && FDMiEditorUI.Button("Maybe"))
            {
                int thisIndex = wing.transform.GetSiblingIndex();
                FDMiWingSection[] secs = wing.transform.parent.GetComponentsInChildren<FDMiWingSection>();
                property.objectReferenceValue = secs.First(s => s.transform.GetSiblingIndex() == thisIndex - 1);
            }
            if (property.name == nameof(wing.SectionR) && FDMiEditorUI.Button("Maybe"))
            {
                int thisIndex = wing.transform.GetSiblingIndex();
                FDMiWingSection[] secs = wing.transform.parent.GetComponentsInChildren<FDMiWingSection>();
                property.objectReferenceValue = secs.First(s => s.transform.GetSiblingIndex() == thisIndex + 1);
            }
            if (property.name == nameof(wing.affectWing) && FDMiEditorUI.Button("Maybe"))
            {
                Transform bt = wing.GetComponentInParent<Rigidbody>().transform;
                FDMiWing[] wings = bt.GetComponentsInChildren<FDMiWing>();
                wings = Array.FindAll(wings, delegate (FDMiWing w) { return w != wing; });
                wings = Array.FindAll(wings, delegate (FDMiWing w) { return w.transform.parent == wing.transform.parent; });
                property.arraySize = 0;
                FDMiEditorUI.SetObjectArrayProperty<FDMiWing>(property, wings);
            }
        }

        public override void SetupAll()
        {
            base.SetupAll();
            SetControlPoint();
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(wing.transform, "Set FDMiWing Transform");
            PrefabUtility.RecordPrefabInstancePropertyModifications(wing.transform);
            EditorUtility.SetDirty(wing);
        }

        public void drawPlanform(FDMiWing w)
        {
            if (!w.SectionL || !w.SectionR) return;
            Transform bt = w.GetComponentInParent<Rigidbody>().transform;
            using (new Handles.DrawingScope(bt.localToWorldMatrix))
            {
                Vector3 wingTipL = w.spL;
                Vector3 wingTipR = w.spR;
                Vector3 wingToeL = w.spL - w.SectionL.chordLength * bt.InverseTransformDirection(w.SectionL.transform.forward);
                Vector3 wingToeR = w.spR - w.SectionR.chordLength * bt.InverseTransformDirection(w.SectionR.transform.forward);
                Handles.color = Color.magenta;
                Handles.DrawLine(wingTipL, wingTipR);
                Handles.DrawLine(wingTipL, wingToeL);
                Handles.DrawLine(wingTipR, wingToeR);
                Handles.DrawLine(wingToeL, wingToeR);
                Handles.color = Color.blue;
                Handles.DrawLine(w.controlPoint, w.controlPoint + w.spanNormal);
                Handles.color = Color.green;
                Handles.DrawLine(w.controlPoint, w.controlPoint + w.chordNormal);
                Handles.color = Color.red;
                Handles.DrawLine(w.controlPoint, w.controlPoint + w.planfNormal);
            }
        }
        private void SetAnimatorCurve(SerializedProperty sp)
        {
            if (FDMiEditorUI.Button("Load")) { sp.animationCurveValue = AnimationCurveUtils.LoadCSVToAnimationCurve(); }
        }

        private void SetControlPoint()
        {
            Undo.RegisterCompleteObjectUndo(wing.gameObject, "FDMiWing Setup");
            Transform bt = wing.GetComponentInParent<Rigidbody>().transform;

            Vector3 spl = bt.InverseTransformPoint(wing.SectionL.transform.position);
            Vector3 spr = bt.InverseTransformPoint(wing.SectionR.transform.position);
            spL.vector3Value = spl;
            spR.vector3Value = spr;
            spRL.vector3Value = spl - spr;

            Vector3 spn = (spl - spr).normalized;
            Vector3 planfn = Vector3.Cross(spn, Vector3.forward).normalized;
            spanNormal.vector3Value = spn;
            planfNormal.vector3Value = planfn;
            chordNormal.vector3Value = -bt.InverseTransformDirection(Vector3.Lerp(wing.SectionL.transform.forward, wing.SectionR.transform.forward, 0.5f));

            Vector3 cpl = spl - 0.25f * wing.SectionL.chordLength * bt.InverseTransformDirection(wing.SectionL.transform.forward);
            Vector3 cpr = spr - 0.25f * wing.SectionR.chordLength * bt.InverseTransformDirection(wing.SectionR.transform.forward);
            controlPoint.vector3Value = Vector3.Lerp(cpl, cpr, 0.5f);

            cpChordLength.floatValue = Mathf.Lerp(wing.SectionL.chordLength, wing.SectionR.chordLength, 0.5f);
            cpSpanLength.floatValue = Vector3.Project(spr - spl, spn).magnitude;
            cpArea.floatValue = cpChordLength.floatValue * cpSpanLength.floatValue;
            wing.transform.position = bt.TransformPoint(Vector3.Lerp(spl, spr, 0.5f));
            wing.transform.rotation = bt.rotation * Quaternion.LookRotation(Vector3.Cross(planfn, spn), planfn);

            Qij.arraySize = wing.affectWing.Length;
            for (int i = 0; i < wing.affectWing.Length; i++)
            {
                FDMiWing aw = wing.affectWing[i];
                Qij.GetArrayElementAtIndex(i).vector3Value = DownwashEffect(aw, controlPoint.vector3Value);
            }
            EditorUtility.SetDirty(wing.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(wing.gameObject);
        }


        private Vector3 DownwashEffect(FDMiWing affectedWing, Vector3 cp)
        {
            Vector3 u = -Vector3.forward;
            Vector3 RC = cp - affectedWing.spR;
            Vector3 CL = affectedWing.spL - cp;
            Vector3 RLn = spRL.vector3Value.normalized;
            Vector3 Qr = (1f / RC.magnitude) * Vector3.Cross(u, RC).normalized;
            Vector3 Ql = (1f / CL.magnitude) * Vector3.Cross(u, CL).normalized;
            Vector3 Qrl = (Vector3.Dot(RLn, RC.normalized) * Vector3.Dot(RLn, CL.normalized)) * Vector3.Cross(spRL.vector3Value, RC).normalized;
            return (0.25f / Mathf.PI) * (Qr + Ql /*+ Qrl*/);
        }
    }
}
