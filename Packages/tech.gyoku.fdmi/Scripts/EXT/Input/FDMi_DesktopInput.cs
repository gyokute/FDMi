
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UdonSharpEditor;
#endif

namespace SaccFlight_FDMi
{
    public class FDMi_DesktopInput : UdonSharpBehaviour
    {
        public FDMi_InputObject[] KeyboardInput;
        public int[] UpKey, DownKey;
        private bool pressPrev = false;
        public void inputUpdate()
        {
            if (!Input.anyKey && !pressPrev) return;
            pressPrev=false;
            for (int i = 0; i < KeyboardInput.Length; i++)
            {
                if (Input.GetKey((KeyCode)UpKey[i])) {KeyboardInput[i].whenPressUp(); pressPrev=true;}
                if (Input.GetKey((KeyCode)DownKey[i])) {KeyboardInput[i].whenPressDown(); pressPrev=true;}
                if (!Input.GetKey((KeyCode)UpKey[i]) && !Input.GetKey((KeyCode)DownKey[i])) KeyboardInput[i].whenReleaseKey();
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(FDMi_DesktopInput))]
    public class FDMi_DesktopInput_Editor : Editor
    {
        public override void OnInspectorGUI()
        {

            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var tgt = target as FDMi_DesktopInput;
            serializedObject.Update();
            var KI = serializedObject.FindProperty("KeyboardInput");
            for(int i=0; i<KI.arraySize;i++){
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.ExpandWidth(false))) {RemoveFromArray(i); return;}
                    if(KI.GetArrayElementAtIndex(i).objectReferenceValue == null){
                        KI.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("KeyboardInput").GetArrayElementAtIndex(i), true);
                    var upk = serializedObject.FindProperty("UpKey").GetArrayElementAtIndex(i);
                    KeyCode upkE = (KeyCode)EditorGUILayout.EnumPopup("Up", (KeyCode)upk.intValue);
                    upk.intValue = (int)upkE;

                    var downk = serializedObject.FindProperty("DownKey").GetArrayElementAtIndex(i);
                    KeyCode downE = (KeyCode)EditorGUILayout.EnumPopup("Down", (KeyCode)downk.intValue);
                    downk.intValue = (int)downE;
                }
            }
            if (GUILayout.Button("Add", EditorStyles.miniButtonMid, GUILayout.ExpandWidth(true))) AddArray();
            serializedObject.ApplyModifiedProperties();
        }
        private void AddArray()
        {
            int newSize = serializedObject.FindProperty("KeyboardInput").arraySize + 1;
            foreach (string str in new string[] { "KeyboardInput", "UpKey", "DownKey"})
                serializedObject.FindProperty(str).arraySize = newSize;
        }
        private void RemoveFromArray(int i)
        {
            foreach (string str in new string[] { "KeyboardInput", "UpKey", "DownKey" })
                serializedObject.FindProperty(str).DeleteArrayElementAtIndex(i);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
#endif
}