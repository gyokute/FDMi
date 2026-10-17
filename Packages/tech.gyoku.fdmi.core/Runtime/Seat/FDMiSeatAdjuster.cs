using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiSeatAdjuster : FDMiBehaviour
    {
        public string seatOffsetPath = "SeatOffset";

        [FDMiDataPath(nameof(seatOffsetPath)), FDMiRegisterCallback(nameof(OffsetSeat))]
        public FDMiVector3 SeatOffset;

        public float seatAdjustStep = 0.1f;
        public Vector3 initialSeatPos;

        public void OffsetSeat()
        {
            transform.localPosition = initialSeatPos + SeatOffset.data[0];
        }

        // public void FDMiOnSeatEnter()
        // {
        //     initialSeatPos = transform.localPosition;
        // }

        public void FDMiOnSeatExit()
        {
            transform.localPosition = initialSeatPos;
            SeatOffset.Data = Vector3.zero;
        }

        public void OffsetUp()
        {
            SeatOffset.Data += Vector3.up * seatAdjustStep;
        }

        public void OffsetDown()
        {
            SeatOffset.Data -= Vector3.up * seatAdjustStep;
        }

        public void OffsetForward()
        {
            SeatOffset.Data -= Vector3.forward * seatAdjustStep;
        }

        public void OffsetBack()
        {
            SeatOffset.Data += Vector3.forward * seatAdjustStep;
        }

        public void OffsetRight()
        {
            SeatOffset.Data += Vector3.right * seatAdjustStep;
        }

        public void OffsetLeft()
        {
            SeatOffset.Data -= Vector3.right * seatAdjustStep;
        }
    }
}
