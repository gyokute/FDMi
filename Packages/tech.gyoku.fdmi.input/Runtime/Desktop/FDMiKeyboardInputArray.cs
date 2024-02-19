
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiKeyboardInputArray : FDMiBehaviour
    {
        [SerializeField] FDMiFloat[] floatVal;
        [SerializeField] int[] key;
        [SerializeField] FDMiKeyboardInputType[] type;
        [SerializeField] float[] initial, multiplier, min, max;

        bool prevAnyKey;
        void Update()
        {
            if (!Input.anyKey && !prevAnyKey) return;
            prevAnyKey = Input.anyKey;
            for (int i = 0; i < floatVal.Length; i++)
            {
                switch (type[i])
                {
                    case FDMiKeyboardInputType.momentum:
                        if (Input.GetKeyDown((KeyCode)key[i])) floatVal[i].set(multiplier[i]);
                        if (Input.GetKeyUp((KeyCode)key[i])) floatVal[i].set(initial[i]);
                        break;
                    case FDMiKeyboardInputType.alternate:
                        if (Input.GetKeyDown((KeyCode)key[i]))
                            floatVal[i].set(floatVal[i].Data == initial[i] ? multiplier[i] : initial[i]);
                        break;
                    case FDMiKeyboardInputType.addition:
                        if (Input.GetKey((KeyCode)key[i])) floatVal[i].set(Mathf.Clamp(floatVal[i].Data + multiplier[i] * Time.deltaTime, min[i], max[i]));
                        break;
                    case FDMiKeyboardInputType.stepAddition:
                        if (Input.GetKeyDown((KeyCode)key[i])) floatVal[i].set(Mathf.Clamp(floatVal[i].Data + multiplier[i], min[i], max[i]));
                        break;
                    case FDMiKeyboardInputType.smoothMomental:
                        if (Input.GetKey((KeyCode)key[i])) floatVal[i].set(Mathf.Clamp(floatVal[i].Data + multiplier[i] * Time.deltaTime, min[i], max[i]));
                        if (Input.GetKeyUp((KeyCode)key[i])) floatVal[i].set(initial[i]);
                        break;
                }
            }
        }
    }
}