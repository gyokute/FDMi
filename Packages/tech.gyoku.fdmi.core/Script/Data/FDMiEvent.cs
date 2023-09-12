
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiEvent : UdonSharpBehaviour
    {
        public string name;
        public UdonSharpBehaviour[] callbackBehaviour = new UdonSharpBehaviour[128];
        public string[] callbackFunctionName = new string[128];

        public bool subscribe(UdonSharpBehaviour behaviour, string functionName)
        {
            for (int i = 0; i < callbackBehaviour.Length; i++)
            {
                if (!callbackBehaviour[i])
                {
                    callbackBehaviour[i] = behaviour;
                    callbackFunctionName[i] = functionName;
                    return true;
                }
            }
            return false;
        }

        public void trigger()
        {
            for (int i = 0; i < callbackBehaviour.Length; i++)
                if (callbackBehaviour[i]) callbackBehaviour[i].SendCustomEvent(callbackFunctionName[i]);
        }
    }
}