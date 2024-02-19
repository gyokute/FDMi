﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiFloat : FDMiData
    {
        public float[] data = new float[1];
        public float Data
        {
            get => data[0];
            set => set(value);
        }
        
        public virtual void set(float src)
        { 
            data[0] = src; 
            trigger();
        }
    }
}
