namespace Slax.QuestSystem
{
    /// <summary>
    /// Interface for quest rewards.
    /// Implement this interface to create a reward for a quest step or quest.
    /// </summary>
    public interface IQuestReward
    {
        void GrantReward();
    }
}