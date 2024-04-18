using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using tech.gyoku.FDMi.core;
using tech.gyoku.FDMi.core.editor;

namespace tech.gyoku.FDMi.editor
{
    public class FDMiEditorExtSetupAll : Editor
    {
        [MenuItem("TESI/FDMi/Setup All FDMi Behaviours", false, 1000)]
        public static void setupAllFDMiComponents()
        {
            var FDMiBehaviours = Resources.FindObjectsOfTypeAll<FDMiBehaviour>();
            var classes = Assembly.GetAssembly(typeof(FDMiEditorExt)).GetTypes().Where(x => x.IsSubclassOf(typeof(FDMiEditorExt)) && !x.IsAbstract);
            classes = classes.Append(typeof(FDMiEditorExt));

            foreach (FDMiBehaviour behaviour in FDMiBehaviours)
            {
                foreach (var cl in classes)
                {
                    var editorComponent = Editor.CreateEditor(behaviour, cl);
                    if (editorComponent != null)
                    {
                        ((dynamic)editorComponent).SetupAll(behaviour, editorComponent.serializedObject);
                        DestroyImmediate(editorComponent);
                    }
                }
            }
        }
        [MenuItem("TESI/FDMi/Settings/Setup Layer", false, 1000)]
        public static void setupLayer()
        {
            // 29番にBoardingColliderを追加
            var layer = 29;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
            tagManager.Update();

            var layersProperty = tagManager.FindProperty("layers");
            layersProperty.arraySize = Mathf.Max(layersProperty.arraySize, layer);
            layersProperty.GetArrayElementAtIndex(29).stringValue =  "BoardingCollider";

            tagManager.ApplyModifiedProperties();
            // 29番のコライダー判定はPlayerLocal以外無効にする
            for(int i=0; i<32; i++) Physics.IgnoreLayerCollision(layer, i, true);
            var playerLayerId = LayerMask.NameToLayer("PlayerLocal");
            Physics.IgnoreLayerCollision(layer, playerLayerId, false);
        }

    }
}