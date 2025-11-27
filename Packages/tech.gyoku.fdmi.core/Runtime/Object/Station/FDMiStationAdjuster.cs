

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiStationAdjuster : FDMiBehaviour
    {
        public FDMiVector3 SeatOffset;
        [SerializeField] Transform SeatPosition;
        [SerializeField] float seatAdjustStep = 0.1f;
        Vector3 initialSeatPos;
        void Start()
        {
            initialSeatPos = SeatPosition.localPosition;
            SeatOffset.subscribe(this, nameof(OffsetSeat));
        }
        public void OffsetSeat()
        {
            this.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ReceiveOffset), SeatOffset.Data);
        }

        [NetworkCallable]
        public void ReceiveOffset(Vector3 offset)
        {
            SeatOffset.data[0] = offset;
            SeatPosition.localPosition = initialSeatPos + offset;
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