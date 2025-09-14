
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.core.editor
{
    public class FDMiDataTypeUtils
    {
        public static Type getFDMiDataType(FDMiDataType T)
        {
            switch (T)
            {
                case FDMiDataType.FDMiBool: return typeof(FDMiBool);
                case FDMiDataType.FDMiSByte: return typeof(FDMiSByte);
                case FDMiDataType.FDMiInt: return typeof(FDMiInt);
                case FDMiDataType.FDMiFloat: return typeof(FDMiFloat);
                case FDMiDataType.FDMiVector3: return typeof(FDMiVector3);
                case FDMiDataType.FDMiQuaternion: return typeof(FDMiQuaternion);
                case FDMiDataType.FDMiSyncedBool: return typeof(FDMiSyncedBool);
                case FDMiDataType.FDMiSyncedSByte: return typeof(FDMiSyncedSByte);
                case FDMiDataType.FDMiSyncedInt: return typeof(FDMiSyncedInt);
                case FDMiDataType.FDMiSyncedFloat: return typeof(FDMiSyncedFloat);
                case FDMiDataType.FDMiSyncedVector3: return typeof(FDMiSyncedVector3);
                case FDMiDataType.FDMiSyncedQuaternion: return typeof(FDMiSyncedQuaternion);
            }
            return typeof(FDMiData);
        }
    }
}