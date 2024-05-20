
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiKeyboardVector3Input : FDMiBehaviour
    {
        public FDMiVector3 Val;
        [SerializeField] KeyCode key;
        [SerializeField] Vector3 multiplier;
        void Update()
        {
            if (Input.GetKeyDown(key)) Val.set(Val.data[0] + multiplier);
        }
    }
}