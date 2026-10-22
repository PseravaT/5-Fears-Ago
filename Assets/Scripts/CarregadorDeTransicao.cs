using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CarregadorDeTransicao : MonoBehaviour
{
    [Header("Tempo de Animação")]
    public float tempoDeEspera = 2.5f; // Tempo que a tela da porta fica aparecendo

    void Start()
    {
        // Assim que a cena da porta carrega, ele começa a contar o tempo
        StartCoroutine(CarregarProximaCena());
    }

    private IEnumerator CarregarProximaCena()
    {
        yield return new WaitForSeconds(tempoDeEspera);

        if (!string.IsNullOrEmpty(DadosDeTransicao.proximaCena))
        {
            // NOVA LINHA: Substitui o SceneManager.LoadScene
            EfeitoFade.Instancia.MudarCenaComFade(DadosDeTransicao.proximaCena);
        }
        else
        {
            Debug.LogError("O nome da próxima cena está vazio!");
        }
    }
}