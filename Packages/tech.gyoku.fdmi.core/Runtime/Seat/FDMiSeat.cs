using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiSeat : FDMiBehaviour
    {
        public VRCStation station;
        public int pilotPriority = -1;

        public VRCPlayerApi seatedPlayer;
        public UdonSharpBehaviour[] InSeatBehaviours;

        [FDMiDataPathConst("~/IsPilot")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/HasPilot")]
        public FDMiBool HasPilot;

        [FDMiDataPathConst("~/PilotID")]
        public FDMiInt PilotID;

        [FDMiDataPathConst("~/CurrentPilotPriority")]
        public FDMiInt CurrentPilotPriority;

        public override void Interact()
        {
            Networking.LocalPlayer.UseAttachedStation();
        }

        public void ExitStation()
        {
            station.ExitStation(Networking.LocalPlayer);
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            seatedPlayer = player;
            if (player.isLocal)
            {
                TryTakePilot();
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatEnter");
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            seatedPlayer = null;
            if (player.isLocal)
            {
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatExit");
            }
        }

        #region Pilot Management
        // 座席への着席によるパイロット獲得。獲得者自身のクライアントのみが共有状態に書き込む。
        // 現パイロットが存在する場合のみ優先度を比較し、現パイロットより高い PilotPriority でなければ獲得しない。
        // 同一 priority の獲得競合 (着席順の入れ替わりによる再獲得の連鎖) は無視(同じ優先度の席がないことを期待する)
        public void TryTakePilot()
        {
            if (seatedPlayer.isLocal != true || pilotPriority < 0)
                return;
            if (HasPilot.Data && pilotPriority <= CurrentPilotPriority.Data)
                return;
            IsPilot.Data = true;
            HasPilot.Data = true;
            PilotID.Data = Networking.LocalPlayer.playerId;
            CurrentPilotPriority.Data = pilotPriority;
        }
        #endregion
    }
}
