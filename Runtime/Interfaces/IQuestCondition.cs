namespace Slax.QuestSystem
{
    /// <summary>
    /// Interface for quest conditions.
    /// Implement this interface to create a condition for a quest step or quest.
    /// </summary>
    public interface IQuestCondition
    {
        bool CanStart();
        bool CanComplete();
    }
}