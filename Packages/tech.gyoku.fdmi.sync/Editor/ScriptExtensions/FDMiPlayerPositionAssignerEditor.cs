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
    [CustomEditor(typeof(FDMiPlayerPositionAssigner), true)]
    public class FDMiPlayerPositionAssignerEditor : FDMiSyncAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiPlayerPositionAssigner.playerSyncMan))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = tgt.GetComponentInParent<FDMiPlayerSyncManager>();
                }
            }
            if (property.name == nameof(FDMiPlayerPositionAssigner.playerPosition))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = tgt.GetComponentInChildren<FDMiPlayerPosition>();
                }
            }
        }

    }
}
