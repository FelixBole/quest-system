using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Slax.QuestSystem
{
    public class QuestManagerEditorWindow : EditorWindow
    {
        private enum BackgroundType { AssetCreator, AssetFinder, QuestInfo, QuestLineDetails }

        private Vector2 _scroll;
        private int _selected;
        private Texture2D _searchIcon;
        private Texture2D _refreshIcon;

        List<string> _SOTypes = new List<string>();

        string[] _questLineGUIDs;
        string[] _questGUIDs;
        string[] _questStepGUIDs;

        QuestLineSO[] _questLineObjects;
        QuestSO[] _questObjects;
        QuestStepSO[] _questStepObjects;

        string[] _displayObjectsGUIDs;
        List<string> _displayObjectsPaths;
        List<ScriptableObject> _displayObjects;

        [SerializeField] private QuestManagerEditorWindowSO _editorData;
        private Dictionary<BackgroundType, Texture2D> _backgrounds;

        int _activeQuestLineInfo;

        private void OnEnable()
        {
            FindAllSOs();
            FindDisplaySOs();
            InitializeBackgrounds();
            LoadIcons();
        }

        void OnFocus()
        {
            FindAllSOs();
            FindDisplaySOs();
        }

        [MenuItem("Window/Slax/Quest Manager")]
        private static void ShowWindow()
        {
            GetWindow<QuestManagerEditorWindow>(false, "Quest Manager", true);
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, false, false);

            SerializedObject editorData = new SerializedObject(_editorData);
            GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Box("The Quest Manager helps create and manage all asset scriptable objects related to Quests in the project.");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(50));
            GUIContent refreshContent = new GUIContent(_refreshIcon);
            if (GUILayout.Button(refreshContent, GetSquareButtonStyle(30))) FindAllSOs();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical("box", GUILayout.Width(130), GUILayout.ExpandHeight(true));

            string[] tabs = { "Asset Creator", "Asset Finder", "Quest Info" };

            for (int i = 0; i < tabs.Length; i++)
            {
                bool tabSelected = GUILayout.Button(tabs[i]);
                if (tabSelected) editorData.FindProperty("LastSelectedTab").intValue = i;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (_editorData.LastSelectedTab == 0)
            {
                EditorGUILayout.LabelField("Quest asset creator", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("When creating Quests it is recommended to follow the following pattern in order to keep things easy to maintain : QuestLineX_QuestX_StepX (shorter : QLX_QX_SX) where X is a number.", MessageType.Info);
                DrawAssetCreator("QuestLine", editorData);
                DrawAssetCreator("Quest", editorData);
                DrawAssetCreator("QuestStep", editorData);
            }
            else if (_editorData.LastSelectedTab == 1)
            {
                EditorGUILayout.LabelField("Quest asset finder", EditorStyles.boldLabel);
                EditorGUILayout.Space(15);

                GUILayout.BeginHorizontal();

                DrawSOsPicker();
                if (GUILayout.Button(refreshContent, GetSquareButtonStyle(30)))
                {
                    FindAllSOs();
                    FindDisplaySOs();
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("__________________________________________________");
                EditorGUILayout.Space(15);

                DrawSOsList();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                DrawQuestConfigs();
                if (_activeQuestLineInfo <= _questLineObjects.Length)
                {
                    EditorGUILayout.BeginVertical();
                    DrawSelectedQuestLineInfo();
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(15);
            EditorGUILayout.EndScrollView();

            editorData.ApplyModifiedProperties();
            EditorUtility.SetDirty(_editorData);
        }

        void DrawAssetCreator(string assetType, SerializedObject editorData)
        {
            EditorGUILayout.Space(15);

            switch (assetType)
            {
                case "QuestLine":
                    EditorGUILayout.PropertyField(editorData.FindProperty("QuestLineAssetPath"));
                    break;
                case "Quest":
                    EditorGUILayout.PropertyField(editorData.FindProperty("QuestAssetPath"));
                    break;
                case "QuestStep":
                    EditorGUILayout.PropertyField(editorData.FindProperty("QuestStepAssetPath"));
                    break;
                default:
                    break;
            }

            ScriptableObject createdAsset = null;

            switch (assetType)
            {
                case "QuestLine":
                    if (GUILayout.Button(new GUIContent("Create Quest Line", "Creates a new Quest Line asset Scriptable Object"), GUILayout.Height(20)))
                    {
                        createdAsset = QuestAssetCreator.CreateQuestLine(_editorData.QuestLineAssetPath);
                    }
                    break;
                case "Quest":
                    if (GUILayout.Button(new GUIContent("Create Quest", "Creates a new Quest asset Scriptable Object"), GUILayout.Height(20)))
                    {
                        createdAsset = QuestAssetCreator.CreateQuest(_editorData.QuestAssetPath);
                    }
                    break;
                case "QuestStep":
                    if (GUILayout.Button(new GUIContent("Create Quest Step", "Creates a new Quest Step asset Scriptable Object"), GUILayout.Height(20)))
                    {
                        createdAsset = QuestAssetCreator.CreateQuestStep(_editorData.QuestStepAssetPath);
                    }
                    break;
            }

            if (createdAsset)
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(createdAsset);
                FindAllSOs();
            }
        }

        void DrawSOsPicker()
        {
            EditorGUI.BeginChangeCheck();
            _selected = EditorGUILayout.Popup(GUIContent.none, _selected, _SOTypes.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                FindDisplaySOs();
            }
        }

        void DrawQuestConfigs()
        {
            EditorGUILayout.BeginVertical(GetBoxStyle(BackgroundType.QuestInfo), GUILayout.ExpandHeight(true), GUILayout.Width(250));
            if (_questLineGUIDs.Length == 0)
            {
                EditorGUILayout.LabelField("No Quest Lines created");
                return;
            }

            for (int i = 0; i < _questLineGUIDs.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(_questLineGUIDs[i]);
                QuestLineSO questLine = (QuestLineSO)AssetDatabase.LoadAssetAtPath(assetPath, typeof(QuestLineSO));
                DrawQuestLineInfo(questLine, i);
            }
            EditorGUILayout.EndVertical();
        }

        void DrawQuestLineInfo(QuestLineSO questLine, int index)
        {
            if (!questLine) return;
            EditorGUILayout.BeginVertical(new GUIStyle(GetBoxStyle(BackgroundType.QuestLineDetails)));
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            int previewSize = 60;
            GUILayout.Label(questLine.Sprite ? questLine.Sprite : new Texture2D(previewSize, previewSize), GUILayout.Width(previewSize), GUILayout.Height(previewSize));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{questLine.DisplayName}", EditorStyles.boldLabel);
            GUILayout.Label($"Asset: {questLine.name}");
            if (GUILayout.Button(_searchIcon, GetSquareButtonStyle(22)))
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(questLine);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Quests: {questLine.Quests.Count}");
            EditorGUILayout.LabelField($"Total Steps: {questLine.GetTotalSteps()}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Info"))
            {
                _activeQuestLineInfo = index;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void DrawSelectedQuestLineInfo()
        {
            QuestLineSO questLine = _questLineObjects[_activeQuestLineInfo];
            if (!questLine) return;
            if (questLine.Quests.Count > 0)
            {
                questLine.Quests.ForEach((quest) =>
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"{quest.DisplayName}", EditorStyles.boldLabel);

                    quest.Steps.ForEach((step) =>
                    {
                        GUI.color = step.Completed ? Color.green : Color.white;
                        EditorGUILayout.LabelField($"- {step.DisplayName}", EditorStyles.boldLabel);
                    });
                    EditorGUILayout.EndVertical();
                    GUI.color = Color.white;
                });
            }
        }

        void DrawSOsList()
        {
            _scroll = GUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _displayObjectsGUIDs.Length; i++)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Width(250));
                GUILayout.Label(i + 1 + ". " + _displayObjects[i].name);

                if (GUILayout.Button(_searchIcon, GetSquareButtonStyle(27)))
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(_displayObjects[i]);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            GUILayout.EndScrollView();
        }

        string[] FindQuestLinesGUIDs() => AssetDatabase.FindAssets("t:Slax.QuestSystem.QuestLineSO") as string[];
        string[] FindQuestGUIDs() => AssetDatabase.FindAssets("t:Slax.QuestSystem.QuestSO") as string[];
        string[] FindQuestStepGUIDs() => AssetDatabase.FindAssets("t:Slax.QuestSystem.QuestStepSO") as string[];

        void FindAllSOs()
        {
            _questLineGUIDs = FindQuestLinesGUIDs();
            _questGUIDs = FindQuestGUIDs();
            _questStepGUIDs = FindQuestStepGUIDs();

            _questLineObjects = new QuestLineSO[_questLineGUIDs.Length];
            _questObjects = new QuestSO[_questGUIDs.Length];
            _questStepObjects = new QuestStepSO[_questStepGUIDs.Length];

            for (int i = 0; i < _questLineGUIDs.Length; i++)
            {
                _questLineObjects[i] = (QuestLineSO)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(_questLineGUIDs[i]), typeof(QuestLineSO));
                _SOTypes.Add(_questLineObjects[i].GetType().ToString());
            }

            for (int i = 0; i < _questGUIDs.Length; i++)
            {
                _questObjects[i] = (QuestSO)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(_questGUIDs[i]), typeof(QuestSO));
                _SOTypes.Add(_questObjects[i].GetType().ToString());
            }

            for (int i = 0; i < _questStepGUIDs.Length; i++)
            {
                _questStepObjects[i] = (QuestStepSO)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(_questStepGUIDs[i]), typeof(QuestStepSO));
                _SOTypes.Add(_questStepObjects[i].GetType().ToString());
            }
        }

        void FindDisplaySOs()
        {
            if (_displayObjects != null)
            {
                _displayObjects.Clear();
            }
            if (_displayObjectsPaths != null)
            {
                _displayObjectsPaths.Clear();
            }

            string type = _SOTypes[_selected];
            string queryString = "t:" + type;

            _displayObjectsGUIDs = AssetDatabase.FindAssets(queryString);

            _displayObjectsPaths = new List<string>(_displayObjectsGUIDs.Length);
            _displayObjects = new List<ScriptableObject>(_displayObjectsGUIDs.Length);

            for (int i = 0; i < _displayObjectsGUIDs.Length; i++)
            {
                _displayObjectsPaths.Add(AssetDatabase.GUIDToAssetPath(_displayObjectsGUIDs[i]));
                _displayObjects.Add(AssetDatabase.LoadAssetAtPath(_displayObjectsPaths[i], typeof(ScriptableObject)) as ScriptableObject);
            }
        }

        GUIStyle GetBoxStyle(BackgroundType backgroundType)
        {
            GUIStyle style = new GUIStyle() { padding = new RectOffset(10, 10, 10, 10) };
            style.normal.background = _backgrounds[backgroundType];
            return style;
        }

        void InitializeBackgrounds()
        {
            _backgrounds = new Dictionary<BackgroundType, Texture2D>();

            Texture2D assetCreator = new Texture2D(1, 1);
            assetCreator.SetPixel(1, 1, new Color(0.13f, 0.2f, 0.23f));
            assetCreator.Apply();
            _backgrounds.Add(BackgroundType.AssetCreator, assetCreator);

            Texture2D questInfo = new Texture2D(1, 1);
            questInfo.SetPixel(1, 1, new Color(0.13f, 0.2f, 0.23f));
            questInfo.Apply();
            _backgrounds.Add(BackgroundType.QuestInfo, questInfo);

            Texture2D questLineDetails = new Texture2D(1, 1);
            // questLineDetails.SetPixel(1, 1, new Color(0.37f, 0.31f, 0.25f));
            questLineDetails.SetPixel(1, 1, Color.clear);
            questLineDetails.Apply();
            _backgrounds.Add(BackgroundType.QuestLineDetails, questLineDetails);
        }

        void LoadIcons()
        {
            if (_searchIcon == null)
            {
                string path = "Scripts/Quest System/Editor/Textures/search.png";
                _searchIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + path, typeof(Texture2D));
                if (_searchIcon == null)
                {
                    _searchIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/" + path, typeof(Texture2D));
                }
            }

            if (_refreshIcon == null)
            {
                string path = "Scripts/Quest System/Editor/Textures/refresh.png";
                _refreshIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + path, typeof(Texture2D));
                if (_refreshIcon == null)
                {
                    _refreshIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/" + path, typeof(Texture2D));
                }
            }
        }

        GUIStyle GetSquareButtonStyle(int size)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(5, 5, 5, 5);
            buttonStyle.fixedHeight = size;
            buttonStyle.fixedWidth = size;
            return buttonStyle;
        }
    }
}
