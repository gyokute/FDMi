
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
    [CustomEditor(typeof(FDMiRelativeSyncStation), true)]
    public class FDMiRelativeSyncStationEditor : FDMiStationEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiRelativeSyncStation.syncManager))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    // find through scene... and it's maybe heavy!
                    var objs = FindObjectsOfType<FDMiRelativeObjectSyncManager>();
                    var ret = objs.FirstOrDefault(o => o);
                    property.objectReferenceValue = ret;
                }
            }

        }

    }
}