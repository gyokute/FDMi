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


namespace Saccflight_FDMi.Editor
{
    [CustomEditor(typeof(FDMi_wing), true)]
    public class FDMi_wing_Editor : UnityEditor.Editor
    {
        private int wingSpanCount = 0;
        private int wingSpanIndex = 0, wingCoordIndex = 0;
        private SerializedProperty wingTip, wingToe, CoordPos, CoordNormal, SpanNormal, PlanformNormal, rNormal, wingArea, coord, alpha;
        private SerializedProperty Cl_Alpha, Cd_Alpha, Cl_Mach, Cd_Mach, Cm_alpha;
        private AnimationCurve copyBuffer = null;
        private Vector3 editPos = new Vector3(1, 1, 1);
        private bool _foldPlanform = true, foldBase = false;
        private float totalArea;

        public void OnEnable()
        {
            var FDMi_wing = target as FDMi_wing;

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            wingToe = serializedObject.FindProperty("wingToe");
            wingTip = serializedObject.FindProperty("wingTip");
            CoordPos = serializedObject.FindProperty("CoordPos");
            CoordNormal = serializedObject.FindProperty("CoordNormal");
            SpanNormal = serializedObject.FindProperty("SpanNormal");
            PlanformNormal = serializedObject.FindProperty("PlanformNormal");
            rNormal = serializedObject.FindProperty("rNormal");
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
            setWingSection(Mathf.Max(wingSpanCount, 2));
        }
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var FDMi_wing = target as FDMi_wing;
            serializedObject.Update();

