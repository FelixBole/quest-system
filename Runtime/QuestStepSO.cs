using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
        [SerializeField] private string _name = "QL0_Q0_S0";
        [SerializeField] private StepState _state = StepState.NotStarted;
        [SerializeField] private List<QuestStepSO> _requirements = new List<QuestStepSO>();

        /// <summary>Event fired when the step is started for the QuestManager to handle</summary>
        public UnityAction<QuestStepSO> OnStarted = delegate { };

        /// <summary>Event fired when the step is completed for the QuestSO to handle</summary>
        public UnityAction<QuestStepSO> OnCompleted = delegate { };

        /// <summary>Event fired when there are requirements missing and there is an attempt to start a quest</summary>
        public UnityAction<List<QuestStepSO>> OnMissingRequirements = delegate { };

        public string DisplayName => _name;
        public StepState State => _state;
        public bool Started => _state == StepState.Started;
        public bool Completed => _state == StepState.Completed;

        /// <summary>Check to see if the list of requirement to start the step has been met</summary>
        public bool IsRequirementsMet => !_requirements.Find(s => !s.Completed);

        /// <summary>List of steps required prior to activate this step</summary>
        public List<QuestStepSO> Requirements => _requirements;

        public bool StartStep() => StepUpdate(StepState.Started);
        public bool CompleteStep() => StepUpdate(StepState.Completed);

        public bool StepUpdate(StepState state)
        {
            if (state == StepState.Started)
            {
                if (!IsRequirementsMet)
                {
                    List<QuestStepSO> missingRequirements = _requirements.FindAll(s => !s.Completed);
                    OnMissingRequirements.Invoke(missingRequirements);
                    return false;
                }

                _state = StepState.Started;
                OnStarted.Invoke(this);
            }
            else if (state == StepState.Completed)
            {
                _state = state;
                OnCompleted.Invoke(this);
            }
            else
            {
                _state = state;
                // OnReset event needed ?
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
