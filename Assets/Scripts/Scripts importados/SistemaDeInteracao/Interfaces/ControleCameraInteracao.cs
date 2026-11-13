using UnityEngine;
using UnityEngine.Events; // Necessário para os eventos visuais
using UnityEngine.InputSystem;

public class ControleCameraInteracao : MonoBehaviour
{
    [Header("Câmera do Objeto")]
    [SerializeField] private GameObject cameraDesteObjeto;

    [Header("O que acontece ao ENTRAR?")]
    public UnityEvent aoEntrarNaVisao;

    [Header("O que acontece ao SAIR?")]
    public UnityEvent aoSairDaVisao;

    private bool estaInteragindo = false;

    public void EntrarNaVisao()
    {
        if (estaInteragindo) return;

        var puzzle = GetComponent<PuzzleIngredientes>();
        if (puzzle != null && puzzle.JaConcluido) return;

        estaInteragindo = true;

        if (cameraDesteObjeto != null)
            cameraDesteObjeto.SetActive(true);

        PuzzleIngredientes puzzleNesteObjeto = GetComponent<PuzzleIngredientes>();

        if (puzzleNesteObjeto != null)
        {
            // Se este objeto tem um PuzzleIngredientes, espera o diálogo terminar para abrir o puzzle
            // (sem sair da visão da câmera ainda!)
            puzzleNesteObjeto.EsperarDialogoEIniciarPuzzle();
        }
        else if (GerenciadorDeDialogo.Instancia != null)
        {
            // Objeto comum sem puzzle: sai da visão da câmera quando o diálogo fechar
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(SairDaVisao);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(SairDaVisao);
        }
        
        // Dispara tudo que você configurar no Inspector para desligar
        aoEntrarNaVisao?.Invoke(); 
    }

    void Update()
    {
        if (estaInteragindo && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SairDaVisao();
        }
    }

    public void SairDaVisao()
    {
        if (!estaInteragindo) return;

        estaInteragindo = false;

        // Se tinha um puzzle esperando o diálogo ou aberto neste objeto, cancela/fecha junto com a saída
        if (TryGetComponent<PuzzleIngredientes>(out var puzzle))
        {
            puzzle.CancelarEsperaDeDialogo();
            if (puzzle.EstaAtivo)
            {
                puzzle.FecharPuzzle(cancelado: true);
            }
        }

        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(SairDaVisao);

            // Se saiu apertando ESC enquanto o diálogo ainda estava aberto, esconde o diálogo também
            if (GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
            {
                GerenciadorDeDialogo.Instancia.EsconderTexto();
            }
        }

        if (cameraDesteObjeto != null)
            cameraDesteObjeto.SetActive(false);
        
        // Dispara tudo que você configurar no Inspector para religar
        aoSairDaVisao?.Invoke();
    }

    void OnDisable()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(SairDaVisao);
        }
    }
}