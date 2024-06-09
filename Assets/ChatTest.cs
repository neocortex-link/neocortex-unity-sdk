using UnityEngine;
using UnityEngine.UI;

public class ChatTest : MonoBehaviour
{
    [SerializeField] private InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Text outputText;

    private NeocortexSmartAgent agent;
    
    private void Start()
    {
        agent = GetComponent<NeocortexSmartAgent>();
        submitButton.onClick.AddListener(Submit);
    }
    
    private async void Submit()
    {
        outputText.text += $"You: {inputField.text}\n";
        submitButton.interactable = false;
        string response = await agent.Send(inputField.text);
        submitButton.interactable = true;
        outputText.text += $"Agent: {response}\n";
    }
}
