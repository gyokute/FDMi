using FDMi.core;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;

namespace FDMi.input
{
    [UnityEngine.RequireComponent(typeof(VRCContactReceiver))]
    public class FDMiContactReceiver : FDMiHandInputGroup
    {
        [Tooltip("対象 Transform の解決パス。名前空間の区切りは /")]
        public string targetPath = "TargetTransform";

        [FDMiDataPath(nameof(targetPath))]
        [Tooltip("コンタクトの基準にする Transform。targetPath から解決する")]
        public FDMiTransformRef targetTransform;

        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            foreach (var t in Trackers)
            {
                t.SetContactInput(contactInfo.matchingTags[0], this);
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            foreach (var t in Trackers)
            {
                t.UnsetContactInput(this);
            }
        }
    }
}
