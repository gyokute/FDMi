using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiSeatAdjuster : FDMiBehaviour
    {
        [Tooltip("座席オフセットの解決パス。名前空間の区切りは /")]
        public string seatOffsetPath = "SeatOffset";

        [FDMiDataPath(nameof(seatOffsetPath)), FDMiRegisterCallback(nameof(OffsetSeat))]
        [Tooltip("座席の移動量 [m]")]
        public FDMiVector3 SeatOffset;

        [Tooltip("1 操作あたりの移動量 [m]")]
        public float seatAdjustStep = 0.1f;

        [Tooltip("基準のローカル位置。Editor が実位置から焼き付ける")]
        public Vector3 initialSeatPos;

        public void OffsetSeat()
        {
            transform.localPosition = initialSeatPos + SeatOffset.data[0];
        }

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
