using UnityEngine;
using UnityEngine.Events;

public class Interagivel: MonoBehaviour, Iinteragivel
{
    [SerializeField] private string displayName = "interaja";
    [SerializeField] private bool isEnabled = true;
    [SerializeField] private UnityEvent onInteract;

    public string DisplayName => displayName;

    public bool CanInteract() => isEnabled;

    public void Interact()
    {
        onInteract?.Invoke();
    }

    public void OnFocus() { }

    public void OnLoseFocus() { }

    public void SetEnabled(bool value) => isEnabled = value;

}