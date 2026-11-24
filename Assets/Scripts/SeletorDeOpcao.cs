using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Seletor estilo "‹ valor ›". NÃO vai na pasta Editor.
/// Teclado/controle: esquerda e direita trocam o valor. Mouse: clicar no lado esquerdo volta,
/// no lado direito (ou no meio) avança. Dá a volta ao chegar no fim da lista.
/// </summary>
public class SeletorDeOpcao : Selectable, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private TMP_Text textoValor;

    private string[] opcoes = new string[0];
    private int indice;

    /// <summary>Disparado só quando o jogador troca o valor (não quando o código define).</summary>
    public event Action<int> AoMudar;

    public int Indice => indice;

    /// <summary>Define a lista e o item mostrado, sem disparar AoMudar.</summary>
    public void DefinirOpcoes(IList<string> lista, int indiceAtual)
    {
        opcoes = new string[lista.Count];
        lista.CopyTo(opcoes, 0);
        DefinirIndice(indiceAtual);
    }

    /// <summary>Muda o item mostrado sem disparar AoMudar.</summary>
    public void DefinirIndice(int novo)
    {
        indice = opcoes.Length == 0 ? 0 : Mathf.Clamp(novo, 0, opcoes.Length - 1);
        Atualizar();
    }

    private void Mudar(int passo)
    {
        if (opcoes.Length < 2) return;

        indice = (indice + passo + opcoes.Length) % opcoes.Length;
        Atualizar();
        AoMudar?.Invoke(indice);
    }

    private void Atualizar()
    {
        if (textoValor != null)
            textoValor.text = opcoes.Length > 0 ? opcoes[indice] : "-";
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (IsActive() && IsInteractable())
        {
            if (eventData.moveDir == MoveDirection.Left)  { Mudar(-1); eventData.Use(); return; }
            if (eventData.moveDir == MoveDirection.Right) { Mudar(1);  eventData.Use(); return; }
        }

        base.OnMove(eventData);
    }

    /// <summary>Confirmar (E ou Enter) avança para a próxima opção.</summary>
    public void OnSubmit(BaseEventData eventData)
    {
        if (IsActive() && IsInteractable()) Mudar(1);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable()) return;

        var rect = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        float fracao = Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, local.x);
        Mudar(fracao < 0.25f ? -1 : 1);
    }
}