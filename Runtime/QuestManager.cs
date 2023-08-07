using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using Slax.Utilities;

namespace Slax.QuestSystem
{
    /// <summary>
    /// Manager for all created and ongoing quests. Handles interactions and holds all relations to necessary entities.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [Tooltip("This references a class that holds the List of quest lines to be able to use the JsonUtility to convert it to JSON")]
        [SerializeField] private List<QuestLineSO> _questLines = new List<QuestLineSO>();

        /// <summary>List of all quest lines in the game</summary>
        public List<QuestLineSO> QuestLines => _questLines;

        [SerializeField] private SaveType _saveType;
        [SerializeField] private ReturnType _returnType;
        [SerializeField] private string _saveFileName = "quests.savegame";

        public SaveType SaveType => _saveType;
        public ReturnType ReturnType => _returnType;

        public UnityAction<QuestEventInfo> OnStepStart = delegate { };
        public UnityAction<QuestEventInfo> OnStepComplete = delegate { };
        public UnityAction<QuestEventInfo> OnQuestComplete = delegate { };
        public UnityAction<QuestEventInfo> OnQuestLineComplete = delegate { };
        public UnityAction<List<QuestEventInfo>> OnMissingRequirements = delegate { };

        public static QuestManager Instance { get; private set; }

        #region MonoBehaviour

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // If a custom save type is used, Initialize should be called directly with the data
            // from another custom script
            if (_saveType == SaveType.Internal)
            {
                string saveData;
                bool fileExisted = FileManager.LoadFromFile(_saveFileName, out saveData);
                if (!fileExisted)
                {
                    ResetAllQuests();
                    Debug.Log($"Quests save file at {_saveFileName} did not exist. Quest Manager is set to default values.");
                    SubscribeToQuestEvents();
                }
                else
                {
                    Initialize(saveData);
                }
            }
        }
        private void OnDisable()
        {
            foreach (QuestLineSO questLine in _questLines)
            {
                questLine.OnCompleted -= HandleQuestLineCompletedEvent;
                questLine.OnProgress -= HandleQuestCompletedEvent;

                foreach (QuestSO quest in questLine.Quests)
                {
                    quest.OnProgress -= HandleStepCompletedEvent;
                }
            }
        }

        #endregion

        #region Setup And Initialization

        /// <summary>
        /// Sets up the Quest Manager from the save data when using json data such as <example>{"DoneQuestSteps":["QL1_Q1_S1","QL1_Q1_S2","QL1_Q2_S1"]}</example>
        /// </summary>
        public void Initialize(string jsonData)
        {
            SaveData save = JsonUtility.FromJson<SaveData>(jsonData);
            Debug.Log(save);
            Initialize(save.Steps);
        }

        /// <summary>
        /// Initializes the Quest Manager with save data when using List<string> saved data
        /// </summary>
        public void Initialize(List<SavedStep> savedSteps)
        {
            // Start by resetting all data
            ResetAllQuests();

            foreach (SavedStep savedStep in savedSteps)
            {
                QuestLineSO questLine = _questLines.Find(ql => ql.Quests.Find(q => q.Steps.Find(s => s.name == savedStep.Step)));
                if (!questLine) continue;
                int questIdx = questLine.Quests.FindIndex(q => q.Steps.Find(s => s.name == savedStep.Step));
                if (questIdx == -1) continue;
                int idx = questLine.Quests[questIdx].Steps.FindIndex(s => s.name == savedStep.Step);
                if (idx == -1) continue;
                questLine.Quests[questIdx].Steps[idx].InitAs(savedStep.State);
            }

            SubscribeToQuestEvents();
        }

        /// <summary>Subscribes to essential events fired by quests and questlines</summary>
        private void SubscribeToQuestEvents()
        {
            foreach (QuestLineSO questLine in _questLines)
            {
                questLine.Initialize();

                questLine.OnCompleted += HandleQuestLineCompletedEvent;
                questLine.OnProgress += HandleQuestCompletedEvent;

                foreach (QuestSO quest in questLine.Quests)
                {
                    if (!quest.Completed)
                    {
                        quest.OnProgress += HandleStepCompletedEvent;
                    }

                    foreach (QuestStepSO step in quest.Steps)
                    {
                        if (step.State != StepState.NotStarted) continue;
                        step.OnStarted += HandleStepStartEvent;
                        step.OnMissingRequirements += HandleMissingRequirementsEvent;
                    }
                }
            }
        }

        /// <summary>
        /// Resets all Quest Steps to false before loading in save data
        /// </summary>
        private void ResetAllQuests()
        {
            foreach (QuestLineSO questLine in _questLines)
            {
                foreach (QuestSO quest in questLine.Quests)
                {
                    foreach (QuestStepSO step in quest.Steps)
                    {
                        step.InitAs(StepState.NotStarted);
                    }
                }
            }
        }

        #endregion

        #region Quest Events

        ///<summary>
        ///Handles the event fired from a Step when the step
        /// attemps to start but there are some missing step
        /// requirements.
        ///</summary>
        private void HandleMissingRequirementsEvent(List<QuestStepSO> requirements)
        {
            List<QuestEventInfo> eventInfoRequirements = new List<QuestEventInfo>();
            foreach (QuestStepSO requirement in requirements)
            {
                requirement.OnMissingRequirements -= HandleMissingRequirementsEvent;
                eventInfoRequirements.Add(PrepareQuestEventInfo(requirement));
            }
            OnMissingRequirements.Invoke(eventInfoRequirements);
        }

        /// <summary>
        /// Handles the event fired from a Step when the
        /// step starts. The QuestEventInfo will hold the
        /// appropriate info to know if it is a Quest start
        /// or a QuestLine start for any listener to process
        /// </summary>
        private void HandleStepStartEvent(QuestStepSO step)
        {
            step.OnStarted -= HandleStepStartEvent;
            QuestEventInfo eventInfo = PrepareQuestEventInfo(step);
            OnStepStart.Invoke(eventInfo);
        }

        /// <summary>
        /// Handles the event fired from a Quest whenever a step
        /// is completed but the quest is not completed yet
        /// </summary>
        private void HandleStepCompletedEvent(QuestSO quest, QuestStepSO step)
        {
            quest.OnProgress -= HandleStepCompletedEvent;
            QuestEventInfo eventInfo = PrepareQuestEventInfo(step);
            OnStepComplete.Invoke(eventInfo);
        }

        /// <summary>
        /// Handles the event fired from the QuestLine whenever a step is completed
        /// and the associated Quest is completed as well but the associated QuestLine
        /// is not yet completed
        /// </summary>
        private void HandleQuestCompletedEvent(QuestLineSO questLine, QuestSO quest, QuestStepSO step)
        {
            questLine.OnProgress -= HandleQuestCompletedEvent;
            QuestEventInfo eventInfo = new QuestEventInfo(questLine, quest, step);
            OnQuestComplete.Invoke(eventInfo);
        }

        /// <summary>
        /// Handles the event fired from the QuestLine whenever a step is completed,
        /// completing the associated Quest and QuestLine with it. It is possible to handle
        /// individual completion of the step and quest with this event as the QuestEventInfo
        /// holds the information for the completed QuestLine as well as what Quest and Step
        /// triggered the completion
        /// </summary>
        private void HandleQuestLineCompletedEvent(QuestLineSO questLine, QuestSO quest, QuestStepSO step)
        {
            questLine.OnCompleted -= HandleQuestLineCompletedEvent;
            OnQuestLineComplete.Invoke(new QuestEventInfo(questLine, quest, step));
        }

        #endregion

        #region Helpers

        public QuestSO QuestFromStep(QuestStepSO questStep)
        {
            QuestLineSO questLine = QuestLineFromQuestStep(questStep);
            if (!questLine) throw new Exception("No QuestLine found for this Quest Step");
            QuestSO quest = questLine.Quests.Find(q => q.Steps.Find(s => s.name == questStep.name));
            return quest;
        }

        private QuestLineSO QuestLineFromQuest(QuestSO quest) => _questLines.Find(ql => ql.Quests.Find(q => q.name == quest.name));

        private QuestLineSO QuestLineFromQuestStep(QuestStepSO step) => _questLines.Find(ql => ql.Quests.Find(q => q.Steps.Find(s => s.name == step.name)));

        /// <summary>Prepares the data to be sent by Quest Manager events</summary>
        private QuestEventInfo PrepareQuestEventInfo(QuestStepSO step)
        {
            QuestLineSO questLine = QuestLineFromQuestStep(step);
            QuestSO quest = QuestFromStep(step);
            return new QuestEventInfo(questLine, quest, step);
        }

        #endregion

        /// <summary>
        /// Returns the Done Quest Steps List as JSON parsed data or
        /// directly as List<SavedStep> depending on the set ReturnType
        /// </summary>
        [ContextMenu("Save")]
        public dynamic CreateSaveData()
        {
            SaveData save = new SaveData();
            foreach (QuestLineSO questLine in _questLines)
            {
                foreach (QuestSO quest in questLine.Quests)
                {
                    foreach (QuestStepSO step in quest.Steps)
                    {
                        save.Steps.Add(new SavedStep(step.name, step.State));
                    }
                }
            }

            string saveDataJSON = JsonUtility.ToJson(save);

            if (_saveType == SaveType.Custom)
            {
                if (_returnType == ReturnType.StepList) return save.Steps;
                return saveDataJSON;
            }

            FileManager.WriteToFile(_saveFileName, saveDataJSON);
            return _returnType == ReturnType.JSON ? saveDataJSON : save.Steps;
        }

