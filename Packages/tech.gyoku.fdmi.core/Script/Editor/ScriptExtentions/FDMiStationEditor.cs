
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
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    UdonSharpBehaviour[] behaviours = FDMiEditorUI.FindChildrenComponents<UdonSharpBehaviour>(tgt).ToArray();
                    FDMiEditorUI.SetObjectArrayProperty<UdonSharpBehaviour>(property, behaviours);
                }
            }
            if (property.name == nameof(FDMiStation.onlyInSeat))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    GameObject obj = tgt.transform.Find("OnlyInSeat").gameObject;
                    property.objectReferenceValue = obj ? obj : property.objectReferenceValue;
                }
            }
            if (property.name == nameof(FDMiStation.stationManager))
            {
                if ((forceSetup ? true : FDMiEditorUI.Button(forceSetup, "Find")))
                {
                    property.objectReferenceValue = tgt.GetComponentInParent<FDMiStationManager>();
                }
            }
        }

    }
}