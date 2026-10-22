using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EfeitoFade : MonoBehaviour
{
    public static EfeitoFade Instancia; // Permite que outros scripts achem o Fade facilmente

    [Header("Configurações")]
    [SerializeField] private Image imagemEscura;
    [SerializeField] private float velocidadeFade = 1f;

    void Awake()
    {
        // Garante que só exista UM sistema de fade no jogo inteiro
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject); // Torna este objeto imortal entre as cenas!
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        // Avisa o script para rodar o FadeIn sempre que uma nova cena terminar de carregar
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
    }

    private void AoCarregarCena(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeIn());
    }

    // Outros scripts vão chamar esta função para trocar de cena
    public void MudarCenaComFade(string nomeDaProximaCena)
    {
        StartCoroutine(FadeOut(nomeDaProximaCena));
    }

    private IEnumerator FadeIn()
    {
        imagemEscura.gameObject.SetActive(true);
        float tempo = 0;
        Color cor = imagemEscura.color;
        cor.a = 1f; // Começa 100% preto

        while (tempo < velocidadeFade)
        {
            tempo += Time.deltaTime;
            cor.a = Mathf.Lerp(1f, 0f, tempo / velocidadeFade); // Vai clareando
            imagemEscura.color = cor;
            yield return null;
        }
        
        imagemEscura.gameObject.SetActive(false); // Desativa para não bloquear seus cliques
    }

    private IEnumerator FadeOut(string nomeDaProximaCena)
    {
        imagemEscura.gameObject.SetActive(true);
        float tempo = 0;
        Color cor = imagemEscura.color;
        cor.a = 0f; // Começa transparente

        while (tempo < velocidadeFade)
        {
            tempo += Time.deltaTime;
            cor.a = Mathf.Lerp(0f, 1f, tempo / velocidadeFade); // Vai escurecendo
            imagemEscura.color = cor;
            yield return null;
        }

        // Quando a tela estiver 100% preta, carrega a próxima cena
        SceneManager.LoadScene(nomeDaProximaCena);
    }
}