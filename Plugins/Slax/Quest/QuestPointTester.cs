using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slax.QuestSystem;

public class QuestPointTester : MonoBehaviour
{
    [SerializeField] private QuestPoint _questPoint;

    private void OnEnable()
    {
        QuestManager.Instance.OnStepComplete += OnQuestManagerEvent;
        QuestManager.Instance.OnQuestComplete += OnQuestManagerEvent;
        QuestManager.Instance.OnQuestLineComplete += OnQuestManagerEvent;
        // QuestManager.Instance.OnStepComplete += HandleStepCompleteEvent;
        // QuestManager.Instance.OnQuestComplete += HandleQuestCompleteEvent;
        // QuestManager.Instance.OnQuestLineComplete += HandleQuestLineCompleteEvent;
    }

    private void OnDisable()
    {
        QuestManager.Instance.OnStepComplete -= OnQuestManagerEvent;
        QuestManager.Instance.OnQuestComplete -= OnQuestManagerEvent;
        QuestManager.Instance.OnQuestLineComplete -= OnQuestManagerEvent;
    }

    private void Start()
    {
        _questPoint.CompleteStep();
    }

    public void OnQuestAlreadyCompleted(QuestStepSO step)
    {
        Debug.Log($"Quest Step {step.name} has already been completed");
    }

    public void OnQuestStepCompleted(QuestStepSO step)
    {
        Debug.Log($"Direct event from QuestPoint {step.name}");
    }

    public void OnQuestManagerEvent(QuestEventInfo eventInfo)
    {
        string stepCompleted = eventInfo.Step.Completed.ToString();
        string questCompleted = eventInfo.Quest.Completed.ToString();
        string questLineCompleted = eventInfo.QuestLine.Completed.ToString();

        Debug.Log($"Questline {eventInfo.QuestLine.name} completed: {questLineCompleted} \n Quest {eventInfo.Quest.name} completed: {questCompleted} \n Step {eventInfo.Step.name} completed: {stepCompleted}");
    }
}
