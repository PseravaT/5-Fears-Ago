using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EstadoDoJogo
{
    private const string Prefixo = "puzzle_";
    private const string ChaveLista = "puzzles_ids"; // guarda os IDs salvos, para poder limpar só os nossos
    private const string ChaveItens = "inventario_ids"; // IDs dos itens, na ordem em que foram coletados

    private static readonly HashSet<string> puzzlesConcluidos = new HashSet<string>();

    private static readonly List<string> itens = new List<string>();
    private static bool itensCarregados;

    /// <summary>Disparado sempre que um item entra ou sai do inventário (ou quando tudo é limpo).</summary>
    public static event Action AoMudarInventario;

    // ---------- PUZZLES ----------

    public static bool PuzzleConcluido(string id)
        => puzzlesConcluidos.Contains(id) || PlayerPrefs.GetInt(Prefixo + id, 0) == 1;

    public static void MarcarPuzzleConcluido(string id)
    {
        puzzlesConcluidos.Add(id);
        PlayerPrefs.SetInt(Prefixo + id, 1);

        // registra o ID na lista (sem duplicar)
        var ids = new HashSet<string>(PlayerPrefs.GetString(ChaveLista, "").Split(',', StringSplitOptions.RemoveEmptyEntries));
        if (ids.Add(id))
            PlayerPrefs.SetString(ChaveLista, string.Join(",", ids));

        PlayerPrefs.Save();
    }

    // ---------- INVENTÁRIO ----------

    /// <summary>IDs dos itens coletados, na ordem de coleta.</summary>
    public static IReadOnlyList<string> ItensColetados
    {
        get
        {
            GarantirItensCarregados();
            return itens;
        }
    }

    public static bool TemItem(string id)
    {
        GarantirItensCarregados();
        return itens.Contains(id);
    }

    /// <summary>Adiciona o item. Retorna false se o ID for inválido ou o item já estiver no inventário.</summary>
    public static bool AdicionarItem(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains(",")) return false;

        GarantirItensCarregados();
        if (itens.Contains(id)) return false;

        itens.Add(id);
        SalvarItens();
        AoMudarInventario?.Invoke();
        return true;
    }

    /// <summary>Remove o item (por exemplo, ao usá-lo). Retorna false se ele não estava no inventário.</summary>
    public static bool RemoverItem(string id)
    {
        GarantirItensCarregados();
        if (!itens.Remove(id)) return false;

        SalvarItens();
        AoMudarInventario?.Invoke();
        return true;
    }

    private static void GarantirItensCarregados()
    {
        if (itensCarregados) return;
        itensCarregados = true;

        itens.Clear();
        foreach (var id in PlayerPrefs.GetString(ChaveItens, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!itens.Contains(id)) itens.Add(id);
        }
    }

    private static void SalvarItens()
    {
        PlayerPrefs.SetString(ChaveItens, string.Join(",", itens));
        PlayerPrefs.Save();
    }

    // ---------- LIMPEZA ----------

    /// <summary>Apaga todo o progresso (puzzles e inventário). Não mexe em outros PlayerPrefs.</summary>
    public static void LimparTudo()
    {
        puzzlesConcluidos.Clear();

        foreach (var id in PlayerPrefs.GetString(ChaveLista, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            PlayerPrefs.DeleteKey(Prefixo + id);

        PlayerPrefs.DeleteKey(ChaveLista);

        itens.Clear();
        itensCarregados = true;
        PlayerPrefs.DeleteKey(ChaveItens);

        PlayerPrefs.Save();
        AoMudarInventario?.Invoke();
        Debug.Log("[EstadoDoJogo] Progresso dos puzzles e inventário apagados.");
    }

#if UNITY_EDITOR
    private const string MenuLimparAoPlay = "Tools/Puzzles/Limpar progresso ao dar Play";
    private const string ChaveEditor = "LimparPuzzlesAoPlay";

    // Reseta o estado em memória (caso o Domain Reload esteja desligado) e limpa o progresso se o toggle estiver ligado
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AoIniciarPlay()
    {
        puzzlesConcluidos.Clear();
        itens.Clear();
        itensCarregados = false;
        AoMudarInventario = null;

        if (EditorPrefs.GetBool(ChaveEditor, true))
            LimparTudo();
    }

    [MenuItem(MenuLimparAoPlay)]
    private static void ToggleLimparAoPlay()
        => EditorPrefs.SetBool(ChaveEditor, !EditorPrefs.GetBool(ChaveEditor, true));

    [MenuItem(MenuLimparAoPlay, true)]
    private static bool ToggleLimparAoPlayValidate()
    {
        UnityEditor.Menu.SetChecked(MenuLimparAoPlay, EditorPrefs.GetBool(ChaveEditor, true));
        return true;
    }

    [MenuItem("Tools/Puzzles/Limpar progresso agora")]
    private static void LimparAgora() => LimparTudo();
#endif
}