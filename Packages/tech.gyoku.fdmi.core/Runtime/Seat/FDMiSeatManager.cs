using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiSeatManager : FDMiBehaviour
    {
        [Header("FDMiSeatと組み合わせ、Seatに座ったプレイヤーから、パイロットを一意に決定します。")]
        public FDMiSeat[] allSeats;

        [FDMiDataPathConst("~/HasPilot"), FDMiRegisterCallback(nameof(DetermineNewPilot))]
        public FDMiBool HasPilot;

        public void DetermineNewPilot()
        {
            if (HasPilot.Data == false)
            {
                int highestPriority = -1;
                FDMiSeat newPilotSeat = null;

                foreach (FDMiSeat seat in allSeats)
                {
                    if (seat.pilotPriority > highestPriority && seat.seatedPlayer != null)
                    {
                        highestPriority = seat.pilotPriority;
                        newPilotSeat = seat;
                    }
                }

                if (newPilotSeat == null)
                    return;

                if (newPilotSeat.seatedPlayer == Networking.LocalPlayer)
                    newPilotSeat.TryTakePilot();
            }
        }

        public void OnPlayerLeft(VRCPlayerApi player)
        {
            if (HasPilot.Data)
                DetermineNewPilot();
        }
    }
}