            wingSpanCount = Mathf.Max(2, wingTip.arraySize);
            setWingSection(wingSpanCount);

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (property.name == nameof(FDMi_wing.Param))
                    {
                        EditorGUILayout.PropertyField(property, true);
                        if (FDMi_UI.Button("Find")) FDMi_Util.SetParamator(property, FDMi_Util.FindParentComponent<FDMi_SharedParam>(FDMi_wing));
                    }
                }
            }

            _foldPlanform = EditorGUILayout.Foldout(_foldPlanform, "wing Definition/翼性能定義");
            if (_foldPlanform)
            {

                GUILayout.Label("Planform Definition/平面形定義");
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Section " + (wingSpanIndex + 1) + "/" + wingSpanCount);
                        if (FDMi_UI.Button("+")) setWingSection(wingSpanCount + 1);
                        if (FDMi_UI.Button("-")) setWingSection(Mathf.Max(wingSpanCount - 1, 2));
                        if (FDMi_UI.Button("<")) { wingSpanIndex = Mathf.Max(0, wingSpanIndex - 1); }
                        if (FDMi_UI.Button(">")) { wingSpanIndex = Mathf.Min(wingSpanCount - 1, wingSpanIndex + 1); }
                        if (FDMi_UI.Button("Mirror"))
                        {
                            for (int i = wingSpanCount / 2 + 1; i < wingSpanCount; i++)
                            {
                                wingTip.GetArrayElementAtIndex(i).vector3Value = Vector3.Reflect(wingTip.GetArrayElementAtIndex(wingSpanCount - i - 1).vector3Value, Vector3.right);
                                wingToe.GetArrayElementAtIndex(i).vector3Value = Vector3.Reflect(wingToe.GetArrayElementAtIndex(wingSpanCount - i - 1).vector3Value, Vector3.right);
                            }
                        }
                    }
                    foreach (var sp in new SerializedProperty[2] { wingTip, wingToe })
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(sp.name);
                            if (FDMi_UI.Button("Put")) { sp.GetArrayElementAtIndex(wingSpanIndex).vector3Value = editPos; }
                            if (FDMi_UI.Button("Get"))
                            {
                                editPos = sp.GetArrayElementAtIndex(wingSpanIndex).vector3Value;
                                EditorApplication.QueuePlayerLoopUpdate();
                            }
                            EditorGUILayout.PropertyField(sp.GetArrayElementAtIndex(wingSpanIndex), GUIContent.none);
                        }
                    }
                }
                FDMi_Util.DrawProperty(serializedObject, "wingAreaCoef");
                GUILayout.Label("Foil Paramator Definition/翼型定義");
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.VerticalScope())
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Section " + (wingCoordIndex + 1) + "/" + (wingSpanCount - 1));
                            if (FDMi_UI.Button("<")) { wingCoordIndex = Mathf.Max(0, wingCoordIndex - 1); }
                            if (FDMi_UI.Button(">")) { wingCoordIndex = Mathf.Min(wingSpanCount - 2, wingCoordIndex + 1); }
                        }
                        foreach (var sp in new SerializedProperty[5] { Cl_Alpha, Cd_Alpha, Cm_alpha, Cl_Mach, Cd_Mach })
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(sp.name);
                                sp.GetArrayElementAtIndex(wingCoordIndex).animationCurveValue = EditorGUILayout.CurveField(sp.GetArrayElementAtIndex(wingCoordIndex).animationCurveValue);
                                if (FDMi_UI.Button("Load")) { sp.GetArrayElementAtIndex(wingCoordIndex).animationCurveValue = AnimationCurveUtils.LoadCSVToAnimationCurve(); }
                                if (FDMi_UI.Button("Copy")) { copyBuffer = sp.GetArrayElementAtIndex(wingCoordIndex).animationCurveValue; }
                                if (FDMi_UI.Button("Paste") && copyBuffer != null) { sp.GetArrayElementAtIndex(wingCoordIndex).animationCurveValue = copyBuffer; }
                            }
                        }
                    }
                }
                GUILayout.Label("Control Surfaces/動翼");
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    FDMi_UI.PropertyField(serializedObject, "controlSurface");
                    if (FDMi_UI.Button("Find"))
                    {
                        SerializedProperty sp = serializedObject.FindProperty("controlSurface");
                        List<FDMi_ControlSurface> surfaces = FDMi_Util.FindChildrenComponents<FDMi_ControlSurface>(FDMi_wing).ToList<FDMi_ControlSurface>();
                        sp.arraySize = surfaces.Count;
                        for (int spi = 0; spi < surfaces.Count; spi++)
                            sp.GetArrayElementAtIndex(spi).objectReferenceValue = surfaces[spi];
                    }
                }
            }
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("summary/設計情報");
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("total Wing Area/翼面積");
                    GUILayout.Label(totalArea * serializedObject.FindProperty("wingAreaCoef").floatValue + " m^2");
                }
            }
            foldBase = EditorGUILayout.Foldout(foldBase, "入力データ");
            if (foldBase) base.OnInspectorGUI();

            setPlanformData();
            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            var FDMi_wing = target as FDMi_wing;
            serializedObject.Update();

            editPos = GUIHandle.handlePosition(FDMi_wing.transform, editPos, "wing Section");

            drawPlanform(FDMi_wing);
            serializedObject.ApplyModifiedProperties();
        }


        public static void drawPlanform(FDMi_wing wing)
        {
            using (new Handles.DrawingScope(wing.transform.localToWorldMatrix))
            {
                Handles.color = Color.magenta;
                for (int i = 1; i < wing.wingTip.Length; i++)
                {
                    Handles.color = Color.magenta;
                    Handles.DrawLine(wing.wingTip[i], wing.wingTip[i - 1]);
                    Handles.DrawLine(wing.wingToe[i], wing.wingToe[i - 1]);
                    Handles.color = Color.red;
                    Handles.DrawLine(wing.CoordPos[i - 1], wing.CoordPos[i - 1] + 2 * wing.PlanformNormal[i - 1]);
                    Handles.color = Color.blue;
                    Handles.DrawLine(wing.CoordPos[i - 1], wing.CoordPos[i - 1] + 2 * wing.SpanNormal[i - 1]);
                    Handles.color = Color.green;
                    Handles.DrawLine(wing.CoordPos[i - 1], wing.CoordPos[i - 1] + 2 * wing.CoordNormal[i - 1]);
                    // var guiStyle = new GUIStyle { fontSize = 20, normal = { textColor = Color.red } };
                    Handles.Label(wing.CoordPos[i - 1], "Sec" + (i - 1).ToString());
                }
            }
        }

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
            foreach (var obj in new SerializedProperty[] { CoordPos, CoordNormal, SpanNormal, PlanformNormal, rNormal, wingArea, Cl_Alpha, Cd_Alpha, Cl_Mach, Cd_Mach, Cm_alpha, coord, alpha })
            {
                if (obj.arraySize != size - 1)
                {
                    toUpdate = true;
                    obj.arraySize = size - 1;
                }
            }

            // controlSurfaceMul.arraySize = (int)SaccFlight_FDMi.ControlSurfaceType.Length;
            // for (int csm_t = 0; csm_t < controlSurfaceMul.arraySize; csm_t++)
            // {
            //     controlSurfaceMul.GetArrayElementAtIndex(csm_t).arraySize = size - 1;
            //     for (int i = 0; i < size - 1; i++)
            //     {
            //         controlSurfaceMul.GetArrayElementAtIndex(csm_t).GetArrayElementAtIndex(i).arraySize = (int)SaccFlight_FDMi.ControlSurfaceAttribute.Length;
            //     }
            // }
            if (toUpdate)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        private void setPlanformData()
        {
            totalArea = 0;
            wingArea.arraySize = wingSpanCount - 1;
            for (int i = 0; i < wingSpanCount - 1; i++)
            {
                int lIndex, rIndex;
                if (wingTip.GetArrayElementAtIndex(i).vector3Value.x - wingTip.GetArrayElementAtIndex(i + 1).vector3Value.x < -0.05f)
                {
                    lIndex = i + 1;
                    rIndex = i;
                }
                else
                {
                    lIndex = i;
                    rIndex = i + 1;
                }
                Vector3 l25Coord = Vector3.Lerp(wingTip.GetArrayElementAtIndex(lIndex).vector3Value, wingToe.GetArrayElementAtIndex(lIndex).vector3Value, 0.25f);
                Vector3 r25Coord = Vector3.Lerp(wingTip.GetArrayElementAtIndex(rIndex).vector3Value, wingToe.GetArrayElementAtIndex(rIndex).vector3Value, 0.25f);
                Vector3 lCoord = wingTip.GetArrayElementAtIndex(lIndex).vector3Value - wingToe.GetArrayElementAtIndex(lIndex).vector3Value;
                Vector3 rCoord = wingTip.GetArrayElementAtIndex(rIndex).vector3Value - wingToe.GetArrayElementAtIndex(rIndex).vector3Value;
                // CoordPos
                CoordPos.GetArrayElementAtIndex(i).vector3Value = Vector3.Lerp(r25Coord, l25Coord, 0.5f);
                if (rIndex < lIndex)
                {
                    SpanNormal.GetArrayElementAtIndex(i).vector3Value = Vector3.Normalize(r25Coord - l25Coord);
                    PlanformNormal.GetArrayElementAtIndex(i).vector3Value = Vector3.Normalize(Vector3.Cross(Vector3.forward, SpanNormal.GetArrayElementAtIndex(i).vector3Value));
                }
                else
                {
                    SpanNormal.GetArrayElementAtIndex(i).vector3Value = Vector3.Normalize(l25Coord - r25Coord);
                    PlanformNormal.GetArrayElementAtIndex(i).vector3Value = Vector3.Normalize(Vector3.Cross(Vector3.forward, SpanNormal.GetArrayElementAtIndex(i).vector3Value));
                }
                rNormal.GetArrayElementAtIndex(i).vector3Value = Vector3.Cross(PlanformNormal.GetArrayElementAtIndex(i).vector3Value, Vector3.forward);
                SpanNormal.GetArrayElementAtIndex(i).vector3Value = Vector3.Lerp(SpanNormal.GetArrayElementAtIndex(i).vector3Value, rNormal.GetArrayElementAtIndex(i).vector3Value, 0.5f);
                CoordNormal.GetArrayElementAtIndex(i).vector3Value = Vector3.Normalize(Vector3.Cross(PlanformNormal.GetArrayElementAtIndex(i).vector3Value, SpanNormal.GetArrayElementAtIndex(i).vector3Value));
                Vector3 span = wingTip.GetArrayElementAtIndex(i + 1).vector3Value - wingTip.GetArrayElementAtIndex(i).vector3Value;
                Vector3 areaNormal = Vector3.Normalize(Vector3.Cross(Vector3.forward, PlanformNormal.GetArrayElementAtIndex(i).vector3Value));
                float Area = Mathf.Abs(0.5f * Vector3.Project(span, areaNormal).magnitude * (lCoord.z + rCoord.z));
                wingArea.GetArrayElementAtIndex(i).floatValue = Area;
                Vector3 tipcenter = Vector3.Lerp(wingTip.GetArrayElementAtIndex(lIndex).vector3Value, wingTip.GetArrayElementAtIndex(rIndex).vector3Value, 0.5f);
                Vector3 toecenter = Vector3.Lerp(wingToe.GetArrayElementAtIndex(lIndex).vector3Value, wingToe.GetArrayElementAtIndex(rIndex).vector3Value, 0.5f);
                coord.GetArrayElementAtIndex(i).floatValue = (tipcenter - toecenter).magnitude;
                totalArea += Area;
            }
        }

    }
}
