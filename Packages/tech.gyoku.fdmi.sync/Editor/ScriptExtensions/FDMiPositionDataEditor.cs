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
    public class FDMiPositionDataEditor : FDMiSyncAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            var ltgt = target as FDMiPositionData;
            var objManager = FDMiEditorUI.FindParentComponent<FDMiObjectManager>(tgt);
            if (property.name == nameof(FDMiPositionData.refPoint) && (forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                property.objectReferenceValue = FDMiEditorUI.FindChildrenComponents<FDMiReferencePoint>(objManager).First();
        }

    }
}
