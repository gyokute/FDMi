using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
namespace SaccFlight_FDMi
{
    public class FDMi_InputManager : FDMi_Attributes
    {
        public FDMi_InputGroup[] inputGroup;
        public Vector3[] handPosition = { Vector3.zero, Vector3.zero };
        public Vector3[] fingerPosition = { Vector3.zero, Vector3.zero };
        [System.NonSerializedAttribute] public float[] Trig = { 0f, 0f }, StickX = { 0f, 0f }, StickY = { 0f, 0f };
        [System.NonSerializedAttribute] public float[] LCtrlVal = { 0f, 0f, 0f };
        private int[] grabIndex = { -1, -1 };
        private int seatAdjustHand = -1;
        private Vector3 seatAdjustStartPos;
        public Transform[] debugTransform;
        public FDMi_DesktopInput desktopInput;

        #region SFEXT Core
        // public override void FDMi_Local_Start() => gameObject.SetActive(false);
        // public override void SFEXT_O_PilotEnter() => gameObject.SetActive(true);
        // public override void SFEXT_O_PilotExit() => gameObject.SetActive(false);
        // public override void SFEXT_P_PassengerEnter() => gameObject.SetActive(true);
        // public override void SFEXT_P_PassengerExit() => gameObject.SetActive(false);
        public override void ResetStatus()
        {
            for (int i = 0; i < inputGroup.Length; i++)
            {
                inputGroup[i].ResetStatus();
            }
        }

        #endregion
        private void LateUpdate()
        {
            if (!sharedBool[0]) return;
            if (!sharedBool[(int)SharedBool.isPilot] && !sharedBool[(int)SharedBool.isPassenger]) return;
            if (!sharedBool[(int)SharedBool.isVR] && desktopInput != null)
            {
                desktopInput.inputUpdate();
                return;
            }
            float LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            float RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");

            Trig[0] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            StickX[0] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
            StickY[0] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            Trig[1] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            StickX[1] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
            StickY[1] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            VRCPlayerApi.TrackingData RTD = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            handPosition[0] = RTD.position;
            VRCPlayerApi.TrackingData LTD = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            handPosition[1] = LTD.position;

            Vector3 RTHS = player.GetBonePosition(HumanBodyBones.RightIndexProximal);
            Vector3 LTHS = player.GetBonePosition(HumanBodyBones.LeftIndexProximal);

            fingerPosition[0] = player.GetBonePosition(HumanBodyBones.RightIndexProximal);
            fingerPosition[1] = player.GetBonePosition(HumanBodyBones.LeftIndexProximal);

            if (debugTransform.Length > 1)
            {
                if (debugTransform[0] != null) debugTransform[0].position = handPosition[0];
                if (debugTransform[1] != null) debugTransform[1].position = handPosition[1];
                // if (debugTransform[2] != null) debugTransform[2].position = fingerPosition[0];
                // if (debugTransform[3] != null) debugTransform[3].position = fingerPosition[1];
            }

            if (RGrip > 0.7 && grabIndex[0] < 0) whenGrab((int)HandType.RIGHT);
            if (RGrip < 0.3 && grabIndex[0] >= 0) whenRelease(HandType.RIGHT);
            if (LGrip > 0.7 && grabIndex[1] < 0) whenGrab((int)HandType.LEFT);
            if (LGrip < 0.3 && grabIndex[1] >= 0) whenRelease(HandType.LEFT);
            if (grabIndex[0] >= 0) inputGroup[grabIndex[0]].InputUpdate();
            if (grabIndex[1] >= 0) inputGroup[grabIndex[1]].InputUpdate();

            // Seat Adjuster (Trigger if no grab)
            if (grabIndex[0] == -1 && grabIndex[1] == -1)
            {
                if (Trig[0] > 0.7 && seatAdjustHand != 0) whenStartSeatAdjust(0);
                if (Trig[1] > 0.7 && seatAdjustHand != 1) whenStartSeatAdjust(1);

                if (seatAdjustHand >= 0)
                {
                    if (Trig[seatAdjustHand] < 0.3) seatAdjustHand = -1;
                    else whenHoldSeatAdjust();
                }
            }
        }

        #region GRAB
        private void whenRelease(HandType grabHand)
        {
            if (grabHand == HandType.RIGHT) inputGroup[grabIndex[(int)grabHand]].whenReleaseR();
            if (grabHand == HandType.LEFT) inputGroup[grabIndex[(int)grabHand]].whenReleaseL();
            grabIndex[(int)grabHand] = -1;
        }

        private void whenGrab(int handType)
        {
            // Stop Seat Adjust
            if (seatAdjustHand == handType) seatAdjustHand = -1;

            for (int i = 0; i < inputGroup.Length; i++)
            {
                Vector3 localHandPos = inputGroup[i].transform.InverseTransformPoint(handPosition[handType]);
                if (IsRoundedSquareHit(localHandPos, inputGroup[i].colliderPos, inputGroup[i].colliderRadius))
                {
                    grabIndex[handType] = i;
                    if (handType == (int)HandType.RIGHT)
                        inputGroup[i].whenGrabR();
                    if (handType == (int)HandType.LEFT)
                        inputGroup[i].whenGrabL();
                    // return; not return in inputManager
                }
            }
        }

        private bool IsRoundedSquareHit(Vector3 handPos, Vector3[] colliderPos, float radius)
        {
            for (int i = 0; i < colliderPos.Length - 1; i++)
            {
                Vector3 s2e = colliderPos[i + 1] - colliderPos[i];
                Vector3 s2h = handPos - colliderPos[i];
                Vector3 e2h = handPos - colliderPos[i + 1];
                float esh = Vector3.Dot(s2e, s2h);
                float seh = Vector3.Dot(-s2e, e2h);
                float t = esh / Vector3.Dot(s2e, s2e);
                if (t >= 0 && t <= 1 && (s2h - t * s2e).magnitude <= radius) return true;
                if (t < 0 && s2h.magnitude <= radius) return true;
                if (t > 1 && e2h.magnitude <= radius) return true;
            }
            return false;
        }
        #endregion

        #region SeatAdjuster
        public FDMi_VehicleSeat Seat;
        private Vector3 adjustedOrigin;
        public Vector3 stickMove;
        private bool grabLatch = false;
        public bool enableStickMove = false;

        private void whenHoldSeatAdjust()
        {
            if (!enableStickMove)
            {
                Seat.AdjustedPos = adjustedOrigin - (transform.InverseTransformPoint(handPosition[seatAdjustHand]) - seatAdjustStartPos);
                return;
            }
            Seat.AdjustedRot += 90f * StickX[0] * Time.deltaTime;
            stickMove = Quaternion.AngleAxis(Seat.AdjustedRot, Vector3.up) * (StickY[1] * Vector3.forward + StickX[1] * Vector3.right + StickY[0] * 0.3f * Vector3.up);
            Seat.AdjustedPos += player.GetRunSpeed() * Time.deltaTime * stickMove;
        }
        private void whenStartSeatAdjust(int handType)
        {
            adjustedOrigin = Seat.AdjustedPos;
            seatAdjustStartPos = transform.InverseTransformPoint(handPosition[handType]);
            seatAdjustHand = handType;
        }
        #endregion

        #region event
        public void whenExitPlane()
        {
            if (grabIndex[(int)HandType.RIGHT] != -1) whenRelease(HandType.RIGHT);
            if (grabIndex[(int)HandType.LEFT] != -1) whenRelease(HandType.LEFT);
        }
        #endregion
    }
}