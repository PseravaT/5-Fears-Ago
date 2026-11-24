using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Progresso do jogo, separado em slots de salvamento (1 a TotalDeSlots).
/// Puzzles, inventário e a última cena valem sempre para o SLOT ATIVO (SlotAtual).
/// O menu escolhe o slot com SelecionarSlot / NovoJogoNoSlot; o resto do jogo continua usando
/// as mesmas funções de antes (PuzzleConcluido, AdicionarItem, UltimaCena...).
/// </summary>
public static class EstadoDoJogo
{
    public const int TotalDeSlots = 3;

    private const string ChaveSlotAtual = "slot_atual"; // último slot usado (vale também entre execuções)

    // Chaves de cada slot: tudo começa com "s<N>_" para um slot nunca misturar com outro
    private static string ChavePuzzle(int slot, string id) => $"s{slot}_puzzle_{id}";
    private static string ChaveLista(int slot) => $"s{slot}_puzzles_ids";      // IDs dos puzzles salvos, para poder limpar só os nossos
    private static string ChaveItens(int slot) => $"s{slot}_inventario_ids";   // IDs dos itens, na ordem em que foram coletados
    private static string ChaveCena(int slot) => $"s{slot}_ultima_cena";       // última cena de jogo visitada (usada pelo "Continuar")
    private static string ChaveData(int slot) => $"s{slot}_data";              // quando foi salvo (texto para mostrar no menu)

    /// <summary>Cenas que NÃO contam como "progresso": o Continuar nunca volta para elas. Ajuste os nomes se precisar.</summary>
    private static readonly string[] CenasQueNaoSalvam = { "Menu", "Cutscene_Menu" };

    // Cache em memória do SLOT ATIVO (é zerado ao trocar de slot)
    private static readonly HashSet<string> puzzlesConcluidos = new HashSet<string>();
    private static readonly List<string> itens = new List<string>();
    private static bool itensCarregados;

    private static int slotAtual; // 0 = ainda não lido do PlayerPrefs

    /// <summary>Disparado sempre que um item entra ou sai do inventário, quando tudo é limpo ou quando o slot muda.</summary>
    public static event Action AoMudarInventario;

    // ---------- SLOTS ----------

    /// <summary>Slot em uso agora (1 a TotalDeSlots). Na primeira vez é o último que foi usado, ou 1.</summary>
    public static int SlotAtual
    {
        get
        {
            if (slotAtual == 0)
                slotAtual = Mathf.Clamp(PlayerPrefs.GetInt(ChaveSlotAtual, 1), 1, TotalDeSlots);
            return slotAtual;
        }
    }

