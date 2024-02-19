
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiHeadPositionAdjuster : FDMiBehaviour
    {
        [SerializeField] Transform headPosition;
        [SerializeField] Transform seatEnterPosition;
        [SerializeField] FDMiVector3 SeatOffset;

        void Start()
        {

        }
        public void FDMiOnSeatEnter()
        {
            SendCustomEventDelayedSeconds(nameof(AdjustSeat), 0.5f);
            SendCustomEventDelayedSeconds(nameof(AdjustSeat), 0.8f);
        }
        public void AdjustSeat()
        {
            Vector3 HeadFromSeat = headPosition.position - seatEnterPosition.position;
            // VRCPlayerApi.TrackingData td = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            // Vector3 headData = td.position;
            Vector3 headData = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftEye);
            headData = Vector3.Lerp(headData, Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightEye), 0.5f);
            Vector3 posDiff = headData - headPosition.position;
            SeatOffset.Data = -posDiff;
        }
    }
}