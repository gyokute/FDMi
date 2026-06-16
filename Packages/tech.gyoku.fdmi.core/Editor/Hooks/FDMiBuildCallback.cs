using VRC.SDKBase.Editor.BuildPipeline;

namespace FDMi.core.Editor.Hooks
{
    public class FDMiBuildCallback : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            FDMiEditorHooks.ResolveAndRegisterAll();
            return true;
        }
    }
}
