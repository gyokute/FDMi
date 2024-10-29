
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [DefaultExecutionOrder(-100)]
    public class FDMiObjectManager : FDMiBehaviour
    {
        public Rigidbody body;
        public FDMiAttribute[] attributes;
        private UdonSharpBehaviour[] ownerManagingObject = new UdonSharpBehaviour[32];
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
        public void takeOwnerOfAllAttributes()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, body.gameObject);
            foreach (FDMiAttribute att in attributes)
                if (att) att.takeOwner();
            foreach (UdonSharpBehaviour behaviour in ownerManagingObject)
            {
                if (!behaviour) break;
                behaviour.SendCustomEvent("SetLocalPlayerAsOwner");
            }
        }
        public void SubscribeOwnerManagement(UdonSharpBehaviour tgt)
        {
            for (int i = 0; i < ownerManagingObject.Length; i++)
            {
                if (!ownerManagingObject[i])
                {
                    ownerManagingObject[i] = tgt;
                    break;
                }
            }
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