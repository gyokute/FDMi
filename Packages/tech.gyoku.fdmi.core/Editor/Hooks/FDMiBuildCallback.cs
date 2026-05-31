using VRC.SDKBase.Editor.BuildPipeline;

namespace tech.gyoku.FDMi.core.Editor.Hooks
{
    /// <summary>
    /// VRChat SDK ビルド直前に FDMiDataPath を解決するコールバック。
    /// IVRCSDKBuildRequestedCallback を実装し、ビルドパイプラインに自動登録される。
    /// </summary>
    public class FDMiBuildCallback : IVRCSDKBuildRequestedCallback
    {
        /// <summary>コールバックの実行順序。0 = 最優先。</summary>
        public int callbackOrder => 0;

        /// <summary>
        /// VRChat SDK ビルド要求時に呼ばれる。FDMiDataPath を解決してからビルドを続行する。
        /// </summary>
        /// <param name="requestedBuildType">ビルド種別（Build のみ / Build + Upload）。</param>
        /// <returns>true を返してビルドを続行する。</returns>
        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            FDMiEditorHooks.ResolveAll();
            return true;
        }
    }
}
