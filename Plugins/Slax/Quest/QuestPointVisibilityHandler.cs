using UnityEngine;
using Slax.QuestSystem;
using UnityEngine.UI;

public class QuestPointVisibilityHandler : MonoBehaviour
{
    [SerializeField] private QuestStepEnabler _enabler;
    [SerializeField] private Button _btn;

    void OnEnable()
    {
        _enabler.OnEnableChange += HandleState;
    }

    void OnDisable()
    {
        _enabler.OnEnableChange -= HandleState;
    }

    void HandleState(bool state)
    {
        _btn.interactable = state;
    }
}
