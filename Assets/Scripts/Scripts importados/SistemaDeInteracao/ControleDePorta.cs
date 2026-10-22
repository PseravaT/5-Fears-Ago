using UnityEngine;
using UnityEngine.SceneManagement;

public class ControleDePorta : MonoBehaviour
{
    private bool estaAberta = false;
    
    [Header("Configurações da Porta")]
    public string nomeDaProximaSala; 
    public string nomeDaCenaDeTransicao; 
    
    [Header("Onde o jogador vai nascer?")]
    public string nomeDoPontoDeSpawnNoDestino; // Nome exato do objeto vazio na próxima cena

    public void AbrirPorta()
    {
        if(!estaAberta)
        {
            estaAberta = true;
            Debug.Log("Iniciando transição da porta...");

            // Salva a cena e o ponto de spawn exato
            DadosDeTransicao.proximaCena = nomeDaProximaSala;
            DadosDeTransicao.pontoDeSpawnAlvo = nomeDoPontoDeSpawnNoDestino;

            // Substitua por EfeitoFade.Instancia.MudarCenaComFade(nomeDaCenaDeTransicao); se estiver usando o script de Fade
            SceneManager.LoadScene(nomeDaCenaDeTransicao); 
        }
    }
}