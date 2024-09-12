using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Slax.QuestSystem
{
    public class QuestManagerEventHub : MonoBehaviour
    {
        public UnityEvent<List<QuestEventInfo>> OnMissingRequirements;
        public UnityEvent<QuestEventInfo> OnStepStart;
        public UnityEvent<QuestEventInfo> OnStepComplete;
        public UnityEvent<QuestEventInfo> OnQuestComplete;
        public UnityEvent<QuestEventInfo> OnQuestLineComplete;

        protected virtual void OnEnable()
        {
            QuestManager.Instance.OnMissingRequirements += ShowMissingRequirements;
            QuestManager.Instance.OnStepStart += ShowStepStart;
            QuestManager.Instance.OnStepComplete += ShowStepComplete;
            QuestManager.Instance.OnQuestComplete += ShowQuestComplete;
            QuestManager.Instance.OnQuestLineComplete += ShowQuestLineComplete;
        }

        protected virtual void OnDisable()
        {
            QuestManager.Instance.OnMissingRequirements -= ShowMissingRequirements;
            QuestManager.Instance.OnStepStart -= ShowStepStart;
            QuestManager.Instance.OnStepComplete -= ShowStepComplete;
            QuestManager.Instance.OnQuestComplete -= ShowQuestComplete;
            QuestManager.Instance.OnQuestLineComplete -= ShowQuestLineComplete;
        }

        protected virtual void ShowMissingRequirements(List<QuestEventInfo> requirements)
        {
            Log($"Missing {requirements.Count} requirements before starting this step !");
            foreach (QuestEventInfo req in requirements)
            {
                Log($"{req.Step.DisplayName} from quest {req.Quest.DisplayName} is a required step.");
            }

            OnMissingRequirements?.Invoke(requirements);
        }

        protected virtual void ShowStepStart(QuestEventInfo eventInfo)
        {
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

            OnStepStart?.Invoke(eventInfo);
        }

        protected virtual void ShowStepComplete(QuestEventInfo eventInfo)
        {
            Log($"Step {eventInfo.Step.DisplayName} completed !");

            OnStepComplete?.Invoke(eventInfo);
        }

        protected virtual void ShowQuestComplete(QuestEventInfo eventInfo)
        {
            Log($"Quest ${eventInfo.Quest.DisplayName} completed by completing {eventInfo.Step.DisplayName}");

            OnQuestComplete?.Invoke(eventInfo);
        }

        protected virtual void ShowQuestLineComplete(QuestEventInfo eventInfo)
        {
            Log($"Quest Line {eventInfo.QuestLine.DisplayName} completed by completing step {eventInfo.Step.DisplayName} of Quest {eventInfo.Quest.DisplayName}");

            OnQuestLineComplete?.Invoke(eventInfo);
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
