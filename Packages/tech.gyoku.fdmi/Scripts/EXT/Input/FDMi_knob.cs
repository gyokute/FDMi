
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace SaccFlight_FDMi
{
    public class FDMi_knob : FDMi_InputObject
    {
        [SerializeField] private LeverAxis rotateAxis;
        [SerializeField] private float inputMul = 1f, triggerInputMul = 10f, inputDetent = 5f, triggerInputDetent = 5f, min = 0f, max = 1f;
        public float a = 0.5f;
        private float rawInput, input, prevParam;
        public bool isRotaly;
        bool prevTrig = false;
        private bool latch = false;
        Quaternion prevAxis;
        public override void Start()
        {
            base.Start();
        }
        public override void InputUpdate()
        {
            base.InputUpdate();
            if ((Trig[handType] > 0.7f) != prevTrig)
            {
                prevAxis = handAxis;
                prevParam = val;
            }
            Vector3 eular = (Quaternion.Inverse(prevAxis) * handAxis).eulerAngles;
            input = eular[(int)rotateAxis] - Mathf.Floor(eular[(int)rotateAxis] / 180.1f) * 360;
            if (handType == 1) input *= -1;

            if (Trig[handType] < 0.7f)
                // rawInput = Mathf.Round(input * inputMul / inputDetent) * inputDetent;
                rawInput = Mathf.Sign(input) * Mathf.Floor(Mathf.Abs(input) / inputDetent) * inputMul;
            if (Trig[handType] > 0.7f)
                rawInput = Mathf.Sign(input) * Mathf.Floor(Mathf.Abs(input) / triggerInputDetent) * triggerInputMul;
            // rawInput = Mathf.Round(input * triggerInputMul / triggerInputDetent) * triggerInputDetent;

            rawInput += prevParam;
            if (isRotaly)
            {
                if (rawInput > max) rawInput += min - max;
                if (rawInput < min) rawInput += max - min;
            }
            Val = Mathf.Clamp(rawInput, min, max);
            // prevParam = rawInput;
            prevTrig = Trig[handType] > 0.7f;
            RequestSerialization();
        }

        public override void whenGrab()
        {
            prevParam = val;
            rawInput = 0f;
            prevAxis = handStartAxis;
        }
        public override void whenRelease()
        {
            base.whenRelease();
            RequestSerialization();
        }
        public override void whenPressUp()
        {
            if (!latch) val += inputMul;
            latch = true;
        }
        public override void whenPressDown()
        {
            if (!latch) val -= inputMul;
            latch = true;
        }
        public override void whenReleaseKey()
        {
            latch = false;
            foreach (FDMi_Avionics ind in indicators) ind.whenChange();
            RequestSerialization();
        }

    }
}