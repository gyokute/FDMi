using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiZoneDetector : FDMiBehaviour
    {
        [FDMiDataPathConst("~/InZone")]
        [Tooltip("ローカルプレイヤーがゾーン内にいるか")]
        public FDMiBool InZone;

        [SerializeField]
        [Tooltip("入ったとき InZone を立てる")]
        private bool DetectWhenEnter = true;

        [SerializeField]
        [Tooltip("出たとき InZone を下ろす")]
        private bool DetectWhenExit = true;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal && DetectWhenEnter)
                InZone.Data = true;
        }

        // ゾーン退出はローカルプレイヤーの資格にだけ影響する。
        // パイロットの権限喪失と再選出は FDMiPilotAuthority が InZone を読んで行う
        // (他プレイヤーのゾーン状態は同期しない)。
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal && DetectWhenExit)
                InZone.Data = false;
        }
    }
}
