using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace Slax.QuestSystem
{
    /// <summary>A Quest Line is a series of quests put together that should be done in succession.</summary>
    [CreateAssetMenu(menuName = "Slax/QuestSystem/QuestLine", fileName = "QL0")]
    [System.Serializable]
    public class QuestLineSO : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] protected bool _useLocalization = false;

        [Tooltip("Questline info")]
        [SerializeField] protected string _name = "QL0";
        [SerializeField] protected LocalizedString _localizedName;
        [SerializeField] protected string _description = "Quest Line 0";
        [SerializeField] protected LocalizedString _localizedDescription;
        [SerializeField] protected Sprite _sprite;
        [SerializeField] protected LocalizedSprite _localizedSprite;

        [Tooltip("List of quests")]
        [SerializeField] protected List<QuestSO> _quests = new List<QuestSO>();

        [Header("Rewards")]
        [SerializeField] protected List<IQuestReward> _rewards = new List<IQuestReward>();

        /// <summary>
        /// List of rewards for the quest line. If the QuestManager is set to
        /// grant rewards automatically, the QuestManager will grant these rewards
        /// when the quest line is completed. Otherwise, these rewards can still
        /// be accessed on the QuestEventInfo event fired when the quest line is
        /// completed.
        /// </summary>
        public List<IQuestReward> Rewards => _rewards;

        /// <summary>
        /// Event fired when the quest line is completed for the QuestManager to handle.
        /// </summary>
        public UnityAction<QuestLineSO, QuestSO, QuestStepSO> OnCompleted = delegate { };

        /// <summary>
        /// Event fired when the quest line is in progress for the QuestManager to handle.
        /// </summary>
        public UnityAction<QuestLineSO, QuestSO, QuestStepSO> OnProgress = delegate { };
        protected bool _completed = false;
        public string DisplayName => _useLocalization ? _localizedName.GetLocalizedString() : _name;
        public string Description => _useLocalization ? _localizedDescription.GetLocalizedString() : _description;
        public Sprite Sprite => _useLocalization ? _localizedSprite.LoadAsset() : _sprite;
        public List<QuestSO> Quests => _quests;
        public bool Completed => _completed;

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

        public QuestLineSO SetUseLocalization(bool useLocalization)
        {
            _useLocalization = useLocalization;
            return this;
        }

        public QuestLineSO SetName(string name)
        {
            _name = name;
            return this;
        }

        public QuestLineSO SetLocalizedName(LocalizedString localizedName)
        {
            _localizedName = localizedName;
            return this;
        }

        public QuestLineSO SetDescription(string description)
        {
            _description = description;
            return this;
        }

        public QuestLineSO SetLocalizedDescription(LocalizedString localizedDescription)
        {
            _localizedDescription = localizedDescription;
            return this;
        }

        public QuestLineSO SetQuests(List<QuestSO> quests)
        {
            UnsuscribeFromQuests();
            _quests = quests;
            Initialize();
            return this;
        }

        public QuestSO GetQuest(string questName)
        {
            return _quests.Find((QuestSO quest) => quest.DisplayName == questName);
        }

        public QuestSO GetQuest(int index) => _quests[index];

        public int GetQuestIndex(QuestSO quest)
        {
            return _quests.FindIndex((QuestSO q) => q.DisplayName == quest.DisplayName);
        }

        public QuestSO GetFirstQuest() => _quests[0];
        public QuestSO GetLastQuest() => _quests[_quests.Count - 1];

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
        protected bool AllQuestsCompleted()
        {
            return !_quests.Find((QuestSO quest) => quest.Completed == false);
        }

        protected bool AllQuestsCompleted(List<QuestSO> quests)
        {
            return !quests.Find((QuestSO quest) => quest.Completed == false);
        }

        /// <summary>Handles the event fired by a quest when it's completed</summary>
        protected void HandleQuestCompletedEvent(QuestSO quest, QuestStepSO step)
        {
            quest.OnCompleted -= HandleQuestCompletedEvent;
            if (AllQuestsCompleted()) OnCompleted.Invoke(this, quest, step);
            else OnProgress.Invoke(this, quest, step);
        }

        protected void UnsuscribeFromQuests()
        {
            foreach (QuestSO quest in _quests)
            {
                quest.OnCompleted -= HandleQuestCompletedEvent;
            }
        }
    }
}