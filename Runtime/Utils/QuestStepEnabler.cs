using UnityEngine;
using UnityEngine.Events;
namespace Slax.QuestSystem
{
    /// <summary>
    /// Class handling the component's enable state depending on
    /// the quest step it should be available for
    /// </summary>
    public class QuestStepEnabler : MonoBehaviour
    {
        [SerializeField] private QuestStepSO _questStep;
        [SerializeField] private bool _skipStartCheck;
        private QuestSO _quest;
        private bool _isEnabled;
        public bool IsEnabled => _isEnabled;

        public UnityAction<bool> OnEnableChange = delegate { };

        private void Awake()
        {
            _quest = QuestManager.Instance.QuestFromStep(_questStep);
            if (!_quest) throw new System.Exception("Could not find quest from provided quest step. Please make sure the quest step is setup in the QuestSO");

            int stepIdx = _quest.GetStepIndex(_questStep);
            if (stepIdx == -1) throw new System.Exception("Cannot use a step that isn't in a quest.");

            if (_skipStartCheck)
            {
                _isEnabled = _quest.AllPreviousStepsCompleted(_questStep) && _questStep.IsRequirementsMet;
            }
            else
            {
                _isEnabled = _quest.AllPreviousStepsCompleted(_questStep) && _questStep.IsRequirementsMet && _questStep.Started;
            }
            OnEnableChange.Invoke(_isEnabled);
        }

        private void OnEnable()
        {
            QuestManager.Instance.OnStepComplete += VerifyActivation;
            _questStep.OnCompleted += HandleStepComplete;
        }

        private void OnDisable()
        {
            QuestManager.Instance.OnStepComplete -= VerifyActivation;
            _questStep.OnCompleted -= HandleStepComplete;
        }

        private void HandleStepComplete(QuestStepSO step)
        {
            _isEnabled = false;
            OnEnableChange.Invoke(_isEnabled);
        }

        private void VerifyActivation(QuestEventInfo eventInfo)
        {
            if (eventInfo.Quest.name != _quest.name) return;
            _isEnabled = _quest.AllPreviousStepsCompleted(_questStep) && _questStep.IsRequirementsMet;
            OnEnableChange.Invoke(_isEnabled);
        }
    }

}