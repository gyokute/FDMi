using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace FDMi.sync.Editor
{
    public class FDMiSyncBuildCallback : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 1;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            new FDMiSyncAutoSetup().ExecuteAll();
            return true;
        }
    }
}
