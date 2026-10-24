using UnityEngine;
using UnityEngine.SceneManagement;

public class ControleDePorta : MonoBehaviour
{
    [Header("Status da Porta")]
    public bool estaTrancada = false; // Marque isso no Inspector se a porta não puder ser aberta
    [TextArea(2, 4)]
    public string textoInteracao = "Parece que deixaram a porta aberta..."; // O texto que aparece no primeiro clique

    [Header("Configurações de Transição")]
    public string nomeDaProximaSala; 
    public string nomeDaCenaDeTransicao; 
    public string nomeDoPontoDeSpawnNoDestino;

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
            
            if (gerenciadorDeDialogo != null)
            {
                gerenciadorDeDialogo.MostrarTexto(textoInteracao);
            }
            else
            {
                Debug.LogWarning("Faltou arrastar o GerenciadorDeDialogo para a porta!");
            }
        }
        // PASSO 2: O jogador clicou a segunda vez (para fechar o texto) -> Decide se entra ou não
        else
        {
            lendoMensagem = false;
            
            if (gerenciadorDeDialogo != null)
            {
                gerenciadorDeDialogo.EsconderTexto();
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
    }
}