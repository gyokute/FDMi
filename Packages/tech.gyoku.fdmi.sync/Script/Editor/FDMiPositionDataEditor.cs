using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using tech.gyoku.FDMi.core;
using tech.gyoku.FDMi.core.editor;
using tech.gyoku.FDMi.sync;

namespace tech.gyoku.FDMi.sync.editor
{
    [CustomEditor(typeof(FDMiPositionData), true)]
    public class FDMiPositionDataEditor : UnityEditor.Editor
    {
        // Start is called before the first frame update
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var tgt = target as FDMiPositionData;
            serializedObject.Update();

            if (FDMiEditorUI.BigButton("Setup All")) SetupAll();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(tgt.refPoint) && FDMiEditorUI.Button("Find"))
                        property.objectReferenceValue = FDMiEditorUI.FindChildrenComponents<FDMiReferencePoint>(tgt.objectManager).First();

                    if (property.name == nameof(tgt.Position) && FDMiEditorUI.Button("Find"))
                        property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.Position));
                    if (property.name == nameof(tgt.KmPosition) && FDMiEditorUI.Button("Find"))
                        property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.KmPosition));
                    if (property.name == nameof(tgt.Velocity) && FDMiEditorUI.Button("Find"))
                        property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.Velocity));
                    if (property.name == nameof(tgt.Attitude) && FDMiEditorUI.Button("Find"))
                        property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.Attitude));
                    if (property.name == nameof(tgt.Rotation) && FDMiEditorUI.Button("Find"))
                        property.objectReferenceValue = FindDataByName<FDMiQuaternion>(tgt, nameof(tgt.Rotation));
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        public void SetupAll()
        {
            SerializedProperty property;
            var tgt = target as FDMiPositionData;
            var objManager = FDMiEditorUI.FindParentComponent<FDMiObjectManager>(tgt);

            property = FDMiEditorUI.GetProperty<FDMiObjectManager>(serializedObject, nameof(tgt.objectManager));
            property.objectReferenceValue = objManager;
            property = FDMiEditorUI.GetProperty<FDMiReferencePoint>(serializedObject, nameof(tgt.refPoint));
            property.objectReferenceValue = FDMiEditorUI.FindChildrenComponents<FDMiReferencePoint>(objManager).First();

            property = FDMiEditorUI.GetProperty<FDMiVector3>(serializedObject, nameof(tgt.Position));
            property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.Position));
            property = FDMiEditorUI.GetProperty<FDMiVector3>(serializedObject, nameof(tgt.KmPosition));
            property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.KmPosition));
            property = FDMiEditorUI.GetProperty<FDMiVector3>(serializedObject, nameof(tgt.Velocity));
            property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.Velocity));
            property = FDMiEditorUI.GetProperty<FDMiVector3>(serializedObject, nameof(tgt.Attitude));
            property.objectReferenceValue = FindDataByName<FDMiVector3>(tgt, nameof(tgt.Attitude));
            property = FDMiEditorUI.GetProperty<FDMiQuaternion>(serializedObject, nameof(tgt.Rotation));
            property.objectReferenceValue = FindDataByName<FDMiQuaternion>(tgt, nameof(tgt.Rotation));
        }

        private T FindDataByName<T>(Component tgt, string name) where T : FDMiEvent
        {
            var objs = tgt.GetComponents<T>();
            return objs.First(o => o.name == name);
        }
    }
}
