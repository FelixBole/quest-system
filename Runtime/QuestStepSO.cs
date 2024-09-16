using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace Slax.QuestSystem
{
    /// <summary>
    /// Every step of a quest from its starting point to the goal of the quest.
    /// The action of starting a quest always corresponds to step 0, so a quest
    /// giver holds quest step 0 of a quest and validates that step to start the quest.
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(menuName = "Slax/QuestSystem/QuestStep", fileName = "QL0_Q0_S0")]
    public class QuestStepSO : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] protected bool _useLocalization = false;

        [Header("Quest Step Info")]
        [SerializeField] protected string _name = "QL0_Q0_S0";
        [SerializeField] protected LocalizedString _localizedName;
        [SerializeField, TextArea] protected string _description = "Quest Step 0";
        [SerializeField] protected LocalizedString _localizedDescription;
        [SerializeField] protected Sprite _sprite;
        [SerializeField] protected LocalizedSprite _localizedSprite;
        [SerializeField] protected StepState _state = StepState.NotStarted;

        [Header("Requirements & Conditions")]

        [SerializeField]
        [Tooltip("Other quest steps that need to be completed so this one can start")]
        protected List<QuestStepSO> _requirements = new List<QuestStepSO>();

        [SerializeField]
        [Tooltip("Custom conditions that run when the step is started and completed")]
        protected List<IQuestCondition> _stepConditions = new List<IQuestCondition>();

        [Header("Rewards")]
        [SerializeField] protected List<IQuestReward> _rewards = new List<IQuestReward>();

        /// <summary>
        /// Event fired when the step is started for the QuestManager to handle.
        /// 
        /// <para>
        /// The QuestSO does not listen to this event as it is not necessary for the quest to
        /// know when a step is started. The QuestManager listens to this event to update the
        /// quest progress.
        /// </para>
        /// </summary>
        public UnityAction<QuestStepSO> OnStarted = delegate { };

        /// <summary>
        /// Event fired when the step is completed for the QuestSO to handle.
        /// 
        /// <para>
        /// This event should not be listened to by the QuestManager as it the
        /// QuestSO runs the completion event pipeline.
        /// </para>
        /// </summary>
        public UnityAction<QuestStepSO> OnCompleted = delegate { };

        /// <summary>
        /// Event fired when there are requirements missing to start the step.
        /// 
        /// <para>
        /// Any subscriber can handle this event but the QuestManager relays it
        /// inside of a QuestEventInfo event in the QuestManager.OnMissingRequirements 
        /// event.
        /// </para>
        /// </summary>
        public UnityAction<QuestStepSO, List<QuestStepSO>> OnMissingRequirements = delegate { };

        /// <summary>
        /// Event fired when the start step is attempted but the custom conditions are not met.
        /// The list of conditions that failed is passed as a parameter.
        /// 
        /// <para>
        /// Any subscriber can handle this event but the QuestManager relays it inside
        /// of a QuestEventInfo event in the QuestManager.OnStepStartConditionsNotMet event.
        /// </para>
        /// </summary>
        public UnityAction<QuestStepSO, List<IQuestCondition>> OnStartConditionsNotMet = delegate { };

        /// <summary>
        /// Event fired when the complete step is attempted but the custom conditions are not met.
        /// The list of conditions that failed is passed as a parameter.
        /// 
        /// <para>
        /// Any subscriber can handle this event but the QuestManager relays it inside
        /// of a QuestEventInfo event in the QuestManager.OnStepCompleteConditionsNotMet event.
        /// </para>
        /// </summary>
        public UnityAction<QuestStepSO, List<IQuestCondition>> OnCompleteConditionsNotMet = delegate { };

        public string DisplayName => _useLocalization ? _localizedName.GetLocalizedString() : _name;
        public string Description => _useLocalization ? _localizedDescription.GetLocalizedString() : _description;
        public Sprite Sprite => _useLocalization ? _localizedSprite.LoadAsset() : _sprite;
        public StepState State => _state;
        public bool Started => _state == StepState.Started;
        public bool Completed => _state == StepState.Completed;

        /// <summary>Check to see if the list of requirement to start the step has been met</summary>
        public bool IsRequirementsMet => !_requirements.Find(s => !s.Completed);

        /// <summary>List of steps required prior to activate this step</summary>
        public List<QuestStepSO> Requirements => _requirements;

        /// <summary>
        /// List of custom conditions that need to be validated on start and
        /// completion of the step. Conditions are checked in order and if
        /// any condition fails, the step will not start or complete.
        /// 
        /// <para>
        /// If you need only start conditions, you can setup the IQuesCondition
        /// to only check the start conditions and return true on CanComplete(),
        /// and vice versa for only complete conditions.
        /// </para>
        /// </summary>
        public List<IQuestCondition> StepConditions => _stepConditions;

        /// <summary>
        /// List of rewards for the quest step. If the QuestManager is set to
        /// grant rewards automatically, the QuestManager will grant these rewards
        /// when the quest step is completed. Otherwise, these rewards can still
        /// be accessed on the QuestEventInfo event fired when the quest step is
        /// completed.
        /// </summary>
        public List<IQuestReward> Rewards => _rewards;

        /// <summary>
        /// Set the requirements for the step to be started.
        /// </summary>
        public QuestStepSO SetRequirements(List<QuestStepSO> requirements)
        {
            _requirements = requirements;
            return this;
        }

        /// <summary>
        /// Set the conditions that need to be met for the step to be started.
        /// </summary>
        public QuestStepSO SetStepConditions(List<IQuestCondition> conditions)
        {
            _stepConditions = conditions;
            return this;
        }

        /// <summary>
        /// Check if the step can be started based on the conditions set in the step.
        /// </summary>
        public bool CanStartStep()
        {
            List<IQuestCondition> failedConditions = new List<IQuestCondition>();
            foreach (IQuestCondition condition in _stepConditions)
            {
                if (!condition.CanStart())
                {
                    failedConditions.Add(condition);
                }

                if (failedConditions.Count > 0)
                {
                    OnStartConditionsNotMet?.Invoke(this, failedConditions);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempt to start the step if the requirements & start conditions are met.
        /// </summary>
        public bool StartStep()
        {
            if (!CanStartStep()) return false;
            return StepUpdate(StepState.Started);
        }
        
        /// <summary>
        /// Attempts to complete the step if the complete conditions are met.
        /// </summary>
        public bool CompleteStep()
        {
            List<IQuestCondition> failedConditions = new List<IQuestCondition>();
            foreach (IQuestCondition condition in _stepConditions)
            {
                if (!condition.CanComplete())
                {
                    failedConditions.Add(condition);
                }

                if (failedConditions.Count > 0)
                {
                    OnCompleteConditionsNotMet?.Invoke(this, failedConditions);
                    return false;
                }
            }

            return StepUpdate(StepState.Completed);
        }

        /// <summary>
        /// Updates the state of the step and fires the appropriate event.
        /// </summary>
        public bool StepUpdate(StepState state)
        {
            if (state == StepState.Started)
            {
                if (!IsRequirementsMet)
                {
                    List<QuestStepSO> missingRequirements = _requirements.FindAll(s => !s.Completed);
                    OnMissingRequirements?.Invoke(this, missingRequirements);
                    return false;
                }

                _state = StepState.Started;
                OnStarted?.Invoke(this);
            }
            else if (state == StepState.Completed)
            {
                _state = state;
                OnCompleted?.Invoke(this);
            }
            else
            {
                _state = state;
            }

            return true;
        }

        /// <summary>
        /// Initializes the completion state without firing an event.
        /// Used for loading from save data to setup quests
        /// </summary>
        public void InitAs(StepState state)
        {
            _state = state;
        }
    }
}
