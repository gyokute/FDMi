using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UdonSharp;
using UdonSharpEditor;
using tech.gyoku.FDMi.v2.aerodynamics;

namespace tech.gyoku.FDMi.v2.aerodynamics.editor
{
    [CustomEditor(typeof(Wing), true)]
    public class Wing_Editor : UnityEditor.Editor
    {
        private int wingSpanCount = 0;
        private int wingSpanIndex = 0, wingCoordIndex = 0;
        private SerializedProperty wingTip, wingToe, CoordPos, CoordNormal, SpanNormal, PlanformNormal, wingArea, coord, alpha;
        private SerializedProperty Cl_Alpha, Cd_Alpha, Cl_Mach, Cd_Mach, Cm_alpha;
        private AnimationCurve copyBuffer = null;
        private Vector3 editPos = new Vector3(1, 1, 1);
        private bool _foldPlanform = true, foldBase = false;
        private float totalArea;

        public void OnEnable()
        {
            var FDMi_wing = target as Wing;

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            wingToe = serializedObject.FindProperty("wingToe");
            wingTip = serializedObject.FindProperty("wingTip");
            CoordPos = serializedObject.FindProperty("CoordPos");
            CoordNormal = serializedObject.FindProperty("CoordNormal");
            SpanNormal = serializedObject.FindProperty("SpanNormal");
            PlanformNormal = serializedObject.FindProperty("PlanformNormal");
            wingArea = serializedObject.FindProperty("wingArea");
            Cl_Alpha = serializedObject.FindProperty("Cl_Alpha");
            Cd_Alpha = serializedObject.FindProperty("Cd_Alpha");
            Cl_Mach = serializedObject.FindProperty("Cl_Mach");
            Cd_Mach = serializedObject.FindProperty("Cd_Mach");
            Cm_alpha = serializedObject.FindProperty("Cm_alpha");
            coord = serializedObject.FindProperty("coord");
            alpha = serializedObject.FindProperty("alpha");

            // loadwingSection(0);
            wingSpanCount = wingTip.arraySize;
            // setWingSection(Mathf.Max(wingSpanCount, 2));
        }
        // public override void OnInspectorGUI() { }


        private void setWingSection(int size)
        {
            wingSpanCount = size;
            bool toUpdate = false;
            foreach (var obj in new SerializedProperty[] { wingToe, wingTip })
            {
                if (obj.arraySize != size)
                {
                    toUpdate = true;
                    obj.arraySize = size;
                }
            }
            foreach (var obj in new SerializedProperty[] { CoordPos, CoordNormal, SpanNormal, PlanformNormal,  wingArea, Cl_Alpha, Cd_Alpha, Cl_Mach, Cd_Mach, Cm_alpha, coord, alpha })
            {
                if (obj.arraySize != size - 1)
                {
                    toUpdate = true;
                    obj.arraySize = size - 1;
                }
            }

            if (toUpdate)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
    }
}