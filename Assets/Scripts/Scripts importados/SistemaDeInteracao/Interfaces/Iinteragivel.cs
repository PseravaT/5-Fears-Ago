using UnityEngine;

public interface Iinteragivel
{
Transform transform { get; }

string DisplayName { get; }
bool CanInteract ();
void Interact ();
void OnFocus ();
void OnLoseFocus ();

}