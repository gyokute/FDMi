using System;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FDMiData : UdonSharpBehaviour
    {
        public UdonSharpBehaviour[] callbackBehaviour;
        public string[] callbackFunction;

        /// <summary>
        /// 登録されたコールバックを呼び出す。
        /// </summary>
        public void TriggerCallbacks()
        {
            for (int i = 0; i < callbackBehaviour.Length; i++)
                if (callbackBehaviour[i])
                    callbackBehaviour[i].SendCustomEvent(callbackFunction[i]);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        /// <summary>
        /// コールバックを登録する。
        /// コールバックは、UdonSharpBehaviourと呼び出す関数の名前の組み合わせで登録される。
        /// </summary>
        /// <param name="behaviour">コールバックを呼び出すUdonSharpBehaviour</param>
        /// <param name="functionName">呼び出す関数の名前</param>
        public void Subscribe(UdonSharpBehaviour behaviour, string functionName)
        {
            // callbackBehaviourに配列要素を追加し、behaviourを格納する。
            // callbackFunctionに配列要素を追加し、functionNameを格納する。
            callbackBehaviour = callbackBehaviour.Append(behaviour).ToArray();
            callbackFunction = callbackFunction.Append(functionName).ToArray();
        }

        /// <summary>
        /// コールバックの登録を解除する。
        /// behaviourとfunctionNameの組み合わせが一致するコールバックをcallbackBehaviourとcallbackFunctionから削除する。
        /// 削除後、callbackBehaviourとcallbackFunctionの配列サイズを1減らす。
        /// </summary>
        /// <param name="behaviour">コールバックを呼び出すUdonSharpBehaviour</param>
        /// <param name="functionName">呼び出す関数の名前</param>
        public void Unsubscribe(UdonSharpBehaviour behaviour, string functionName)
        {
            var list = callbackBehaviour
                .Select((b, i) => new { Behaviour = b, Function = callbackFunction[i] })
                .Where(x => !(x.Behaviour == behaviour && x.Function == functionName));
            callbackBehaviour = list.Select(x => x.Behaviour).ToArray();
            callbackFunction = list.Select(x => x.Function).ToArray();
        }
#endif
    }
}
