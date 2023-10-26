
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    [CustomEditor(typeof(FDMiStation), true)]
    public class FDMiStationEditor : FDMiAttributeEditor
    {
        public override void SetPropertyOption(Component tgt, SerializedProperty property, bool forceSetup)
        {
            base.SetPropertyOption(tgt, property, forceSetup);
            if (property.name == nameof(FDMiStation.InSeatBehaviours))
            {
                if ((FDMiEditorUI.Button("Find") || forceSetup))
                {
                    UdonSharpBehaviour[] behaviours = FDMiEditorUI.FindChildrenComponents<UdonSharpBehaviour>(tgt).ToArray();
                    FDMiEditorUI.SetObjectArrayProperty<UdonSharpBehaviour>(property, behaviours);
                }
            }
            if (property.name == nameof(FDMiStation.onlyInSeat))
            {
                if ((FDMiEditorUI.Button("Find") || forceSetup))
                {
                    GameObject obj = tgt.transform.Find("OnlyInSeat").gameObject;
                    property.objectReferenceValue = obj ? obj : property.objectReferenceValue;
                }
            }
        }

    }
}