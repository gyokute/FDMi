
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectManager : UdonSharpBehaviour
    {
        public Rigidbody body;
        public FDMiAttribute[] attributes;
        public FDMiDataBus[] data;
        public VRCStation[] stations;

        void Start()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].init();
        }

        public void EVT_O_Enter()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_O_Enter();
        }
        public void EVT_O_Exit()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_O_Exit();
        }
        public void EVT_G_PilotChanged()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_G_PilotChanged();
        }
        public void EVT_G_PassengerEnter()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_G_PassengerEnter();
        }
        public void EVT_G_PassengerExit()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_G_PassengerExit();
        }
        public void EVT_O_TakeOwnership()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_O_TakeOwnership();
        }
        public void EVT_O_LoseOwnership()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_O_LoseOwnership();
        }
        public void EVT_G_Explode()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_G_Explode();
        }
        public void ResetStatus()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].ResetStatus();
        }
        public void EVT_O_OnPlayerJoined()
        {
            for (int i = 0; i < attributes.Length; i++)
                attributes[i].EVT_O_OnPlayerJoined();
        }

    }
}