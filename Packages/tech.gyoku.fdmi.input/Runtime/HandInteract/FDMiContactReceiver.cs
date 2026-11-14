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
            // VRChat の Contact は各クライアントで全プレイヤー分シミュレートされる。
            // 他人の接触をローカルの HandInput と Haptic に反映しないため、接触元が自分のときだけ反応する。
            VRCPlayerApi contactPlayer = contactInfo.contactSender.player;
            if (contactPlayer == null || !contactPlayer.isLocal)
                return;

            foreach (var t in Trackers)
            {
                t.SetContactInput(contactInfo.matchingTags[0], this);
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            VRCPlayerApi contactPlayer = contactInfo.contactSender.player;
            if (contactPlayer == null || !contactPlayer.isLocal)
                return;

            // 同じ Receiver に左右両手が触れている場合に、片手の Exit でもう片方の接触候補を消さない。
            // Exit の matchingTags で手を特定し、該当する Tracker だけを解除する。
            foreach (var t in Trackers)
            {
                t.UnsetContactInput(contactInfo.matchingTags[0], this);
            }
        }
    }
}
