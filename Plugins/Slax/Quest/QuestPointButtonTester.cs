using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestPointButtonTester : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _questEventText;

    public void StepAlreadyValidated() => _questEventText.text = "Step already validated direct callback event";
    public void StepComplete() => _questEventText.text = "Step Complete direct callback event";
}