    /// <summary>Passa a usar o slot (sem apagar nada). Usado pelo "Continuar".</summary>
    public static void SelecionarSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, TotalDeSlots);

        bool mudou = slot != SlotAtual;
        slotAtual = slot;
        PlayerPrefs.SetInt(ChaveSlotAtual, slot);

        if (mudou)
        {
            ReiniciarCache();
            AoMudarInventario?.Invoke();
        }

        PlayerPrefs.Save();
    }

    /// <summary>Passa a usar o slot e apaga o que havia nele. Usado pelo "Novo jogo".</summary>
    public static void NovoJogoNoSlot(int slot)
    {
        SelecionarSlot(slot);
        ApagarSlot(slot);
        Debug.Log($"[EstadoDoJogo] Novo jogo no slot {SlotAtual}.");
    }

    /// <summary>Esse slot tem um jogo salvo?</summary>
    public static bool TemJogoNoSlot(int slot) => !string.IsNullOrEmpty(UltimaCenaDoSlot(slot));

    public static string UltimaCenaDoSlot(int slot)
        => PlayerPrefs.GetString(ChaveCena(Mathf.Clamp(slot, 1, TotalDeSlots)), "");

    /// <summary>Data e hora do último salvamento do slot (texto pronto para mostrar). Vazio se não houver.</summary>
    public static string DataDoSlot(int slot)
        => PlayerPrefs.GetString(ChaveData(Mathf.Clamp(slot, 1, TotalDeSlots)), "");

    /// <summary>Algum dos slots tem jogo salvo?</summary>
    public static bool AlgumSlotTemJogo
    {
        get
        {
            for (int s = 1; s <= TotalDeSlots; s++)
                if (TemJogoNoSlot(s)) return true;
            return false;
        }
    }

    /// <summary>Apaga tudo de um slot (puzzles, inventário, última cena e data).</summary>
    public static void ApagarSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, TotalDeSlots);

        foreach (var id in PlayerPrefs.GetString(ChaveLista(slot), "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            PlayerPrefs.DeleteKey(ChavePuzzle(slot, id));

        PlayerPrefs.DeleteKey(ChaveLista(slot));
        PlayerPrefs.DeleteKey(ChaveItens(slot));
        PlayerPrefs.DeleteKey(ChaveCena(slot));
        PlayerPrefs.DeleteKey(ChaveData(slot));
        PlayerPrefs.Save();

        if (slot == SlotAtual)
        {
            ReiniciarCache();
            AoMudarInventario?.Invoke();
        }
    }

    /// <summary>Apaga todos os slots. Não mexe em outros PlayerPrefs (ex.: opções).</summary>
    public static void LimparTodosOsSlots()
    {
        for (int s = 1; s <= TotalDeSlots; s++) ApagarSlot(s);
        Debug.Log("[EstadoDoJogo] Todos os slots apagados.");
    }

    private static void ReiniciarCache()
    {
        puzzlesConcluidos.Clear();
        itens.Clear();
        itensCarregados = false;
    }

    // ---------- PUZZLES ----------

    public static bool PuzzleConcluido(string id)
        => puzzlesConcluidos.Contains(id) || PlayerPrefs.GetInt(ChavePuzzle(SlotAtual, id), 0) == 1;

    public static void MarcarPuzzleConcluido(string id)
    {
        int slot = SlotAtual;

        puzzlesConcluidos.Add(id);
        PlayerPrefs.SetInt(ChavePuzzle(slot, id), 1);

        // registra o ID na lista do slot (sem duplicar)
        var ids = new HashSet<string>(PlayerPrefs.GetString(ChaveLista(slot), "").Split(',', StringSplitOptions.RemoveEmptyEntries));
        if (ids.Add(id))
            PlayerPrefs.SetString(ChaveLista(slot), string.Join(",", ids));

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
        foreach (var id in PlayerPrefs.GetString(ChaveItens(SlotAtual), "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!itens.Contains(id)) itens.Add(id);
        }
    }

    private static void SalvarItens()
    {
        PlayerPrefs.SetString(ChaveItens(SlotAtual), string.Join(",", itens));
        PlayerPrefs.Save();
    }

    // ---------- CONTINUAR (última cena do slot ativo) ----------

    /// <summary>Nome da última cena de jogo visitada no slot ativo. Vazio se não há jogo salvo nele.</summary>
    public static string UltimaCena => UltimaCenaDoSlot(SlotAtual);

    public static bool TemJogoSalvo => TemJogoNoSlot(SlotAtual);

    /// <summary>Grava a cena (e a data/hora) no slot ativo.</summary>
    public static void SalvarCena(string nomeDaCena)
    {
        if (string.IsNullOrEmpty(nomeDaCena)) return;

        int slot = SlotAtual;
        PlayerPrefs.SetString(ChaveCena(slot), nomeDaCena);
        PlayerPrefs.SetString(ChaveData(slot), DateTime.Now.ToString("dd/MM/yy HH:mm"));
        PlayerPrefs.Save();
    }

    // Salva a cena automaticamente toda vez que uma cena de jogo é carregada
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegistrarSalvamentoDeCena()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;

        // a primeira cena já carregou antes de nos registrarmos
        SalvarCenaSeForDeJogo(SceneManager.GetActiveScene().name);
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Single)
            SalvarCenaSeForDeJogo(cena.name);
    }

    private static void SalvarCenaSeForDeJogo(string nomeDaCena)
    {
        foreach (string excluida in CenasQueNaoSalvam)
        {
            if (string.Equals(nomeDaCena, excluida, StringComparison.OrdinalIgnoreCase)) return;
        }

        SalvarCena(nomeDaCena);
    }

    // ---------- LIMPEZA ----------

    /// <summary>Apaga o progresso do SLOT ATIVO (puzzles, inventário e última cena). Para apagar tudo, use LimparTodosOsSlots.</summary>
    public static void LimparTudo()
    {
        ApagarSlot(SlotAtual);
        Debug.Log($"[EstadoDoJogo] Progresso do slot {SlotAtual} apagado (puzzles, inventário e última cena).");
    }

#if UNITY_EDITOR
    private const string MenuLimparAoPlay = "Tools/Puzzles/Limpar progresso ao dar Play";
    private const string ChaveEditor = "LimparPuzzlesAoPlay";

    // Reseta o estado em memória (caso o Domain Reload esteja desligado).
    // Se o toggle estiver ligado, apaga TODOS os slots ao dar Play. Desligue-o para testar o Continuar e os slots no Editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AoIniciarPlay()
    {
        ReiniciarCache();
        AoMudarInventario = null;
        slotAtual = 0;

        if (EditorPrefs.GetBool(ChaveEditor, true))
            LimparTodosOsSlots();
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

    [MenuItem("Tools/Puzzles/Limpar todos os slots agora")]
    private static void LimparAgora() => LimparTodosOsSlots();
#endif
}