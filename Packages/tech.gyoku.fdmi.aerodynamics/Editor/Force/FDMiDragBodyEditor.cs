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
    [CustomEditor(typeof(FDMiDragBody), true)]
    public class FDMiDragBodyEditor : FDMiAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            FDMiDragBody db = (FDMiDragBody)tgt;
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiDragBody.cdA) && (FDMiEditorUI.Button(forceSetup, "Set")))
                property.floatValue = 0.5f * (db.cdp + db.lambda * db.l / db.d) * (Mathf.PI * db.d);
        }

        public void OnSceneGUI()
        {
            var tgt = target as FDMiDragBody;
            serializedObject.Update();
            float rad = tgt.d / 2;
            Vector3 fwd = Vector3.forward * 0.5f * tgt.l;
            Vector3 aft = -Vector3.forward * 0.5f * tgt.l;
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = Color.yellow;
                using (new Handles.DrawingScope(tgt.transform.localToWorldMatrix))
                {

                    foreach (var q in new Vector3[] { Vector3.right, Vector3.up, Vector3.forward })
                    {
                        Handles.CircleHandleCap(0, fwd, tgt.transform.rotation * Quaternion.LookRotation(q), rad, EventType.Repaint);
                        Handles.CircleHandleCap(0, aft, tgt.transform.rotation * Quaternion.LookRotation(q), rad, EventType.Repaint);
                        Handles.DrawLine(fwd + rad * q, aft + rad * q);
                        Handles.DrawLine(fwd - rad * q, aft - rad * q);
                    }
                }
            }
        }

    }
}