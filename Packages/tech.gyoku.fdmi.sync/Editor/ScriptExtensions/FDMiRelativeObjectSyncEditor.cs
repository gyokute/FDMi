
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using tech.gyoku.FDMi.core;
using tech.gyoku.FDMi.core.editor;
using tech.gyoku.FDMi.sync;

namespace tech.gyoku.FDMi.sync.editor
{
    [CustomEditor(typeof(FDMiRelativeObjectSync), true)]
    public class FDMiRelativeObjectSyncEditor : FDMiReferencePointEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            var relativeObjectSyncManager = FindObjectOfType<FDMiRelativeObjectSyncManager>();
            if (property.name == nameof(FDMiRelativeObjectSync.obejctManaer))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = tgt.GetComponentInParent<FDMiObjectManager>();
                }
            }
            if (property.name == nameof(FDMiRelativeObjectSync.parentRefPoint))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = relativeObjectSyncManager;
                }
            }
            if (property.name == nameof(FDMiRelativeObjectSync.rootRefPoint))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = relativeObjectSyncManager;
                }
            }
        }
    }
}