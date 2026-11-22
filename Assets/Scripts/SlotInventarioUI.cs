using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Vai no prefab do slot: um Button com uma Image (ícone) e um TextMeshPro (nome).</summary>
public class SlotInventarioUI : MonoBehaviour
{
    [SerializeField] private Button botao;
    [SerializeField] private Image icone;
    [SerializeField] private TMPro.TextMeshProUGUI textoNome;

    public void Configurar(ItemData item, Action<ItemData> aoClicar)
    {
        if (icone != null)
        {
            icone.sprite = item.icone;
            icone.enabled = item.icone != null;
        }

        if (textoNome != null) textoNome.text = item.Nome;

        if (botao != null)
        {
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(() => aoClicar?.Invoke(item));
        }
    }
}
