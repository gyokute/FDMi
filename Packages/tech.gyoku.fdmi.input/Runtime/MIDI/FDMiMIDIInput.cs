
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiMIDIInput : FDMiBehaviour
    {
        public FDMIMIDIInputConfigulation[] configs;
        public int[] midiConfigIndex = new int[2048];
        public override void MidiControlChange(int channel, int number, int value)
        {
            int configIndex = midiConfigIndex[channel * 128 + number];
            configs[configIndex].onChange(value);
        }
    }
}
