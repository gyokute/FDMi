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

        [FDMiDataPathConst("~/IsPilot")]
        [Tooltip("この座席のプレイヤーがパイロットか")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/HasPilot")]
        [Tooltip("パイロットが存在するか")]
        public FDMiBool HasPilot;

        [FDMiDataPathConst("~/PilotID")]
        [Tooltip("パイロットのプレイヤー ID。不在は -1")]
        public FDMiInt PilotID;

        [SerializeField]
        [Tooltip("入ったとき InZone を立てる")]
        private bool DetectWhenEnter = true;

        [SerializeField]
        [Tooltip("出たとき InZone を下ろす。パイロットなら権限も解放する")]
        private bool DetectWhenExit = true;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal && DetectWhenEnter)
                InZone.Data = true;
        }

        // ローカルプレイヤーがゾーン外に出た。パイロット自身が出たときのみ権限を解放し、
        // ~/HasPilot の変更経由で新パイロットの再選出を起動する。
        // (乗客のゾーン外出ではパイロット状態に触らない。判定は ~/PilotID の一意性による)
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal && DetectWhenExit)
            {
                InZone.Data = false;
                if (PilotID.Data != Networking.LocalPlayer.playerId)
                    return;

                // ~/HasPilot の変更で再選出 (DetermineNewPilot) が同期的に走るため、
                // PilotID の解放は必ず先に書く (後書きの -1 が再選出の獲得値を潰さないようにする)。
                PilotID.Data = -1;
                IsPilot.Data = false;
                HasPilot.Data = false;
            }
        }
    }
}
