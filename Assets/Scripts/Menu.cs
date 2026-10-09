using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("UI")]
    public Button botaoJogar;
    public CanvasGroup grupoMenu;
    public CanvasGroup painelDialogo;
    public CanvasGroup fade;
    public TMP_Text legenda;

    [Header("Telefone")]
    public VisorTelefone visor;

    [Header("Áudio")]
    public AudioSource fonte;
    public AudioClip toque;
    public AudioClip mensagem;

    [Header("Tempos (segundos)")]
    public float fadeBotao = 0.8f;
    public float tempoToque = 4f;
    public float pausaAntesMensagem = 0.5f;
    public float fadePainel = 0.6f;
    public float duracaoSemAudio = 5f;
    public float pausaAposMensagem = 1.5f;
    public float duracaoFade = 1.5f;

    [Header("Cena")]
    public string cenaJogo = "atual";

    [System.Serializable]
    public class Linha { public float tempo; [TextArea] public string texto; }
    public Linha[] legendas;

    bool iniciou;

    void Start()
    {
        fade.alpha = 0;
        fade.blocksRaycasts = false;
        if (painelDialogo) { painelDialogo.alpha = 0; painelDialogo.blocksRaycasts = false; }
        if (legenda) legenda.text = "";
        botaoJogar.onClick.AddListener(Jogar);
    }

    void Jogar()
    {
        if (iniciou) return;
        iniciou = true;
        StartCoroutine(Sequencia());
    }

    IEnumerator Sequencia()
    {
        botaoJogar.interactable = false;
        yield return Fade(grupoMenu, 1, 0, fadeBotao);
        grupoMenu.blocksRaycasts = false;

        if (visor) visor.Chamando();

        if (toque != null)
        {
            fonte.clip = toque;
            fonte.loop = true;
            fonte.Play();
            yield return new WaitForSeconds(tempoToque);
            fonte.Stop();
            fonte.loop = false;
        }

        yield return new WaitForSeconds(pausaAntesMensagem);

        if (visor) visor.NovaMensagem();

        if (mensagem != null)
        {
            fonte.clip = mensagem;
            fonte.Play();
        }

        if (painelDialogo) StartCoroutine(Fade(painelDialogo, 0, 1, fadePainel));
        StartCoroutine(Legendas());

        float duracao = mensagem != null ? mensagem.length : duracaoSemAudio;
        yield return new WaitForSeconds(duracao + pausaAposMensagem);

        if (visor) visor.Apagar();
        if (painelDialogo) StartCoroutine(Fade(painelDialogo, 1, 0, fadePainel));

        fade.blocksRaycasts = true;
        yield return Fade(fade, 0, 1, duracaoFade);

        yield return SceneManager.LoadSceneAsync(cenaJogo);
    }

    IEnumerator Legendas()
    {
        float t0 = Time.time;
        foreach (var l in legendas)
        {
            while (Time.time - t0 < l.tempo) yield return null;
            if (legenda) legenda.text = l.texto;
        }
    }

    IEnumerator Fade(CanvasGroup g, float de, float para, float dur)
    {
        float t = 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(de, para, t / dur);
            yield return null;
        }
        g.alpha = para;
    }
}