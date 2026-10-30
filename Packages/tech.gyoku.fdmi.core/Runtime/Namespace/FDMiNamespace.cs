using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FDMi.core
{
    public class FDMiNamespace : MonoBehaviour
    {
        [Tooltip("名前空間の名前。FDMiDataPath のパスセグメントになる")]
        public string nameSpace;

        [Tooltip("名前空間の起点。~/ はここを指す")]
        public bool isNamespaceRoot = false;

        // Editor の名前空間グラフ。Runtime は読まない。
        [HideInInspector]
        public FDMiNamespace _parentNamespace;

        [HideInInspector]
        public HashSet<FDMiNamespace> _childNamespaces = new HashSet<FDMiNamespace>();
    }
}
