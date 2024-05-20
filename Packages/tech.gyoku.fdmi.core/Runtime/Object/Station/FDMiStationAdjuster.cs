

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

    }
}