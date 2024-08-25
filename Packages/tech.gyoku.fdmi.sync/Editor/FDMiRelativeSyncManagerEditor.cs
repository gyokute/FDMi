#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using tech.gyoku.FDMi.sync;

namespace tech.gyoku.FDMi.sync.editor
{
    public class FDMiRelativeSyncManagerEditor : MonoBehaviour
    {
        [InitializeOnLoadAttribute]
        public static class PlayModeStateChanged
        {
            static PlayModeStateChanged()
            {
                EditorApplication.playModeStateChanged += SetupSyncStuff;
            }

            private static void SetupSyncStuff(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    SetObjectReferences.setupSync();
                    EditorApplication.playModeStateChanged -= SetupSyncStuff;
                }
            }
        }
    }

    public class SetObjectReferences : Editor, IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 10;
        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            setupSync();
            return true;
        }
        [MenuItem("FDMi/Sync/Setup sync", false, 1000)]
        public static void setupSync()
        {
            setUpSyncManager();
        }

        public static void setUpSyncManager()
        {
            FDMiRelativeObjectSyncManager man = FindObjectOfType<FDMiRelativeObjectSyncManager>();
            man.refPoints = FindObjectsOfType<FDMiReferencePoint>();
            foreach(var rp in man.refPoints){
                rp.syncManager = man;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(man);
            EditorUtility.SetDirty(man);
        }
    }
}
#endif