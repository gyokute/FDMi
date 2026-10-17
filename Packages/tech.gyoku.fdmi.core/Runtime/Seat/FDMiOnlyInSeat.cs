using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiOnlyInSeat : FDMiBehaviour
    {
        void Start()
        {
            gameObject.SetActive(false);
        }

        public void FDMiOnSeatEnter()
        {
            gameObject.SetActive(true);
        }

        public void FDMiOnSeatExit()
        {
            gameObject.SetActive(false);
        }
    }
}
