using System.Collections;
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
        [SerializeField] private string _name = "QL0_Q0";
        [TextArea]
        [SerializeField] private string _description;
        [SerializeField] private List<QuestStepSO> _steps;
        [SerializeField] private Texture2D _sprite;

        public UnityAction<QuestSO, QuestStepSO> OnCompleted = delegate { };
        public UnityAction<QuestSO, QuestStepSO> OnProgress = delegate { };

        public string DisplayName => _name;
        public string Description => _description;
        public List<QuestStepSO> Steps => _steps;
        public bool Completed => !_steps.Find(step => !step.Completed);
        public bool Started => _steps.Find(step => step.Started || step.Completed);
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

        public int GetStepIndex(QuestStepSO step) => _steps.FindIndex(s => s.name == step.name);

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
        private void HandleStepCompletedEvent(QuestStepSO step)
        {
            step.OnCompleted -= HandleStepCompletedEvent;
            // Check quest completion
            if (Completed) OnCompleted.Invoke(this, step);
            else OnProgress.Invoke(this, step);
        }
    }

}
