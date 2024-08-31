

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiStationAdjuster : FDMiBehaviour
    {
        public FDMiVector3 SeatOffset;
        [SerializeField] Transform SeatPosition;
        public float seatAdjustStep = 0.1f;
        Vector3 initialSeatPos;
        void Start()
        {
            initialSeatPos = SeatPosition.localPosition;
            SeatOffset.subscribe(this, nameof(OffsetSeat));
        }
        public void OffsetSeat()
        {
            SeatPosition.localPosition = initialSeatPos + SeatOffset.data[0];
        }
        public void FDMiOnSeatExit()
        {
            SeatPosition.localPosition = initialSeatPos;
            SeatOffset.Data = Vector3.zero;
        }

        public void OffsetUp() { SeatOffset.Data += Vector3.up * seatAdjustStep; }
        public void OffsetDown() { SeatOffset.Data -= Vector3.up * seatAdjustStep; }
        public void OffsetForward() { SeatOffset.Data -= Vector3.forward * seatAdjustStep; }
        public void OffsetBack() { SeatOffset.Data += Vector3.forward * seatAdjustStep; }
        public void OffsetRight() { SeatOffset.Data += Vector3.right * seatAdjustStep; }
        public void OffsetLeft() { SeatOffset.Data -= Vector3.right * seatAdjustStep; }
    }
}