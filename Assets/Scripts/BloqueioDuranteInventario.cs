using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coloque no Jogador (ou em qualquer objeto) de CADA cena que tenha controles.
/// Enquanto o inventário estiver aberto, os scripts da lista ficam desligados;
/// ao fechar, voltam ao normal. Só reativa o que ele mesmo desligou.
/// </summary>
public class BloqueioDuranteInventario : MonoBehaviour
{
    [Tooltip("Scripts que devem ficar desligados com o inventário aberto: movimento, câmera/mouse, interação, etc.")]
    [SerializeField] private Behaviour[] scriptsParaDesativar;

    private readonly List<Behaviour> desligadosPorMim = new List<Behaviour>();

    void OnEnable()
    {
        GerenciadorDeInventario.AoMudarEstado += AoMudarEstado;

        // Se a cena carregou com o inventário aberto, já começa bloqueado
        if (GerenciadorDeInventario.InventarioAberto) Bloquear();
    }

    void OnDisable()
    {
        GerenciadorDeInventario.AoMudarEstado -= AoMudarEstado;
        Liberar();
    }

    private void AoMudarEstado(bool aberto)
    {
        if (aberto) Bloquear();
        else Liberar();
    }

    private void Bloquear()
    {
        if (scriptsParaDesativar == null) return;

        foreach (Behaviour script in scriptsParaDesativar)
        {
            if (script == null || script == this || !script.enabled) continue;

            script.enabled = false;
            desligadosPorMim.Add(script);
        }
    }

    private void Liberar()
    {
        foreach (Behaviour script in desligadosPorMim)
        {
            if (script != null) script.enabled = true;
        }
        desligadosPorMim.Clear();
    }
}
