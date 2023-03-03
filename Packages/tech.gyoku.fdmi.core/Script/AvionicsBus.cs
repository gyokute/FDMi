
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class AvionicsBus : UdonSharpBehaviour
    {
        public AvionicsTerminal[] terminal;
        public float[][] register;

        void Start()
        {
            register = new float[terminal.Length][];
            for (int i = 0; i < terminal.Length; i++)
            {
                register[i] = terminal[i].register;
                terminal[i].busId = i;
            }
        }

    }
}