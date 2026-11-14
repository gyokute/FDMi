using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    // 1 つの FDMiData を 1 つの同期値として送る型の基底。
    // Why-not: 基底 FDMiDataSync に data を置かない。グループ同期（FDMiDataSyncGroup）は 1 値ではない。
    public abstract class FDMiDataSyncValue : FDMiDataSync
    {
        [Tooltip("データの解決パス。名前空間の区切りは /、~ は機体の起点")]
        public string dataPath;

        [FDMiDataPath(nameof(dataPath)), FDMiRegisterCallback(nameof(OnDataChanged))]
        [Tooltip("同期する FDMiData。dataPath から解決する")]
        public FDMiData data;
        protected bool whenDeserializing = false;

        public abstract void OnDataChanged();
    }
}
