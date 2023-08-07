using UnityEngine;
using UnityEditor;

namespace Slax.QuestSystem
{
    [CustomEditor(typeof(QuestManager))]
    public class QuestManagerEditor : Editor
    {
        bool showSaveBox = true;
        bool showManualOperations = false;

        public override void OnInspectorGUI()
        {
            QuestManager obj = (QuestManager)target;
            SerializedProperty saveType = serializedObject.FindProperty("_saveType");
            SerializedProperty returnType = serializedObject.FindProperty("_returnType");
            SerializedProperty questLineAssetPath = serializedObject.FindProperty("_questLineAssetPath");
            SerializedProperty questAssetPath = serializedObject.FindProperty("_questAssetPath");
            SerializedProperty questStepAssetPath = serializedObject.FindProperty("_questStepAssetPath");

            GUILayout.Box("The Quest Manager handles all quest related operations. \n Quest completion, quest events, saving and loading etc. It is easy to customize as well.", EditorStyles.helpBox);

            GUILayout.Space(30);

            EditorGUI.indentLevel = 1;
            GUILayout.Label("Quests Configuration", EditorStyles.boldLabel);
            DrawDefaultProp("_questLines");
            GUILayout.Space(10);

            EditorGUI.indentLevel = 0;
            GUILayout.Space(30);

            GUILayout.Label("Save Configuration", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            GUI.backgroundColor = Color.cyan;
            showSaveBox = EditorGUILayout.Foldout(showSaveBox, "Save Configuration Details");
            if (showSaveBox)
            {
                GUILayout.Box("This Manager allows you to use either its internal saving system which will save Quests data to a separate save file or have it return save data in a format of choice to plug in your own save data. \n Set Save Type to Custom if you need to plug it into your own save system, or leave it as Internal to let the Quest Manager handle it for you.", EditorStyles.helpBox);
            }
            GUILayout.Space(10);
            DrawDefaultProp("_saveType");
            string selectedSaveType = saveType.enumNames[saveType.enumValueIndex];

            if (selectedSaveType == "Custom")
            {
                DrawDefaultProp("_returnType");
            }
            else
            {
                DrawDefaultProp("_saveFileName");
            }
            EditorGUI.indentLevel = 0;

            GUILayout.Space(30);

            // Manual actions
            GUI.backgroundColor = Color.yellow;
            showManualOperations = EditorGUILayout.Foldout(showManualOperations, "Manual Operations (testing)");
            if (showManualOperations)
            {
                GUILayout.Label("Manual Operations", EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("Manual Reset", "Resets the Quest Manager, marking all quests are incomplete in the current instance of the Quest Manager."), GUILayout.Height(30)))
                {
                    obj.ManualReset();
                }
                if (GUILayout.Button(new GUIContent("Manual Save", "Saves the current quest progress to the save file at the specified path."), GUILayout.Height(30)))
                {
                    obj.ManualSave();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawDefaultProp(string propName)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propName));
        }
    }
}
