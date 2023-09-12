﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [DefaultExecutionOrder(-1000)]
    public class FDMiObjectManager : UdonSharpBehaviour
    {
        public Rigidbody body;
        public FDMiAttribute[] attributes;
        public bool isOwner;

        void Start()
        {
            SendEventToAttribute("init");
        }

        [RecursiveMethod]
        public void SendEventToAttribute(string eventName)
        {
            foreach (FDMiAttribute att in attributes)
                if (att) att.SendCustomEvent(eventName);
        }
        #region ownership management
        private void takeOwnerOfAllAttributes()
        {
            foreach (FDMiAttribute att in attributes)
                if (att) att.takeOwner();
        }
        #endregion

        #region global events
        private void OnEnable()
        {
            SendEventToAttribute("FDMi_L_OnEnable");
        }
        private void OnDisable()
        {
            SendEventToAttribute("FDMi_L_OnDisable");
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                isOwner = true;
                takeOwnerOfAllAttributes();
                SendEventToAttribute("FDMi_O_TakeOwnership");
            }
            if (!player.isLocal && isOwner)
            {
                isOwner = false;
                SendEventToAttribute("FDMi_O_LoseOwnership");
            }
            SendEventToAttribute("FDMi_L_OwnershipTransfer");
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (isOwner)
            { SendEventToAttribute("FDMi_O_OnPlayerJoined"); }
        }
        #endregion
    }
}