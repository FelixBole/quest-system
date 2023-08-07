using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Slax.QuestSystem
{
    /// <summary>
    /// Scriptable object holding data for the Quest Manager editor window
    /// </summary>
    public class QuestManagerEditorWindowSO : ScriptableObject
    {
        public string QuestLineAssetPath = "Assets";
        public string QuestAssetPath = "Assets";
        public string QuestStepAssetPath = "Assets";
        public int LastSelectedTab = 0;
        public ScriptableObject LastCreatedAsset;
    }
}
