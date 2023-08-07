using System.Collections.Generic;

namespace Slax.QuestSystem
{
    /// <summary>
    /// Actual class that will be converted to JSON to be saved
    /// </summary>
    [System.Serializable]
    public class QuestSaveData
    {
        /// A list of references of quest steps with whether it has been completed or not
        public List<string> DoneQuestSteps = new List<string>();
    }

    [System.Serializable]
    public class SaveData
    {
        public List<SavedStep> Steps = new List<SavedStep>();
    }

    [System.Serializable]
    public struct SavedStep
    {
        public string Step;
        public StepState State;

        public SavedStep(string step, StepState state)
        {
            this.Step = step;
            this.State = state;
        }
    }
}
