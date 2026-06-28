using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiComponentRef : FDMiData
    {
        public Component[] data = new Component[1];
        public Component Data
        {
            get => data[0];
            set => Set(value);
        }

        public virtual void Set(Component src)
        {
            data[0] = src;
            TriggerCallbacks();
        }
    }
}
