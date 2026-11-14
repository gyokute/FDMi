using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    // 同期の最小単位は「1 GameObject = 1 所有権 = 1 送信間隔」。同期する値をどこに持つかは派生クラスが決める。
    // Why-not: 基底に data / dataPath を置かない。グループ同期は 1 値ではないため、値を持たない基底に
    // 値のフィールドを置くと、グループ側が使わないフィールドを Inspector に出すことになる。
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class FDMiDataSync : FDMiBehaviour
    {
        [SerializeField]
        [Tooltip("送信間隔 [s]。混雑時はこの間隔で再試行する")]
        protected float updateInterval = 0.25f;
        protected double nextUpdateTime;
        protected bool trySerializeLatch = false;

        // public: SendCustomEventDelayedSeconds は Udon の exported event しか呼べない。
        // protected のままだと間隔内の再試行が無言で捨てられる。
        public void TrySerialize()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            // Try Serialize.
            if (Time.time > nextUpdateTime && !Networking.IsClogged)
            {
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
                trySerializeLatch = false;
            }
            else if (!trySerializeLatch)
            {
                SendCustomEventDelayedSeconds("TrySerialize", updateInterval);
                trySerializeLatch = true;
            }
        }
    }
}
