using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CarregadorDeTransicao : MonoBehaviour
{
    [Header("Tempo de Animação")]
    public float tempoDeEspera = 2.5f;

    void Start()
    {
        StartCoroutine(CarregarProximaCena());
    }

    private IEnumerator CarregarProximaCena()
    {
        yield return new WaitForSeconds(tempoDeEspera);

        if (!string.IsNullOrEmpty(DadosDeTransicao.proximaCena))
        {
            // TRAVA DE SEGURANÇA: Só tenta fazer o Fade se o objeto do Fade existir
            if (EfeitoFade.Instancia != null)
            {
                EfeitoFade.Instancia.MudarCenaComFade(DadosDeTransicao.proximaCena);
            }
            else
            {
                Debug.LogWarning("Sistema de Fade ausente. Carregando a cena diretamente!");
                SceneManager.LoadScene(DadosDeTransicao.proximaCena);
            }
        }
        else
        {
            Debug.LogError("O nome da próxima cena está vazio!");
        }
    }
}