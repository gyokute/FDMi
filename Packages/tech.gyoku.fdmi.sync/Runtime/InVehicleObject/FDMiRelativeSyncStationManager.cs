
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSyncStationManager : FDMiStationManager
    {
        public FDMiRelativeObjectSyncManager syncManager;
        public FDMiReferencePoint refPoint;

    }
}