#if UNITY_EDITOR
        #region Editor Methods

        public void ManualReset()
        {
            ResetAllQuests();
            Debug.Log("Quest Manager resetted, all quests marked as not completed.");
        }

        public void ManualSave()
        {
            var data = CreateSaveData();
            if (data is string)
            {
                if (_saveType == SaveType.Internal)
                {
                    Debug.Log($"Saved Quests Data to {_saveFileName}");
                    return;
                }
            }

            Debug.Log($"Quest Manager save type not set as internal. Returning data as : {data}");
        }

        #endregion
#endif
    }

    /// <summary>
    /// Sets the way the quests will be saved. Internal is handled by the Quest System and Custom
    /// simply sends back the data in the selected format to plug in any custom save system
    /// </summary>
    public enum SaveType
    {
        /// <summary>
        /// Setting save type to internal will let the QuestSystem manage the quest savefile
        /// By default it will read/write to quests.savegame, but this is customizable
        /// </summary>
        Internal,

        /// <summary>
        /// If you want to use a custom save system, setting this value will return the data in
        /// the type selected by the ReturnType field
        /// </summary>
        Custom,
    }

    /// <summary>
    /// Sets the type of data returned in a custom save type
    /// </summary>
    public enum ReturnType
    {
        /// <summary>
        /// Setting this return type value will return data in the following format : {"DoneQuestSteps":["QL1_Q1_S1","QL1_Q1_S2","QL1_Q2_S1"]}
        /// </summary>
        JSON,

        /// <summary>
        /// Setting this return type value will return data as a List<string> with the names/ids of the completed quests
        /// allowing for custom processing of the data before save if needed
        /// </summary>
        StepList,
    }

    /// <summary>
    /// The completion state of a step. This information is used when saving the steps to disk
    /// </summary>
    public enum StepState
    {
        NotStarted = 0,
        Started = 1,
        Completed = 2,
    }

    /// <summary>
    /// Information sent from unity actions / events on certain triggers
    /// such as quest start / end / checkpoint for external managers (like UI)
    /// to be able to display or use such information
    /// </summary>
    public struct QuestEventInfo
    {
        /// <summary>The current questline</summary>
        public QuestLineSO QuestLine;
        /// <summary>The current quest for which the event was sent</summary>
        public QuestSO Quest;
        /// <summary>The current step for which the event was sent</summary>
        public QuestStepSO Step;
        /// <summary>If the step validated was the first step, meaning the quest just started</summary>
        public bool IsQuestStart;
        /// <summary>If the step validated was the first step of the quest & of the first quest of the questline</summary>
        public bool IsQuestLineStart;

        public QuestEventInfo(QuestLineSO questLine, QuestSO quest, QuestStepSO step)
        {
            this.QuestLine = questLine;
            this.Quest = quest;
            this.Step = step;
            this.IsQuestStart = quest.Steps.FindIndex(s => s.name == step.name) == 0;
            this.IsQuestLineStart = questLine.Quests.FindIndex(q => q.name == quest.name) == 0 && this.IsQuestStart;
        }
    }
}
