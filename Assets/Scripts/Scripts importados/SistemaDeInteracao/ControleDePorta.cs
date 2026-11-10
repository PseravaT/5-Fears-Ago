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

    // Esta é a função que o seu 'Interagivel' vai chamar agora
    public void Interagir()
    {
        if (emTransicao) return; // Impede que o jogador clique várias vezes e quebre o jogo durante o fade

        // PASSO 1: O jogador clicou a primeira vez -> Mostra o texto
        if (!lendoMensagem)
        {
            lendoMensagem = true;

            // Se estiver trancada, toca o som da maçaneta forçada / tranca
            if (estaTrancada && somPortaTrancada != null)
            {
                TocarSomTrancada();
            }
            
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
        // PASSO 2: O jogador fechou o texto -> Decide se entra ou não
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
        if (!string.IsNullOrEmpty(textoQuandoDestrancada))
        {
            textoInteracao = textoQuandoDestrancada;
        }
        Debug.Log("A porta foi destrancada com sucesso!");
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