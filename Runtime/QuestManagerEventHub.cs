using System.Collections;
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

        private void OnEnable()
        {
            QuestManager.Instance.OnMissingRequirements += ShowMissingRequirements;
            QuestManager.Instance.OnStepStart += ShowStepStart;
            QuestManager.Instance.OnStepComplete += ShowStepComplete;
            QuestManager.Instance.OnQuestComplete += ShowQuestComplete;
            QuestManager.Instance.OnQuestLineComplete += ShowQuestLineComplete;
        }

        private void OnDisable()
        {
            QuestManager.Instance.OnMissingRequirements -= ShowMissingRequirements;
            QuestManager.Instance.OnStepStart -= ShowStepStart;
            QuestManager.Instance.OnStepComplete -= ShowStepComplete;
            QuestManager.Instance.OnQuestComplete -= ShowQuestComplete;
            QuestManager.Instance.OnQuestLineComplete -= ShowQuestLineComplete;
        }

        private void ShowMissingRequirements(List<QuestEventInfo> requirements)
        {
            Debug.Log($"Missing {requirements.Count} requirements before starting this step !");
            foreach (QuestEventInfo req in requirements)
            {
                Debug.Log($"{req.Step.DisplayName} from quest {req.Quest.DisplayName} is a required step.");
            }

            OnMissingRequirements?.Invoke(requirements);
        }

        private void ShowStepStart(QuestEventInfo eventInfo)
        {
            if (eventInfo.IsQuestLineStart)
            {
                Debug.Log($"Questline {eventInfo.QuestLine.DisplayName} start with quest {eventInfo.Quest.DisplayName} and step {eventInfo.Step.DisplayName}");
            }
            else if (eventInfo.IsQuestStart)
            {
                Debug.Log($"Quest {eventInfo.Quest.DisplayName}start and step {eventInfo.Step.DisplayName} start");
            }
            else
            {
                Debug.Log($"Step {eventInfo.Step.DisplayName} start");
            }

            OnStepStart?.Invoke(eventInfo);
        }

        private void ShowStepComplete(QuestEventInfo eventInfo)
        {
            Debug.Log($"Step {eventInfo.Step.DisplayName} completed !");

            OnStepComplete?.Invoke(eventInfo);
        }

        private void ShowQuestComplete(QuestEventInfo eventInfo)
        {
            Debug.Log($"Quest ${eventInfo.Quest.DisplayName} completed by completing {eventInfo.Step.DisplayName}");

            OnQuestComplete?.Invoke(eventInfo);
        }

        private void ShowQuestLineComplete(QuestEventInfo eventInfo)
        {
            Debug.Log($"Quest Line {eventInfo.QuestLine.DisplayName} completed by completing step {eventInfo.Step.DisplayName} of Quest {eventInfo.Quest.DisplayName}");

            OnQuestLineComplete?.Invoke(eventInfo);
        }
    }
}
