
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace SaccFlight_FDMi
{
    public enum LeverType { normal, detent, momentary }

    public enum LeverAxis { x, y, z }
    public class FDMi_Lever : FDMi_InputObject
    {
        [SerializeField] private LeverAxis movingAxis;
        public LeverType type;
        public float[] detent;
        public FDMi_SyncObject triggerObject;
        public KeyCode DesktopUp, DesktopDown;
        public float KeyboardMul = 1f;
        private bool detentLatch;
        [SerializeField] private float inputMul = 1f, min = 0f, max = 1f;
        private float rawInput, prevParam;
        private Vector3 axis = Vector3.zero;
        public override void Start()
        {
            base.Start();
            axis[(int)movingAxis] = 1f;
        }

        public override void InputUpdate()
        {
            // if (handType < 0) return;
            base.InputUpdate();
            // Quaternion q = Quaternion.FromToRotation(handStartPos, handPos);
            // rawInput = q.eulerAngles[(int)movingAxis];
            rawInput = Vector3.SignedAngle(handStartPos, handPos, axis);
            // rawInput = rawInput - Mathf.Floor(rawInput / 180.1f) * 360;
            Val = Mathf.Clamp(rawInput * inputMul + prevParam, min, max);
            if (triggerObject != null)
            {
                triggerObject.Val = Trig[handType];
                triggerObject.RequestSerialization();
            }
            RequestSerialization();
        }
        public override void whenGrab()
        {
            prevParam = val;
            if (triggerObject != null) Networking.SetOwner(player, triggerObject.gameObject);
            // if (handType == 0) player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1f, 1f, 1f);
            // if (handType == 1) player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1f, 1f, 1f);
        }

        public override void whenRelease()
        {
            if (type == LeverType.momentary)
            {
                Val = initialValue;
                RequestSerialization();
            }
            if (type == LeverType.detent)
            {
                float nearestDetent = detent[0];
                for (int i = 1; i < detent.Length; i++)
                {
                    if (Mathf.Abs(nearestDetent - val) > Mathf.Abs(detent[i] - val))
                        nearestDetent = detent[i];
                }
                Val = nearestDetent;
            }
            if (triggerObject != null)
            {
                triggerObject.Val = 0f;
                triggerObject.RequestSerialization();
            }
            base.whenRelease();
            RequestSerialization();
        }
        public override void whenPressUp()
        {
            switch (type)
            {
                case LeverType.normal:
                    Val = Mathf.Clamp(val + KeyboardMul * Time.deltaTime, min, max);
                    break;
                case LeverType.momentary:
                    Val = KeyboardMul;
                    detentLatch = true;
                    break;
                case LeverType.detent:
                    if (detentLatch) break;
                    detentLatch = true;
                    for (int d = 0; d < detent.Length; d++)
                    {
                        if (val < detent[d])
                        {
                            Val = detent[d];
                            return;
                        }
                    }
                    Val = detent[0];
                    break;
            }
        }
        public override void whenReleaseKey()
        {
            if (type == LeverType.momentary && detentLatch) Val = 0f;
            RequestSerialization();
            detentLatch = false;
        }

        public override void whenPressDown()
        {
            switch (type)
            {
                case LeverType.normal:
                    Val = Mathf.Clamp(val - KeyboardMul * Time.deltaTime, min, max);
                    break;
                case LeverType.momentary:
                    Val = -KeyboardMul;
                    detentLatch = true;
                    break;
                case LeverType.detent:
                    Val = Mathf.Clamp(val + KeyboardMul * Time.deltaTime, min, max);
                    break;
            }
        }

    }
}