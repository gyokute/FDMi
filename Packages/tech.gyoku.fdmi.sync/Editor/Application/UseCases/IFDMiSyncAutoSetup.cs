using UnityEngine;

namespace FDMi.sync.Editor
{
    public interface IFDMiSyncAutoSetup
    {
        void Execute(Object target);
        void ExecuteAll();
    }
}
