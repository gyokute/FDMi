
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectActivateByValue : FDMiBehaviour
    {
        [SerializeField] FDMiFloat Value;
        [SerializeField] AnimationCurve turnOnCurve;
        [SerializeField] private bool moveOnValueChange, moveOnUpdate;
        void Start()
        {
            if (moveOnValueChange) Value.subscribe(this, "OnChange");
            gameObject.SetActive(moveOnUpdate);
        }
        void Update()
        {
            OnChange();
        }
        public void OnChange()
        {
            gameObject.SetActive(turnOnCurve.Evaluate(Value.Data) > 0.5f);
        }
    }
}