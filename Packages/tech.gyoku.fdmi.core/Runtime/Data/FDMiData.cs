using System;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FDMiData : UdonSharpBehaviour
    {
        public UdonSharpBehaviour[] callbackBehaviour;
        public string[] callbackFunction;

        /// <summary>
        /// 登録されたコールバックを呼び出す。
        /// </summary>
        public void TriggerCallbacks()
        {
            for (int i = 0; i < callbackBehaviour.Length; i++)
                if (callbackBehaviour[i])
                    callbackBehaviour[i].SendCustomEvent(callbackFunction[i]);
        }
    }
}
