using UnityEngine;
using UnityEditor;

namespace Slax.QuestSystem
{
    [CustomEditor(typeof(QuestPoint))]
    public class QuestPointEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Box("This component should be attached to any item / npc / interactible that can complete a quest step (or start a quest). \nPlug in any logic to the UnityEvents to handle step completion automatically. It is worth noting that the QuestManager singleton instance also fires quest step completion with more information.", EditorStyles.helpBox);
            DrawDefaultInspector();
        }
    }
}
