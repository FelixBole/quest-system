using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static UnityEditor.EditorGUILayout;

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

        string _questLineAssetPath = "Assets/QuestSystem/QuestLines";
        string _questAssetPath = "Assets/QuestSystem/Quests";
        string _questStepAssetPath = "Assets/QuestSystem/QuestSteps";

        int _lastSelectedTab = 0;
        ScriptableObject _lastCreatedAsset;

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
            _scroll = BeginScrollView(_scroll, false, false);

            GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);
            BeginHorizontal();

            BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Box("The Quest Manager helps create and manage all asset scriptable objects related to Quests in the project.");
            EndVertical();

            BeginVertical(GUILayout.Width(50));
            GUIContent refreshContent = new GUIContent(_refreshIcon);
            if (GUILayout.Button(refreshContent, GetSquareButtonStyle(30))) FindAllSOs();
            EndVertical();

            EndHorizontal();
            Space(15);

            BeginHorizontal();
            BeginVertical("box", GUILayout.Width(130), GUILayout.ExpandHeight(true));

            string[] tabs = { "Asset Creator", "Asset Finder", "Quest Info" };

            for (int i = 0; i < tabs.Length; i++)
            {
                bool tabSelected = GUILayout.Button(tabs[i]);
                if (tabSelected) _lastSelectedTab = i;
            }

            EndVertical();
            BeginVertical("box", GUILayout.ExpandHeight(true));
            if (_lastSelectedTab == 0)
            {
                LabelField("Quest asset creator", EditorStyles.boldLabel);
                HelpBox("When creating Quests it is recommended to follow the following pattern in order to keep things easy to maintain : QuestLineX_QuestX_StepX (shorter : QLX_QX_SX) where X is a number.", MessageType.Info);
                DrawAssetCreator("QuestLine");
                DrawAssetCreator("Quest");
                DrawAssetCreator("QuestStep");
            }
            else if (_lastSelectedTab == 1)
            {
                LabelField("Quest asset finder", EditorStyles.boldLabel);
                Space(15);

                GUILayout.BeginHorizontal();

                DrawSOsPicker();
                if (GUILayout.Button(refreshContent, GetSquareButtonStyle(30)))
                {
                    FindAllSOs();
                    FindDisplaySOs();
                }
                GUILayout.EndHorizontal();
                Space(15);
                LabelField("__________________________________________________");
                Space(15);

                DrawSOsList();
            }
            else
            {
                BeginHorizontal();
                DrawQuestConfigs();
                if (_activeQuestLineInfo <= _questLineObjects.Length)
                {
                    BeginVertical();
                    DrawSelectedQuestLineInfo();
                    EndVertical();
                }
                EndHorizontal();
            }

            EndVertical();
            EndHorizontal();
            Space(15);
            EndScrollView();
        }

        void DrawAssetCreator(string assetType)
        {
            Space(15);

            switch (assetType)
            {
                case "QuestLine":
                    // update questline in a field here
                    _questLineAssetPath = TextField("Quest Line Asset Path", _questLineAssetPath);
                    break;
                case "Quest":
                    _questAssetPath = TextField("Quest Asset Path", _questAssetPath);
                    break;
                case "QuestStep":
                    _questStepAssetPath = TextField("Quest Step Asset Path", _questStepAssetPath);
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
                        createdAsset = QuestAssetCreator.CreateQuestLine(_questLineAssetPath);
                    }
                    break;
                case "Quest":
                    if (GUILayout.Button(new GUIContent("Create Quest", "Creates a new Quest asset Scriptable Object"), GUILayout.Height(20)))
                    {
                        createdAsset = QuestAssetCreator.CreateQuest(_questAssetPath);
                    }
                    break;
                case "QuestStep":
                    if (GUILayout.Button(new GUIContent("Create Quest Step", "Creates a new Quest Step asset Scriptable Object"), GUILayout.Height(20)))
                    {
                        createdAsset = QuestAssetCreator.CreateQuestStep(_questStepAssetPath);
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
            _selected = Popup(GUIContent.none, _selected, _SOTypes.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                FindDisplaySOs();
            }
        }

        void DrawQuestConfigs()
        {
            BeginVertical(GetBoxStyle(BackgroundType.QuestInfo), GUILayout.ExpandHeight(true), GUILayout.Width(250));
            if (_questLineGUIDs.Length == 0)
            {
                LabelField("No Quest Lines created");
                return;
            }

            for (int i = 0; i < _questLineGUIDs.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(_questLineGUIDs[i]);
                QuestLineSO questLine = (QuestLineSO)AssetDatabase.LoadAssetAtPath(assetPath, typeof(QuestLineSO));
                DrawQuestLineInfo(questLine, i);
            }
            EndVertical();
        }

        void DrawQuestLineInfo(QuestLineSO questLine, int index)
        {
            if (!questLine) return;
            BeginVertical(new GUIStyle(GetBoxStyle(BackgroundType.QuestLineDetails)));
            BeginHorizontal(GUILayout.ExpandWidth(true));
            int previewSize = 60;
            GUILayout.Label(questLine.Sprite ? questLine.Sprite : new Texture2D(previewSize, previewSize), GUILayout.Width(previewSize), GUILayout.Height(previewSize));

            BeginVertical();
            LabelField($"{questLine.DisplayName}", EditorStyles.boldLabel);
            GUILayout.Label($"Asset: {questLine.name}");
            if (GUILayout.Button(_searchIcon, GetSquareButtonStyle(22)))
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(questLine);
            }
            EndVertical();
            EndHorizontal();

            BeginHorizontal();
            BeginVertical();
            LabelField($"Quests: {questLine.Quests.Count}");
            LabelField($"Total Steps: {questLine.GetTotalSteps()}");
            EndVertical();

            BeginVertical();
            if (GUILayout.Button("Info"))
            {
                _activeQuestLineInfo = index;
            }
            EndVertical();
            EndHorizontal();
            EndVertical();
        }

        void DrawSelectedQuestLineInfo()
        {
            QuestLineSO questLine = _questLineObjects[_activeQuestLineInfo];
            if (!questLine) return;
            if (questLine.Quests.Count > 0)
            {
                questLine.Quests.ForEach((quest) =>
                {
                    BeginVertical("box");
                    LabelField($"{quest.DisplayName}", EditorStyles.boldLabel);

                    quest.Steps.ForEach((step) =>
                    {
                        GUI.color = step.Completed ? Color.green : Color.white;
                        LabelField($"- {step.DisplayName}", EditorStyles.boldLabel);
                    });
                    EndVertical();
                    GUI.color = Color.white;
                });
            }
        }

        void DrawSOsList()
        {
            _scroll = GUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _displayObjectsGUIDs.Length; i++)
            {
                BeginHorizontal(GUILayout.Width(250));
                GUILayout.Label(i + 1 + ". " + _displayObjects[i].name);

                if (GUILayout.Button(_searchIcon, GetSquareButtonStyle(27)))
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(_displayObjects[i]);
                }
                EndHorizontal();

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            GUILayout.EndScrollView();
        }

        string[] FindQuestLinesGUIDs() => AssetDatabase.FindAssets("t:Slax.QuestSystem.QuestLineSO");
        string[] FindQuestGUIDs() => AssetDatabase.FindAssets("t:Slax.QuestSystem.QuestSO");
        string[] FindQuestStepGUIDs() => AssetDatabase.FindAssets("t:Slax.QuestSystem.QuestStepSO");

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
                string path = "Packages/com.slax.questsystem/Editor/Textures/search.png";
                _searchIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            }

            if (_refreshIcon == null)
            {
                string path = "Packages/com.slax.questsystem/Editor/Textures/refresh.png";
                _refreshIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
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
