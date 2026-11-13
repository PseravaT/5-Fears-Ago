using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EstadoDoJogo
{
    private const string Prefixo = "puzzle_";
    private const string ChaveLista = "puzzles_ids"; // guarda os IDs salvos, para poder limpar só os nossos

    private static readonly HashSet<string> puzzlesConcluidos = new HashSet<string>();

    public static bool PuzzleConcluido(string id)
        => puzzlesConcluidos.Contains(id) || PlayerPrefs.GetInt(Prefixo + id, 0) == 1;

    public static void MarcarPuzzleConcluido(string id)
    {
        puzzlesConcluidos.Add(id);
        PlayerPrefs.SetInt(Prefixo + id, 1);

        // registra o ID na lista (sem duplicar)
        var ids = new HashSet<string>(PlayerPrefs.GetString(ChaveLista, "").Split(',', System.StringSplitOptions.RemoveEmptyEntries));
        if (ids.Add(id))
            PlayerPrefs.SetString(ChaveLista, string.Join(",", ids));

        PlayerPrefs.Save();
    }

    /// <summary>Apaga todo o progresso de puzzles (não mexe em outros PlayerPrefs).</summary>
    public static void LimparTudo()
    {
        puzzlesConcluidos.Clear();

        foreach (var id in PlayerPrefs.GetString(ChaveLista, "").Split(',', System.StringSplitOptions.RemoveEmptyEntries))
            PlayerPrefs.DeleteKey(Prefixo + id);

        PlayerPrefs.DeleteKey(ChaveLista);
        PlayerPrefs.Save();
        Debug.Log("[EstadoDoJogo] Progresso dos puzzles apagado.");
    }

#if UNITY_EDITOR
    private const string MenuLimparAoPlay = "Tools/Puzzles/Limpar progresso ao dar Play";
    private const string ChaveEditor = "LimparPuzzlesAoPlay";

    // Reseta o HashSet (caso o Domain Reload esteja desligado) e limpa o progresso se o toggle estiver ligado
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AoIniciarPlay()
    {
        puzzlesConcluidos.Clear();
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