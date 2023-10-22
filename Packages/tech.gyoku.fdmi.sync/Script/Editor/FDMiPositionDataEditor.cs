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
    public class FDMiPositionDataEditor : FDMiAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            var ltgt = target as FDMiPositionData;
            if (property.name == nameof(FDMiPositionData.refPoint) && (FDMiEditorUI.Button("Find") || forceSetup))
                property.objectReferenceValue = FDMiEditorUI.FindChildrenComponents<FDMiReferencePoint>(ltgt.objectManager).First();
        }
        public override void SetupAll()
        {
            base.SetupAll();
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            var tgt = target as FDMiPositionData;
            var objManager = FDMiEditorUI.FindParentComponent<FDMiObjectManager>(tgt);
            while (property.NextVisible(false))
            {
                if (property.name == nameof(tgt.objectManager))
                    property.objectReferenceValue = objManager;
                if (property.name == nameof(tgt.refPoint))
                    property.objectReferenceValue = FDMiEditorUI.FindChildrenComponents<FDMiReferencePoint>(objManager).First();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
