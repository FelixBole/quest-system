using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Slax.QuestSystem
{
    /// <summary>
    /// A quest is a collection of quest steps under a questline. Completing quest steps
    /// results in progress for the quest. Completing a quest results in progress of the
    /// associated quest line.
    /// </summary>
    [CreateAssetMenu(menuName = "Slax/QuestSystem/Quest", fileName = "QL0_Q0")]
    [System.Serializable]
    public class QuestSO : ScriptableObject
    {
        [SerializeField] protected string _name = "QL0_Q0";
        [TextArea]
        [SerializeField] protected string _description;
        [SerializeField] protected List<QuestStepSO> _steps;
        [SerializeField] protected Texture2D _sprite;

        [Header("Rewards")]
        [SerializeField] protected List<IQuestReward> _rewards = new List<IQuestReward>();

        /// <summary>
        /// List of rewards for the quest. If the QuestManager is set to
        /// grant rewards automatically, the QuestManager will grant these 
        /// rewards when the quest is completed. Otherwise, these rewards can
        /// be accessed on the QuestEventInfo event fired when the line is
        /// is completed.
        /// </summary>
        public List<IQuestReward> Rewards => _rewards;

        /// <summary>
        /// Event fired when the quest is completed for the QuestLineSO to handle.
        /// 
        /// <para>
        /// The QuestManager does not listen to this event as the QuestLineSO acts
        /// as the event relayer after updating itself with the new completion state
        /// of the quest. It will determine if the quest line is completed and fire
        /// the appropriate event for the QuestManager to handle.
        /// </para>
        /// </summary>
        public UnityAction<QuestSO, QuestStepSO> OnCompleted = delegate { };

        /// <summary>
        /// Event fired when the quest is in progress for the QuestManager to handle.
        /// 
        /// <para>
        /// The QuestLine does not need to listen to this event as if the quest is
        /// simply in progress, the QuestLine is not completed.
        /// </para>
        /// </summary>
        public UnityAction<QuestSO, QuestStepSO> OnProgress = delegate { };

        public string DisplayName => _name;
        public string Description => _description;
        public List<QuestStepSO> Steps => _steps;
        public bool Completed => !_steps.Find(step => !step.Completed);
        public Texture2D Sprite => _sprite;

        public void Initialize()
        {
            if (Completed) return;

            foreach (QuestStepSO step in _steps)
            {
                if (!step.Completed)
                {
                    step.OnCompleted += HandleStepCompletedEvent;
                }
            }
        }

        public QuestSO SetSteps(List<QuestStepSO> steps)
        {
            UnsubscribeFromSteps();
            _steps = steps;
            Initialize();
            return this;
        }

        public int GetStepIndex(QuestStepSO step) => _steps.FindIndex(s => s.name == step.name);

        public QuestStepSO GetStep(int index) => _steps[index];
        public QuestStepSO GetStep(string name) => _steps.Find(s => s.name == name);

        public QuestStepSO GetFirstStep() => _steps[0];
        public QuestStepSO GetLastStep() => _steps[_steps.Count - 1];

        /// <summary>
        /// Verifies if all the previous steps from the given steps
        /// have been completed
        /// </summary>
        public bool AllPreviousStepsCompleted(QuestStepSO step)
        {
            int idx = GetStepIndex(step);
            if (idx == -1) return false;
            if (idx == 0) return true;

            for (int i = 0; i < idx; i++)
            {
                if (!_steps[i].Completed) return false;
            }

            return true;
        }

        /// <summary>
        /// Handles the step completion in the verification pipeline to determine
        /// which event should be fired by the Quest Manager. If the quest is completed
        /// after the step validation, the event fired is received by the quest line to
        /// further verify questline completion. Otherwise, the quest progress event is
        /// fired for the Quest Manager to read and process.
        /// </summary>
        protected void HandleStepCompletedEvent(QuestStepSO step)
        {
            // Unsubscribe from the step completion event
            step.OnCompleted -= HandleStepCompletedEvent;

            if (Completed) OnCompleted.Invoke(this, step);
            else OnProgress.Invoke(this, step);
        }

        protected void UnsubscribeFromSteps()
        {
            foreach (QuestStepSO step in _steps)
            {
                step.OnCompleted -= HandleStepCompletedEvent;
            }
        }
    }
}
