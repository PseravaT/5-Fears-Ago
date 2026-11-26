using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public class CarregadorDeTransicao : MonoBehaviour
{
    [Header("Tempo de Animação")]
    public float tempoDeEspera = 2.5f;

    [Header("Ação Opcional ao Terminar (ex.: Tela Final)")]
    [Tooltip("Se tiver algum evento configurado aqui, ele é chamado em vez de carregar DadosDeTransicao.proximaCena.")]
    public UnityEvent aoTerminarEspera;

    void Start()
    {
        StartCoroutine(CarregarProximaCena());
    }

    private IEnumerator CarregarProximaCena()
    {
        yield return new WaitForSeconds(tempoDeEspera);

        if (aoTerminarEspera != null && aoTerminarEspera.GetPersistentEventCount() > 0)
        {
            aoTerminarEspera.Invoke();
            yield break;
        }

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