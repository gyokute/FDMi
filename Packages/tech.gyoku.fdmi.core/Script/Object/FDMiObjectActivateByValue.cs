
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiObjectActivateByValue : FDMiBehaviour
    {
        [SerializeField] GameObject obj;
        [SerializeField] FDMiFloat Value;
        [SerializeField] AnimationCurve turnOnCurve;
        [SerializeField] private bool moveOnValueChange, moveOnUpdate;
        void Start()
        {
            if (!obj) obj = gameObject;
            if (moveOnValueChange) Value.subscribe(this, "OnChange");
            gameObject.SetActive(moveOnUpdate);
        }
        void Update()
        {
            OnChange();
        }
        public void OnChange()
        {
            obj.SetActive(turnOnCurve.Evaluate(Value.Data) > 0.5f);
        }
    }
}