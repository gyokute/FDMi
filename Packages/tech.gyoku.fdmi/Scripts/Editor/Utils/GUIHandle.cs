using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using SaccFlight_FDMi;

namespace SaccFlight_FDMi
{
    public static class GUIHandle
    {
        public static Vector3 handlePosition(Transform transform, Vector3 editPos, string label)
        {
            using (new Handles.DrawingScope(transform.localToWorldMatrix))
            {
                EditorGUI.BeginChangeCheck();
                var newPosition = Handles.PositionHandle(
                    editPos,
                    Quaternion.identity
                );
                Handles.color = Color.magenta;
                Handles.Label(editPos, label);
                if (EditorGUI.EndChangeCheck())
                {
                    editPos = newPosition;
                }
                return editPos;
            }
        }

    }
}