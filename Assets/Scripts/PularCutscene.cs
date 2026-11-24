using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PularCutscene : MonoBehaviour
{
    [Tooltip("Segure a tecla por este tempo para pular (0 = pula na hora)")]
    [SerializeField] private float segundosParaPular = 0.8f;
    [SerializeField] private Key tecla = Key.Space;
    [SerializeField] private GameObject indicadorUI;   // "segure Espaço para pular"
    public UnityEvent aoPular;

    private float segurado;
    private bool pulou;

    void Update()
    {
        if (pulou || Keyboard.current == null) return;

        if (Keyboard.current[tecla].isPressed)
        {
            segurado += Time.unscaledDeltaTime;
            if (segurado >= segundosParaPular)
            {
                pulou = true;
                aoPular?.Invoke();
            }
        }
        else segurado = 0f;
    }
}