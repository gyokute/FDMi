
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum FDMiKeyboardInputType { momentum, alternate, detent, addition, stepAddition }
    public class FDMiKeyboardInput : UdonSharpBehaviour
    {
        [SerializeField] FDMiFloat floatVal;
        [SerializeField] KeyCode key;
        [SerializeField] FDMiKeyboardInputType type;
        [SerializeField] float initial, multiplier, min, max;
        [SerializeField] float[] detents;
        void Update()
        {
            switch (type)
            {
                case FDMiKeyboardInputType.momentum:
                    if (Input.GetKeyDown(key)) floatVal.set(multiplier);
                    if (Input.GetKeyUp(key)) floatVal.set(initial);
                    break;
                case FDMiKeyboardInputType.alternate:
                    if (Input.GetKeyDown(key))
                        floatVal.set(floatVal.Data == initial ? multiplier : initial);
                    break;
                case FDMiKeyboardInputType.detent:
                    if (Input.GetKeyDown(key))
                    {
                        int nearestIndex = 0;
                        float nearestValue = float.MaxValue, current;
                        for (int i = 0; i < detents.Length; i++)
                        {
                            current = Mathf.Abs(floatVal.Data - detents[i]);
                            if (current < nearestValue)
                            {
                                nearestIndex = i;
                                nearestValue = current;
                            }
                        }
                        floatVal.set(detents[(nearestIndex + 1) % detents.Length]);
                    }
                    break;
                case FDMiKeyboardInputType.addition:
                    if (Input.GetKey(key)) floatVal.set(Mathf.Clamp(floatVal.Data + multiplier * Time.deltaTime, min, max));
                    break;
                case FDMiKeyboardInputType.stepAddition:
                    if (Input.GetKeyDown(key)) floatVal.set(Mathf.Clamp(floatVal.Data + multiplier, min, max));
                    if (Input.GetKeyUp(key)) floatVal.set(Mathf.Clamp(floatVal.Data - multiplier, min, max));
                    break;
            }
        }
    }
}