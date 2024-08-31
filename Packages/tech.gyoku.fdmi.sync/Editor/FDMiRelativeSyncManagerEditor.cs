using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using tech.gyoku.FDMi.sync;
using tech.gyoku.FDMi.core;
using tech.gyoku.FDMi.core.editor;

namespace tech.gyoku.FDMi.sync.editor
{
    [CustomEditor(typeof(FDMiRelativeObjectSyncManager), true)]
    public class FDMiRelativeObjectSyncManagerEditor : FDMiReferencePointEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            var relativeObjectSyncManager = tgt;
            if (property.name == nameof(FDMiRelativeObjectSyncManager.refPoints))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    var refPoints = FindObjectsOfType<FDMiReferencePoint>();
                    FDMiEditorUI.SetObjectArrayProperty(property, refPoints);
                }
            }
            
            if (property.name == nameof(FDMiRelativeObjectSyncManager.rootRefPoint))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = relativeObjectSyncManager;
                }
            }
            if (property.name == nameof(FDMiRelativeObjectSyncManager.parentRefPoint))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = relativeObjectSyncManager;
                }
            }
        }
    }
}