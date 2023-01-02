using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
namespace SaccFlight_FDMi
{
    public class FDMi_InputGroup : UdonSharpBehaviour
    {
        public FDMi_InputManager im;
        public FDMi_InputObject[] inputObject;
        private int[] grabIndex = { -1, -1 };
        public Vector3[] colliderPos;
        public float colliderRadius;
        private Vector3[] handPosition = { Vector3.zero, Vector3.zero };

        void Start()
        {
            handPosition = im.handPosition;
        }

        #region EVENT
        public void InputUpdate()
        {
            if (grabIndex[0] > -1) inputObject[grabIndex[0]].InputUpdate();
            if (grabIndex[1] > -1) inputObject[grabIndex[1]].InputUpdate();
        }
        public void whenGrabR() => whenGrab((int)HandType.RIGHT);
        public void whenGrabL() => whenGrab((int)HandType.LEFT);
        public void whenReleaseR() => whenRelease(HandType.RIGHT);
        public void whenReleaseL() => whenRelease(HandType.LEFT);

        public void ResetStatus()
        {
            for (int i = 0; i < inputObject.Length; i++)
            {
                inputObject[i].ResetStatus();
            }
        }
        #endregion
        #region GRAB
        private void whenRelease(HandType grabHand)
        {
            if (grabIndex[(int)grabHand] >= 0) inputObject[grabIndex[(int)grabHand]].whenRelease();
            grabIndex[(int)grabHand] = -1;
        }

        private void whenGrab(int handType)
        {
            for (int i = 0; i < inputObject.Length; i++)
            {
                Vector3 localHandPos = inputObject[i].transform.InverseTransformPoint(handPosition[handType]);
                if (IsRoundedSquareHit(localHandPos, inputObject[i].colliderPos, inputObject[i].colliderRadius))
                {
                    grabIndex[handType] = i;
                    if (handType == (int)HandType.RIGHT)
                    {
                        if (grabIndex[(int)HandType.LEFT] == i) whenRelease(HandType.LEFT);
                        inputObject[i].whenGrabR();
                    }
                    if (handType == (int)HandType.LEFT)
                    {
                        if (grabIndex[(int)HandType.RIGHT] == i) whenRelease(HandType.RIGHT);
                        inputObject[i].whenGrabL();
                    }
                    return;
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

    }
}