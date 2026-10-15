using UnityEngine;
using UnityEngine.SceneManagement;

public class ControleDePorta : MonoBehaviour
{
    private bool estaAberta = false;
    
    [Header("Configurações da Porta")]
    public string nomeDaProximaSala; 
    public string nomeDaCenaDeTransicao; 

    public void AbrirPorta()
    {
        if(!estaAberta)
        {
            estaAberta = true;
            Debug.Log("Iniciando transição da porta...");

            // Salva para onde o jogador vai na variável estática
            DadosDeTransicao.proximaCena = nomeDaProximaSala;

            // Carrega a cena de transição
            SceneManager.LoadScene(nomeDaCenaDeTransicao);
        }
    }
}