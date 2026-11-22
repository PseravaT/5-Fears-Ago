using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fica na linha inteira do painel de opções (NÃO na pasta Editor).
/// O mouse passando em qualquer ponto da linha seleciona o controle dela (slider ou toggle),
/// e clicar na linha alterna o toggle.
/// </summary>
public class LinhaDeOpcao : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private Selectable alvo;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (alvo == null || !alvo.IsInteractable() || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(alvo.gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (alvo is Toggle toggle && toggle.IsInteractable())
            toggle.isOn = !toggle.isOn;
    }
}