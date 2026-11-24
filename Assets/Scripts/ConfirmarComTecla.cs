using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Faz uma tecla (por padrão E) funcionar como "confirmar" nos menus: aciona o item que está selecionado,
/// igual ao Enter. Botões são clicados, e o SeletorDeOpcao avança para a próxima opção.
/// Chame Verificar() uma vez por frame, de dentro do Update de um menu que esteja aberto.
/// </summary>
public static class ConfirmarComTecla
{
    /// <summary>Tecla de confirmação. Troque aqui se quiser outra.</summary>
    public static Key tecla = Key.E;

    public static void Verificar()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null || !teclado[tecla].wasPressedThisFrame) return;

        EventSystem es = EventSystem.current;
        if (es == null || es.currentSelectedGameObject == null) return;

        // Mesmo evento que o Enter dispara: Button.onClick, SeletorDeOpcao, etc.
        ExecuteEvents.Execute(es.currentSelectedGameObject, new BaseEventData(es), ExecuteEvents.submitHandler);
    }
}
