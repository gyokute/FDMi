
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
    [CustomEditor(typeof(FDMiRelativeSyncStationManager), true)]
    public class FDMiRelativeSyncStationManagerEditor : FDMiSyncAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiStationManager.stations))
            {
                if ((FDMiEditorUI.Button(forceSetup, "Find") || forceSetup))
                {
                    FDMiStation[] stations = FDMiEditorUI.FindChildrenComponents<FDMiStation>(tgt)/*.Where(s => s.pilotPriority < 1)*/.OrderBy(s => s.pilotPriority).ToArray();
                    FDMiEditorUI.SetObjectArrayProperty<FDMiStation>(property, stations);
                }
            }
            if (property.name == nameof(FDMiRelativeSyncStationManager.syncManager))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = FindObjectOfType<FDMiRelativeObjectSyncManager>();
                }
            }
            if (property.name == nameof(FDMiRelativeSyncStationManager.refPoint))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    FDMiObjectManager man = tgt.GetComponentInParent<FDMiObjectManager>();
                    property.objectReferenceValue = man.GetComponentInChildren<FDMiReferencePoint>();
                }
            }
        }
    }
}