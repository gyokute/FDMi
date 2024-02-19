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
    [CustomEditor(typeof(FDMiPlayerSyncManager), true)]
    public class FDMiPlayerSyncManagerEditor : FDMiAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiPlayerSyncManager.assigners))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    FDMiPlayerPositionAssigner[] behaviours = FDMiEditorUI.FindChildrenComponents<FDMiPlayerPositionAssigner>(tgt).ToArray();
                    FDMiEditorUI.SetObjectArrayProperty<FDMiPlayerPositionAssigner>(property, behaviours);
                }
            }
        }

    }
}
