#if UNITY_EDITOR
using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        // Validation belongs to explicit development actions, never a player's startup.
        [ContextMenu("Development/Run Prototype Self Checks")]
        private void RunPrototypeSelfChecks()
        {
            if (!openingGameReady)
            { Debug.LogWarning("Wait for game preparation before running prototype checks."); return; }
            RunT02SelfTest(); RunT03SelfTest(); RunT04SelfTest(); RunT05SelfTest();
            RunT06SelfTest(); RunT07SelfTest(); RunT08SelfTest(modelCatalog);
            RunT09SelfTest(); RunT12SelfTest();
        }
    }
}
#endif
