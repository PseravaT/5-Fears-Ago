using UnityEngine;
using UnityEngine.SceneManagement;

public class ControleDePorta : MonoBehaviour
{
    [Header("Status da Porta")]
    public bool estaTrancada = false; // Marque isso no Inspector se a porta não puder ser aberta
    [TextArea(2, 4)]
    public string textoInteracao = "Parece que deixaram a porta aberta..."; // O texto que aparece no primeiro clique
    [Tooltip("Texto opcional quando a porta acabou de ser destrancada")]
    [TextArea(2, 4)]
    public string textoQuandoDestrancada = "A porta se destrancou!";

    [Header("Configurações de Transição")]
    public string nomeDaProximaSala; 
    public string nomeDaCenaDeTransicao; 
    public string nomeDoPontoDeSpawnNoDestino;

    [Header("Persistência")]
    [Tooltip("ID do puzzle que destranca esta porta (o mesmo idUnico do PuzzleIngredientes). Deixe vazio se a porta não depende de puzzle.")]
    [SerializeField] private string idPuzzleQueDestranca = "caldeirao";
    [Tooltip("Texto mostrado quando a porta já está aberta (ao voltar para a cena depois do puzzle). Vazio = vai direto para a próxima cena.")]
    [TextArea(2, 4)]
    public string textoPortaAberta = "";

    [Header("Efeitos Sonoros (Opcional)")]
    [Tooltip("Som de tranca/maçaneta emperrada tocado ao interagir com a porta trancada")]
    public AudioClip somPortaTrancada;
    [Range(0f, 1f)] public float volumeSomTrancada = 1f;
    [Tooltip("Opcional: AudioSource específico. Se vazio, o script cria ou usa o do próprio objeto")]
    public AudioSource audioSource;

    [Header("Referências")]
    public GerenciadorDeDialogo gerenciadorDeDialogo;

    private bool lendoMensagem = false;
    private bool emTransicao = false;

    void Awake()
    {
        if (!string.IsNullOrEmpty(idPuzzleQueDestranca) && EstadoDoJogo.PuzzleConcluido(idPuzzleQueDestranca))
        {
            estaTrancada = false;
            textoInteracao = textoPortaAberta;
        }
    }

    // Esta é a função que o seu 'Interagivel' vai chamar agora
    public void Interagir()
    {
        if (emTransicao) return;

        if (estaTrancada && !lendoMensagem
            && !string.IsNullOrEmpty(idPuzzleQueDestranca)
            && EstadoDoJogo.PuzzleConcluido(idPuzzleQueDestranca))
        {
            estaTrancada = false;
            textoInteracao = textoPortaAberta;
        }

        // PASSO 1: primeiro clique
        if (!lendoMensagem)
        {
            // Porta aberta e sem texto: vai direto para a transição
            if (!estaTrancada && string.IsNullOrEmpty(textoInteracao))
            {
                lendoMensagem = true;
                FinalizarLeituraDaPorta();
                return;
            }

            lendoMensagem = true;

            if (estaTrancada && somPortaTrancada != null)
                TocarSomTrancada();

            if (gerenciadorDeDialogo != null)
            {
                gerenciadorDeDialogo.aoFecharDialogo.RemoveListener(FinalizarLeituraDaPorta);
                gerenciadorDeDialogo.aoFecharDialogo.AddListener(FinalizarLeituraDaPorta);
                gerenciadorDeDialogo.MostrarTexto(textoInteracao);
            }
            else
            {
                Debug.LogWarning("Faltou arrastar o GerenciadorDeDialogo para a porta!");
            }
        }
        // PASSO 2: segundo clique, com o texto aberto
        else
        {
            FinalizarLeituraDaPorta();
        }
    }

    private void FinalizarLeituraDaPorta()
    {
        if (!lendoMensagem) return;

        lendoMensagem = false;

        if (gerenciadorDeDialogo != null)
        {
            gerenciadorDeDialogo.aoFecharDialogo.RemoveListener(FinalizarLeituraDaPorta);
            if (gerenciadorDeDialogo.DialogoEstaAtivo)
            {
                gerenciadorDeDialogo.EsconderTexto();
            }
        }

        // Se não estiver trancada, faz a viagem
        if (!estaTrancada)
        {
            emTransicao = true;
            Debug.Log("Iniciando transição da porta...");

            DadosDeTransicao.proximaCena = nomeDaProximaSala;
            DadosDeTransicao.pontoDeSpawnAlvo = nomeDoPontoDeSpawnNoDestino;

            // Usa o sistema de Fade se ele existir na cena
            if (EfeitoFade.Instancia != null)
            {
                EfeitoFade.Instancia.MudarCenaComFade(nomeDaCenaDeTransicao);
            }
            else
            {
                SceneManager.LoadScene(nomeDaCenaDeTransicao);
            }
        }
        // Se estiver trancada, o texto simplesmente some e o jogador continua jogando normalmente
        else
        {
            Debug.Log("A porta está trancada. Nenhuma transição ocorrerá.");
        }
    }

    /// <summary>
    /// Destranca a porta (pode ser chamado no evento AoConcluirPuzzle do PuzzleIngredientes).
    /// </summary>
    public void DestrancarPorta()
    {
        estaTrancada = false;
        textoInteracao = textoPortaAberta; // interações futuras: sem texto

        if (!string.IsNullOrEmpty(textoQuandoDestrancada) && gerenciadorDeDialogo != null)
            StartCoroutine(MostrarTextoDestrancada());

        Debug.Log("A porta foi destrancada!");
    }

    private System.Collections.IEnumerator MostrarTextoDestrancada()
    {
        yield return new WaitForSeconds(0.3f);
        gerenciadorDeDialogo.MostrarTexto(textoQuandoDestrancada);
    }

    public void TrancarPorta()
    {
        estaTrancada = true;
    }

    private void TocarSomTrancada()
    {
        if (somPortaTrancada == null) return;

        if (audioSource == null)
        {
            if (!TryGetComponent<AudioSource>(out audioSource))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        audioSource.PlayOneShot(somPortaTrancada, volumeSomTrancada);
    }
}