
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using SaccFlightAndVehicles;

namespace SaccFlight_FDMi
{
    public class FDMi_SeatAdjuster : FDMi_InputObject
    {
        [SerializeField] private FDMi_VehicleSeat Seat;
        private Vector3 adjustedOrigin;
        private bool grabLatch = false;

        public override void InputUpdate()
        {
            base.InputUpdate();
            Seat.AdjustedPos = adjustedOrigin - (handPos - handStartPos);
        }
        public override void whenGrab()
        {
            adjustedOrigin = Seat.AdjustedPos;
        }
    }
}