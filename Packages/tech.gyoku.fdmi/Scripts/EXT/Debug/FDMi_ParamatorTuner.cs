
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace SaccFlight_FDMi
{
    public class FDMi_ParamatorTuner : FDMi_Indicator
    {
        public float val;
        public FDMi_knob knob;
        [SerializeField] UdonSharpBehaviour tgt;
        [SerializeField] string targetParam;
        [SerializeField] Text _text;
        public bool isLog = true;
        void Start()
        {
            if (isLog) val = Mathf.Pow(10f, knob.val);
            else val = knob.val;
            tgt.SetProgramVariable(targetParam, val);
            _text.text = val.ToString();
        }
        public override void whenChange()
        {
            if (isLog) val = Mathf.Pow(10f, knob.val);
            else val = knob.val;
            tgt.SetProgramVariable(targetParam, val);
            _text.text = val.ToString();
        }
    }
}