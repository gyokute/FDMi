using FDMi.input;
using UnityEditor;
using UnityEngine;

namespace FDMi.input.Editor
{
    [CustomEditor(typeof(FDMiHandDFUNC))]
    public class FDMiHandDFUNCEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Build DFUNC Display"))
                FDMiHandDFUNCDisplayBuilder.Build((FDMiHandDFUNC)target);
        }
    }
}
