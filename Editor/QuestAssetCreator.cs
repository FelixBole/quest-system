using UnityEditor;
using UnityEngine;

namespace Slax.QuestSystem
{
    public static class QuestAssetCreator
    {
        public static QuestLineSO CreateQuestLine(string path)
        {
            string name = "QL0";
            CreateAsset<QuestLineSO>(path, name);
            return GetCreatedAsset<QuestLineSO>($"{name} t:Slax.QuestSystem.QuestLineSO");
        }

        public static QuestSO CreateQuest(string path)
        {
            string name = "QL0_Q0";
            CreateAsset<QuestSO>(path, name);
            return GetCreatedAsset<QuestSO>($"{name} t:Slax.QuestSystem.QuestSO");
        }

        public static QuestStepSO CreateQuestStep(string path)
        {
            string name = "QL0_Q0_S0";
            CreateAsset<QuestStepSO>(path, name);
            return GetCreatedAsset<QuestStepSO>($"{name} t:Slax.QuestSystem.QuestStepSO");
        }

        private static void CreateAsset<T>(string path, string name) where T : ScriptableObject
        {
            if (path == "") path = "Assets";
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), $"{path}/{name}.asset");
            Debug.Log($"Created asset at path {path}/{name}");
        }

        private static T GetCreatedAsset<T>(string filter) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{filter}") as string[];
            string guid = guids[0];
            string objPath = AssetDatabase.GUIDToAssetPath(guid);
            T obj = (T)AssetDatabase.LoadAssetAtPath(objPath, typeof(T));
            return obj;
        }
    }
}
