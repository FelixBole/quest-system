using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace Slax.QuestSystem
{
    /// <summary>
    /// Manager for all created and ongoing quests. Handles interactions and holds all relations to necessary entities.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's quest lines.")] protected List<QuestLineSO> _questLines = new List<QuestLineSO>();

        /// <summary>List of all quest lines in the game</summary>
        public List<QuestLineSO> QuestLines => _questLines;

        [SerializeField] protected SaveType _saveType;
        [SerializeField] protected ReturnType _returnType;
        [SerializeField] protected string _saveFileName = "quests.savegame";

        [SerializeField] protected bool _grantRewards = true;

        public SaveType SaveType => _saveType;
        public ReturnType ReturnType => _returnType;

        /// <summary>
        /// Event fired when a step is started. The QuestEventInfo will hold the appropriate info to know if it is a Quest start
        /// or a QuestLine start for any listener to process
        /// </summary>
        public UnityAction<QuestEventInfo> OnStepStart = delegate { };

        /// <summary>
        /// Event fired when a step is completed but the quest is not completed yet
        /// </summary>
        public UnityAction<QuestEventInfo> OnStepComplete = delegate { };

        /// <summary>
        /// Event fired when a step is completed, completing the associated Quest
        /// but not the QuestLine
        /// </summary>
        public UnityAction<QuestEventInfo> OnQuestComplete = delegate { };

        /// <summary>
        /// Event fired when a step is completed, completing the associated Quest and QuestLine with it
        /// </summary>
        public UnityAction<QuestEventInfo> OnQuestLineComplete = delegate { };

        /// <summary>
        /// Event fired when a step is started but there are some missing step requirements
        /// </summary>
        public UnityAction<QuestEventInfo> OnMissingRequirements = delegate { };

        /// <summary>
        /// Event fired when a step is started but there are some missing step requirements
        /// </summary>
        public UnityAction<QuestEventInfo> OnStepStartConditionsNotMet = delegate { };

        /// <summary>
        /// Event fired when a step is started but there are some missing step requirements
        /// </summary>
        public UnityAction<QuestEventInfo> OnStepCompleteConditionsNotMet = delegate { };

        /// <summary>
        /// Singleton instance of the QuestManager
        /// </summary>
        public static QuestManager Instance { get; protected set; }

        #region MonoBehaviour

        protected virtual void Awake()
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
                    Log($"Quests save file at {_saveFileName} did not exist. Quest Manager is set to default values.");
                    SubscribeToQuestEvents();
                }
                else
                {
                    Initialize(saveData);
                }
            }
        }
        protected virtual void OnDisable()
        {
            UnsuscribeFromQuestEvents();
        }

        #endregion

        #region Setup And Initialization

        /// <summary>
        /// Sets up the Quest Manager from the save data when using json data such as 
        /// <para>
        /// <example>{"Steps":["QL1_Q1_S1","QL1_Q1_S2","QL1_Q2_S1"]}</example>
        /// </para>
        /// </summary>
        public virtual void Initialize(string jsonData)
        {
            SaveData save = JsonUtility.FromJson<SaveData>(jsonData);
            Log(save);
            Initialize(save.Steps);
        }

        /// <summary>
        /// Initializes the Quest Manager with save data when using List<string> saved data.
        /// Basically runs through all scriptable objects in the questlines and sets the state
        /// of the steps to the saved state.
        /// </summary>
        public virtual void Initialize(List<SavedStep> savedSteps)
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

        public QuestManager SetGrantRewards(bool grantRewards)
        {
            _grantRewards = grantRewards;
            return this;
        }

        /// <summary>Subscribes to essential events fired by quests and questlines</summary>
        protected virtual void SubscribeToQuestEvents()
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
                        if (step.State == StepState.NotStarted)
                        {
                            step.OnStarted += HandleStepStartEvent;
                            step.OnMissingRequirements += HandleStepRequirementsNotMetEvent;
                            step.OnStartConditionsNotMet += HandleStepStartConditionsNotMetEvent;
                        }

                        if (step.State == StepState.Started)
                        {
                            step.OnCompleteConditionsNotMet += HandleStepCompleteConditionsNotMetEvent;
                        }
                    }
                }
            }
        }

        protected virtual void UnsuscribeFromQuestEvents()
        {
            foreach (QuestLineSO questLine in _questLines)
            {
                questLine.OnCompleted -= HandleQuestLineCompletedEvent;
                questLine.OnProgress -= HandleQuestCompletedEvent;

                foreach (QuestSO quest in questLine.Quests)
                {
                    quest.OnProgress -= HandleStepCompletedEvent;

                    foreach (QuestStepSO step in quest.Steps)
                    {
                        step.OnStarted -= HandleStepStartEvent;
                        step.OnMissingRequirements -= HandleStepRequirementsNotMetEvent;
                        step.OnStartConditionsNotMet -= HandleStepStartConditionsNotMetEvent;
                        step.OnCompleteConditionsNotMet -= HandleStepCompleteConditionsNotMetEvent;
                    }
                }
            }
        }

        /// <summary>
        /// Resets all Quest Steps to false before loading in save data
        /// </summary>
        protected virtual void ResetAllQuests()
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
        /// Handles the event fired from a Step when the step attemps to start but there 
        /// are some missing step requirements.
        ///</summary>
        ///<param name="requirements">The list of requirements that are missing</param>
        protected virtual void HandleStepRequirementsNotMetEvent(QuestStepSO notifier, List<QuestStepSO> requirements)
        {
            QuestEventInfo eventInfo = PrepareQuestEventInfo(notifier);
            eventInfo.Requirements = requirements;
            eventInfo.ConditionType = QuestEventInfo.ConditionNotMetType.StepRequirements;
            OnMissingRequirements.Invoke(eventInfo);
        }


        ///<summary>
        /// Handles the event fired from a Step when the step attemps to start but there
        /// are some missing step start conditions.
        /// </summary>
        /// <param name="step">The step that was started</param>
        /// <param name="conditions">The list of conditions that were not met</param>
        protected virtual void HandleStepStartConditionsNotMetEvent(QuestStepSO step, List<IQuestCondition> conditions)
        {
            QuestEventInfo eventInfo = PrepareQuestEventInfo(step);
            eventInfo.Conditions = conditions;
            eventInfo.ConditionType = QuestEventInfo.ConditionNotMetType.StepStartConditions;
            OnStepStartConditionsNotMet.Invoke(eventInfo);
        }

        /// <summary>
        /// Handles the event fired from a Step when the step attemps to complete but there
        /// are some missing step complete conditions.
        /// </summary>
        /// <param name="step">The step that was completed</param>
        /// <param name="conditions">The list of conditions that were not met</param>
        protected virtual void HandleStepCompleteConditionsNotMetEvent(QuestStepSO step, List<IQuestCondition> conditions)
        {
            QuestEventInfo eventInfo = PrepareQuestEventInfo(step);
            eventInfo.Conditions = conditions;
            eventInfo.ConditionType = QuestEventInfo.ConditionNotMetType.StepCompleteConditions;
            OnStepCompleteConditionsNotMet.Invoke(eventInfo);
        }

        /// <summary>
        /// Handles the event fired from a Step when the
        /// step starts. The QuestEventInfo will hold the
        /// appropriate info to know if it is a Quest start
        /// or a QuestLine start for any listener to process
        /// </summary>
        /// <param name="step">The step that was started</param>
        protected virtual void HandleStepStartEvent(QuestStepSO step)
        {
            step.OnStarted -= HandleStepStartEvent;
            QuestEventInfo eventInfo = PrepareQuestEventInfo(step);
            OnStepStart.Invoke(eventInfo);
        }

        /// <summary>
        /// Handles the event fired from a Quest whenever a step
        /// is completed but the quest is not completed yet
        /// </summary>
        /// <param name="quest">The quest that the step belongs to</param>
        /// <param name="step">The step that was completed</param>
        protected virtual void HandleStepCompletedEvent(QuestSO quest, QuestStepSO step)
        {
            quest.OnProgress -= HandleStepCompletedEvent;

            if (_grantRewards)
            {
                foreach (IQuestReward reward in step.Rewards)
                {
                    reward.GrantReward();
                }
            }

            QuestEventInfo eventInfo = PrepareQuestEventInfo(step);
            OnStepComplete.Invoke(eventInfo);
        }

        /// <summary>
        /// Handles the event fired from the QuestLine whenever a step is completed
        /// and the associated Quest is completed as well but the associated QuestLine
        /// is not yet completed
        /// </summary>
        /// <param name="questLine">The questline that the quest belongs to</param>
        /// <param name="quest">The quest that was completed</param>
        /// <param name="step">The step that was completed</param>
        protected virtual void HandleQuestCompletedEvent(QuestLineSO questLine, QuestSO quest, QuestStepSO step)
        {
            questLine.OnProgress -= HandleQuestCompletedEvent;

            if (_grantRewards)
            {
                // We also grant the rewards for step here because the
                // step completion event is not fired when the quest is completed
                foreach (IQuestReward reward in step.Rewards)
                {
                    reward.GrantReward();
                }

                foreach (IQuestReward reward in quest.Rewards)
                {
                    reward.GrantReward();
                }
            }

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
        /// <param name="questLine">The questline that was completed</param>
        /// <param name="quest">The quest that was completed</param>
        /// <param name="step">The step that was completed</param>
        protected virtual void HandleQuestLineCompletedEvent(QuestLineSO questLine, QuestSO quest, QuestStepSO step)
        {
            questLine.OnCompleted -= HandleQuestLineCompletedEvent;

            if (_grantRewards)
            {
                // We also grant the rewards for step and quest here because the
                // step / quest completion events are not fired when the questline
                // is completed.
                foreach (IQuestReward reward in step.Rewards)
                {
                    reward.GrantReward();
                }

                foreach (IQuestReward reward in quest.Rewards)
                {
                    reward.GrantReward();
                }

                foreach (IQuestReward reward in questLine.Rewards)
                {
                    reward.GrantReward();
                }
            }

            OnQuestLineComplete.Invoke(new QuestEventInfo(questLine, quest, step));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the QuestSO inside of which the QuestStepSO is found
        /// </summary>
        /// <param name="questStep">The quest step to look for in the available questlines</param>
        public virtual QuestSO QuestFromStep(QuestStepSO questStep)
        {
            QuestLineSO questLine = QuestLineFromQuestStep(questStep);
            if (!questLine) throw new Exception("No QuestLine found for this Quest Step");
            QuestSO quest = questLine.Quests.Find(q => q.Steps.Find(s => s.name == questStep.name));
            return quest;
        }

        /// <summary>
        /// Returns the QuestLineSO inside of which the QuestSO is found
        /// </summary>
        /// <param name="quest">The quest to look for in the available questlines</param>
        public virtual QuestLineSO QuestLineFromQuest(QuestSO quest) => _questLines.Find(ql => ql.Quests.Find(q => q.name == quest.name));

        /// <summary>
        /// Returns the QuestLineSO inside of which the QuestStepSO is found
        /// </summary>
        /// <param name="step">The quest step to look for in the available questlines</param>
        public virtual QuestLineSO QuestLineFromQuestStep(QuestStepSO step) => _questLines.Find(ql => ql.Quests.Find(q => q.Steps.Find(s => s.name == step.name)));

        /// <summary>Prepares the data to be sent by Quest Manager events</summary>
        /// <param name="step">The step for which the event is being prepared</param>
        protected virtual QuestEventInfo PrepareQuestEventInfo(QuestStepSO step)
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
        public virtual dynamic CreateSaveData()
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

        [SerializeField] protected bool _logMessages = false;

        protected void Log(object message)
        {
            if (_logMessages) Debug.Log(message);
        }

        public void ManualReset()
        {
            ResetAllQuests();
            Log("Quest Manager resetted, all quests marked as not completed.");
        }

        public void ManualSave()
        {
            var data = CreateSaveData();
            if (data is string)
            {
                if (_saveType == SaveType.Internal)
                {
                    Log($"Saved Quests Data to {_saveFileName}");
                    return;
                }
            }

            Log($"Quest Manager save type not set as internal. Returning data as : {data}");
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
        /// <summary>
        /// The type of condition that was not met, set to None if it isn't an event
        /// for conditions that were not met.
        /// </summary>
        public enum ConditionNotMetType
        {
            None,
            StepRequirements,
            StepStartConditions,
            StepCompleteConditions,
        }

        /// <summary>The type of condition that was not met</summary>
        public ConditionNotMetType ConditionType;

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

        /// <summary>The list of conditions that were not met for the step to start / complete</summary>
        public List<IQuestCondition> Conditions;

        /// <summary>
        /// The list of step completion requirements that were not met for the step to start
        /// </summary>
        public List<QuestStepSO> Requirements;

        public QuestEventInfo(QuestLineSO questLine, QuestSO quest, QuestStepSO step, ConditionNotMetType conditionType = ConditionNotMetType.None)
        {
            QuestLine = questLine;
            Quest = quest;
            Step = step;
            IsQuestStart = quest.Steps.FindIndex(s => s.name == step.name) == 0;
            IsQuestLineStart = questLine.Quests.FindIndex(q => q.name == quest.name) == 0 && IsQuestStart;
            ConditionType = conditionType;
            Conditions = new List<IQuestCondition>();
            Requirements = new List<QuestStepSO>();
        }
    }
}
