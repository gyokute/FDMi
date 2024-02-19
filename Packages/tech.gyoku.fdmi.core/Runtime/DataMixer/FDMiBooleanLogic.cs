
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public enum BooleanLogic { AND, OR }
    public class FDMiBooleanLogic : FDMiBehaviour
    {
        [SerializeField] FDMiBool output;
        [SerializeField] FDMiBool[] data;
        [SerializeField] BooleanLogic logic;
        [SerializeField] private bool useUpdate = false, useOnChange = true;
        void Start()
        {
            if (useOnChange) foreach (FDMiBool d in data) d.subscribe(this, "OnChange");
            gameObject.SetActive(useUpdate);
        }
        void Update()
        {
            OnChange();
        }

        bool o;
        public void OnChange()
        {
            if (logic == BooleanLogic.AND)
            {
                o = true;
                foreach (FDMiBool d in data) o = o && d.Data;
            }
            if (logic == BooleanLogic.OR)
            {
                o = false;
                foreach (FDMiBool d in data) o = o || d.Data;
            }
            output.Data = o;
        }
    }
}
