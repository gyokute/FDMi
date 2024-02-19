﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiBool : FDMiData
    {
        public bool[] data = new bool[1];
        public bool Data
        {
            get => data[0];
            set => set(value);
        }
        
        public virtual void set(bool src)
        { 
            data[0] = src; 
            trigger();
        }

    }
}
