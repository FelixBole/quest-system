using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Slax.QuestSystem
{
    /// <summary>
    /// Component to add to any interactable item or character that can start a quest
    /// or complete a quest step.
    /// </summary>
    public class QuestPoint : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] protected List<QuestStepSO> _questSteps = new List<QuestStepSO>();

        protected QuestStepSO _currentStep;

        [Header("Events")]
        /// <summary>
        /// Event fired when a step is started. It is worth noting that
        /// the Quest Manager also fires an event with the step, the associated
        /// quest and the questline (QuestEventInfo), but this event allows for
        /// some additionnal easy direct customization after the event fired
        /// by the Quest Manager Singleton Instance
        /// </summary>
        public UnityEvent<QuestStepSO> OnStepStarted;

        /// <summary>
        /// Event fired when a step has already been started and trying to
        /// start it again.
        /// </summary>
        public UnityEvent<QuestStepSO> OnStepAlreadyStarted;

        /// <summary>
        /// Event fired when a step has already been validated and trying
        /// to complete it again.
        /// </summary>
        public UnityEvent<QuestStepSO> OnStepAlreadyValidated;

        /// <summary>
        /// Event fired when a step is validated. It is worth noting that
        /// the Quest Manager also fires an event with the step, the associated
        /// quest and the questline (QuestEventInfo), but this event allows for
        /// some additionnal easy direct customization after the event fired
        /// by the Quest Manager Singleton Instance
        /// </summary>
        public UnityEvent<QuestStepSO> OnStepValidated;

        /// <summary>
        /// Attemps to Start the quest step if not already started.
        /// If not started, this method will run the quest validation
        /// event pipeline, making the QuestManager event fire before
        /// the questpoint OnStepStarted event
        /// </summary>
        public void StartStep()
        {
            // Find the first available quest step
            QuestStepSO step = _questSteps.Find(step => !step.Completed);

            Debug.Log("Starting Step " + step.name);

            if (step == null)
            {
                Debug.LogError("No Quest Step assigned to this Quest Point");
                return;
            }

            _currentStep = step;

            if (step.StartStep())
            {
                OnStepStarted.Invoke(step);
            }
        }

        public void StartSpecificStep(QuestStepSO step)
        {
            if (!_questSteps.Contains(step))
            {
                Debug.LogError("Quest Step not assigned to this Quest Point");
                return;
            }

            _currentStep = step;

            if (step.StartStep())
            {
                OnStepStarted.Invoke(step);
            }
        }

        /// <summary>
        /// Tries to process the quest step. If the step has already been started or completed
        /// will fire the OnAlreadyStarted or OnStepAlreadyValidated event and return. Otherwise, 
        /// it launch the Quest Manager step validation pipeline resulting in the Quest Manager 
        /// firing the full QuestEventInfo
        /// </summary>
        public void CompleteStep()
        {
            if (_currentStep == null)
            {
                Debug.LogError("No Quest Step assigned to this Quest Point");
                return;
            }

            if (!_currentStep.Started)
            {
                Debug.LogWarning("Quest Step not started yet, cannot complete it.");
                return;
            }

            if (_currentStep.Completed)
            {
                OnStepAlreadyValidated.Invoke(_currentStep);
                return;
            }
            else OnStepValidated.Invoke(_currentStep);
            _currentStep.CompleteStep();
        }
    }
}
