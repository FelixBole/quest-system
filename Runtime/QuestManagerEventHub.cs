using UnityEngine;
using UnityEngine.Events;

namespace Slax.QuestSystem
{
    /// <summary>
    /// Event hub for the QuestManager. This class is used to listen to the 
    /// QuestManager events and trigger UnityEvents when they are fired.
    /// 
    /// <para>
    /// This class is to facilitate usage for those who prefer assigning UnityEvents
    /// in the inspector rather than subscribing to the events in code.
    /// </para>
    /// </summary>
    public class QuestManagerEventHub : MonoBehaviour
    {
        public UnityEvent<QuestEventInfo> OnMissingRequirements;
        public UnityEvent<QuestEventInfo> OnStepStartConditionsNotMet;
        public UnityEvent<QuestEventInfo> OnStepCompleteConditionsNotMet;
        public UnityEvent<QuestEventInfo> OnStepStart;
        public UnityEvent<QuestEventInfo> OnStepComplete;
        public UnityEvent<QuestEventInfo> OnQuestComplete;
        public UnityEvent<QuestEventInfo> OnQuestLineComplete;

        protected virtual void OnEnable()
        {
            QuestManager.Instance.OnMissingRequirements += ShowMissingRequirements;
            QuestManager.Instance.OnStepStartConditionsNotMet += ShowStepStartConditionsNotMet;
            QuestManager.Instance.OnStepCompleteConditionsNotMet += ShowStepCompleteConditionsNotMet;
            QuestManager.Instance.OnStepStart += ShowStepStart;
            QuestManager.Instance.OnStepComplete += ShowStepComplete;
            QuestManager.Instance.OnQuestComplete += ShowQuestComplete;
            QuestManager.Instance.OnQuestLineComplete += ShowQuestLineComplete;
        }

        protected virtual void OnDisable()
        {
            QuestManager.Instance.OnMissingRequirements -= ShowMissingRequirements;
            QuestManager.Instance.OnStepStartConditionsNotMet -= ShowStepStartConditionsNotMet;
            QuestManager.Instance.OnStepCompleteConditionsNotMet -= ShowStepCompleteConditionsNotMet;
            QuestManager.Instance.OnStepStart -= ShowStepStart;
            QuestManager.Instance.OnStepComplete -= ShowStepComplete;
            QuestManager.Instance.OnQuestComplete -= ShowQuestComplete;
            QuestManager.Instance.OnQuestLineComplete -= ShowQuestLineComplete;
        }

        protected virtual void ShowMissingRequirements(QuestEventInfo eventInfo)
        {
#if UNITY_EDITOR
            Log($"Missing {eventInfo.Requirements.Count} requirements before starting this step !");
            {
                foreach (QuestStepSO req in eventInfo.Requirements)
                {
                    Log($"{req.DisplayName} from quest {eventInfo.Quest.DisplayName} is a required step.");
                }
            }
#endif
            OnMissingRequirements?.Invoke(eventInfo);
        }

        protected virtual void ShowStepStart(QuestEventInfo eventInfo)
        {
#if UNITY_EDITOR
            if (eventInfo.IsQuestLineStart)
            {
                Log($"Questline {eventInfo.QuestLine.DisplayName} start with quest {eventInfo.Quest.DisplayName} and step {eventInfo.Step.DisplayName}");
            }
            else if (eventInfo.IsQuestStart)
            {
                Log($"Quest {eventInfo.Quest.DisplayName}start and step {eventInfo.Step.DisplayName} start");
            }
            else
            {
                Log($"Step {eventInfo.Step.DisplayName} start");
            }
#endif
            OnStepStart?.Invoke(eventInfo);
        }

        protected virtual void ShowStepComplete(QuestEventInfo eventInfo)
        {
#if UNITY_EDITOR
            Log($"Step {eventInfo.Step.DisplayName} completed !");
#endif
            OnStepComplete?.Invoke(eventInfo);
        }

        protected virtual void ShowQuestComplete(QuestEventInfo eventInfo)
        {
#if UNITY_EDITOR
            Log($"Quest ${eventInfo.Quest.DisplayName} completed by completing {eventInfo.Step.DisplayName}");
#endif
            OnQuestComplete?.Invoke(eventInfo);
        }

        protected virtual void ShowQuestLineComplete(QuestEventInfo eventInfo)
        {
#if UNITY_EDITOR
            Log($"Quest Line {eventInfo.QuestLine.DisplayName} completed by completing step {eventInfo.Step.DisplayName} of Quest {eventInfo.Quest.DisplayName}");
#endif
            OnQuestLineComplete?.Invoke(eventInfo);
        }

        protected virtual void ShowStepStartConditionsNotMet(QuestEventInfo eventInfo)
        {
#if UNITY_EDITOR
            Log($"Step {eventInfo.Step.DisplayName} has {eventInfo.Conditions.Count} unmet start conditions !");
#endif
            OnStepStartConditionsNotMet?.Invoke(eventInfo);
        }

        protected virtual void ShowStepCompleteConditionsNotMet(QuestEventInfo eventInfo)
        {
#if UNITY_EDITOR
            Log($"Step {eventInfo.Step.DisplayName} has {eventInfo.Conditions.Count} unmet complete conditions !");
#endif
            OnStepCompleteConditionsNotMet?.Invoke(eventInfo);
        }

#if UNITY_EDITOR
        [SerializeField] private bool _logMessages = true;
        void Log(object msg)
        {
            if (_logMessages)
            {
                Debug.Log(msg);
            }
        }
#endif
    }
}
