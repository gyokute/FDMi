

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiStationAdjuster : UdonSharpBehaviour
    {
        [SerializeField] FDMiVector3 SeatOffset;
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

    }
}