
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMIMIDIInputConfigulation : FDMiBehaviour
    {
        public FDMiFloat value;
        public int MIDIChannel = 1;
        public int MIDIControlNumber;
        public AnimationCurve curve;

        public void onChange(int input)
        {
            value.Data = curve.Evaluate(input);
        }
    }
}