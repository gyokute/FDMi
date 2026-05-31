using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tech.gyoku.FDMi.core
{
    public class FDMiNamespace : MonoBehaviour
    {
        public string nameSpace;
        public bool isNamespaceRoot = false;
        [HideInInspector]public FDMiNamespace _parentNamespace;
        [HideInInspector]public HashSet<FDMiNamespace> _childNamespaces = new HashSet<FDMiNamespace>();

    }
}
