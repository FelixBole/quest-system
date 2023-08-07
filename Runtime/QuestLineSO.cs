using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Slax.QuestSystem
{
    /// <summary>A Quest Line is a series of quests put together that should be done in succession.</summary>
    [CreateAssetMenu(menuName = "Slax/QuestSystem/QuestLine", fileName = "QL0")]
    [System.Serializable]
    public class QuestLineSO : ScriptableObject
    {
        [Tooltip("The name of the Quest Line")]
        [SerializeField] private string _name = "QL0";
        [Tooltip("List of quests")]
        [SerializeField] private List<QuestSO> _quests = new List<QuestSO>();
        [SerializeField] private Texture2D _sprite;
        public UnityAction<QuestLineSO, QuestSO, QuestStepSO> OnCompleted = delegate { };
        public UnityAction<QuestLineSO, QuestSO, QuestStepSO> OnProgress = delegate { };
        private bool _completed = false;
        public string DisplayName => _name;
        public List<QuestSO> Quests => _quests;
        public bool Completed => _completed;
        public Texture2D Sprite => _sprite;

        /// <summary>
        /// Should take in some QuestLine data from the save system or other and setup the completion state
        /// OR SHOULD TAKE IN STRING JSON DATA AND CONVERT IT
        /// </summary>
        public void Initialize()
        {
            _completed = AllQuestsCompleted(_quests);

            if (_completed) return;

            foreach (QuestSO quest in _quests)
            {
                if (!quest.Completed)
                {
                    quest.OnCompleted += HandleQuestCompletedEvent;

                    // Setup step completion events
                    quest.Initialize();
                }
            }
        }

        public int GetTotalSteps()
        {
            int total = 0;
            foreach (QuestSO quest in _quests)
            {
                total += quest.Steps.Count;
            }
            return total;
        }

        /// <summary>Checks if all quests in the questline are completed</summary>
        private bool AllQuestsCompleted()
        {
            return !_quests.Find((QuestSO quest) => quest.Completed == false);
        }

        private bool AllQuestsCompleted(List<QuestSO> quests)
        {
            return !quests.Find((QuestSO quest) => quest.Completed == false);
        }

        /// <summary>Handles the event fired by a quest when it's completed</summary>
        private void HandleQuestCompletedEvent(QuestSO quest, QuestStepSO step)
        {
            quest.OnCompleted -= HandleQuestCompletedEvent;
            if (AllQuestsCompleted()) OnCompleted.Invoke(this, quest, step);
            else OnProgress.Invoke(this, quest, step);
        }
    }
}