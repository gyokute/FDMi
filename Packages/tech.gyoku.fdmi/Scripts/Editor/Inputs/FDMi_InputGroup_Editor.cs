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
    [CustomEditor(typeof(FDMi_InputGroup), true)]
    public class FDMi_InputGroup_Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var tgt = target as FDMi_InputGroup;
            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(tgt.im))
                    {
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetParamator(property, FDMi_Util.FindParentComponent<FDMi_InputManager>(tgt));
                    }
                    if (property.name == nameof(tgt.inputObject))
                    {
                        if (FDMi_UI.Button("Find")) findGrabbableObjects(tgt, property);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void findGrabbableObjects(FDMi_InputGroup tgt, SerializedProperty property)
        {
            var arr = FDMi_Util.FindChildrenComponents<FDMi_InputObject>(tgt);
            arr = arr.OrderBy(a => tgt.transform.InverseTransformPoint(a.transform.position).x);
            FDMi_Util.SetObjectArrayProperty(property, arr);
        }

        public void OnSceneGUI()
        {
            var tgt = target as FDMi_InputGroup;
            serializedObject.Update();
            List<Vector3> colliderPos = new List<Vector3>();
            float rad = serializedObject.FindProperty("colliderRadius").floatValue;
            for (int cp = 0; cp < serializedObject.FindProperty("colliderPos").arraySize; cp++)
                colliderPos.Add(serializedObject.FindProperty("colliderPos").GetArrayElementAtIndex(cp).vector3Value);
            while (colliderPos.Count < 2)
                colliderPos.Add(Vector3.zero);
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = Color.yellow;
                using (new Handles.DrawingScope(tgt.transform.localToWorldMatrix))
                {
                    for (int cp = 0; cp < colliderPos.Count; cp++)
                    {
                        foreach (var q in new Vector3[] { Vector3.right, Vector3.up, Vector3.forward })
                        {
                            Handles.CircleHandleCap(
                                0,
                                colliderPos[cp],
                                tgt.transform.rotation * Quaternion.LookRotation(q),
                                rad,
                                EventType.Repaint
                            );
                            if (cp + 1 < colliderPos.Count)
                            {
                                Handles.DrawLine(colliderPos[cp] + rad * q, colliderPos[(cp + 1) % colliderPos.Count] + rad * q);
                                Handles.DrawLine(colliderPos[cp] - rad * q, colliderPos[(cp + 1) % colliderPos.Count] - rad * q);
                            }
                        }
                    }
                }
            }

            serializedObject.FindProperty("colliderPos").arraySize = colliderPos.Count;
            for (int cp = 0; cp < colliderPos.Count; cp++)
                serializedObject.FindProperty("colliderPos").GetArrayElementAtIndex(cp).vector3Value = colliderPos[cp];

            serializedObject.ApplyModifiedProperties();
        }
    }
}