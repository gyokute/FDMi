using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.7。領域進入で root 切替 (任意クラス)。同期フィールドを持たない。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiAnchorTrigger : UdonSharpBehaviour
    {
        [Tooltip("進入時に切り替える root")]
        public FDMiSyncAnchor target;

        [Tooltip("管理する FDMiSyncManager")]
        public FDMiSyncManager manager;

        [Tooltip("退出時に切り替える root。空なら defaultAnchor")]
        public FDMiSyncAnchor exitTarget;

        [Tooltip("進入を検出する")]
        public bool detectEnter = true;

        [Tooltip("退出を検出する")]
        public bool detectExit = true;

        [Tooltip("退出を有効にする。進入直後の振動を抑える")]
        public bool enableOnExit = true;

        // Start 後 1 秒間は偽。参加直後の誤発火を防ぐ (§10.4 の状態表)。
        [HideInInspector]
        public bool enableDetecting;

        public void Start()
        {
            SendCustomEventDelayedSeconds(nameof(_EnableDetecting), 1f);
        }

        // 文字列配送なので public かつ引数なし (§9)。
        public void _EnableDetecting()
        {
            enableDetecting = true;
        }

        public void _Rearm()
        {
            enableOnExit = true;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!detectEnter || !enableDetecting || !player.isLocal) return;
            if (manager.GetRoot() == target) return;

            manager.SwitchRoot(target);
            // 進入後 0.5 秒は退出を無視する。境界上の振動で root が往復するのを防ぐ。
            enableOnExit = false;
            SendCustomEventDelayedSeconds(nameof(_Rearm), 0.5f);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!detectExit || !enableDetecting || !player.isLocal) return;
            if (manager.GetRoot() != target || !enableOnExit) return;

            manager.SwitchRoot(exitTarget != null ? exitTarget : manager.defaultAnchor);
            SendCustomEventDelayedSeconds(nameof(_Rearm), 0.5f);
        }
    }
}
