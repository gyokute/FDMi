using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using tech.gyoku.FDMi.core;
using tech.gyoku.FDMi.core.editor.process;

namespace tech.gyoku.FDMi.core.editor
{
    public class FDMiEditorExtSetupAll : Editor
    {
        [MenuItem("FDMi/Setup All FDMi Components", false, 1000)]
        public static void setupAllFDMiComponents()
        {
            var processClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(x => x.IsSubclassOf(typeof(FDMiEditorProcess)) && !x.IsAbstract)
                .Select(pc => new
                {
                    process = pc,
                    priority = Attribute.GetCustomAttribute(pc, typeof(FDMiEditorProcessPriorityAttribute)) as FDMiEditorProcessPriorityAttribute
                })
                .OrderBy(d => d.priority != null ? d.priority.priority : Int32.MaxValue).ToArray();
            for (int i = 0; i < processClasses.Length; i++)
            {
                var processClass = processClasses[i];
                EditorUtility.DisplayProgressBar("Running All FDMi Setup Processes", $"process > {nameof(processClass)}", i / processClasses.Length);
                NewExpression body = Expression.New(processClass.process);
                var expression = Expression.Lambda<Func<FDMiEditorProcess>>(body).Compile();
                var instance = expression();
                instance.execute();
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("FDMi/Setup/Setup Layer", false, 1000)]
        public static void setupLayer()
        {
            // 29番にBoardingColliderを追加
            var layer = 29;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
            tagManager.Update();

            var layersProperty = tagManager.FindProperty("layers");
            layersProperty.arraySize = Mathf.Max(layersProperty.arraySize, layer);
            layersProperty.GetArrayElementAtIndex(29).stringValue = "BoardingCollider";

            tagManager.ApplyModifiedProperties();
            // 29番のコライダー判定はPlayerLocal,boardingCollider以外無効にする
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(layer, i, true);
            var playerLayerId = LayerMask.NameToLayer("PlayerLocal");
            Physics.IgnoreLayerCollision(layer, playerLayerId, false);
            playerLayerId = LayerMask.NameToLayer("BoardingCollider");
            Physics.IgnoreLayerCollision(layer, playerLayerId, false);
        }

    }
}