using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    [Header("Outros botões do menu")]
    public Button botaoContinuar;
    public Button botaoOpcoes;
    public Button botaoSair;

    [Header("Painel de Opções")]
    [Tooltip("CanvasGroup do painel de opções (começa escondido)")]
    public CanvasGroup painelOpcoes;
    public Button botaoVoltarOpcoes;
    [Tooltip("Primeiro item a ser selecionado ao abrir as opções (o slider de volume)")]
    public Selectable primeiroItemOpcoes;

    [Header("Saída")]
    public float duracaoFadeSaida = 0.6f;

    [Header("Telefone")]
    public VisorTelefone visor;

    [Header("Caixa postal")]
    public AudioClip clique;
    public float pausaAposClique = 0.4f;

    [Header("Digitação da legenda")]
    public bool efeitoDigitacao = true;
    public float segundosPorLetra = 0.06f;  // velocidade (menor = mais rápido)
    public float pausaPontuacao = 0.18f;    // segundos extras depois de , . ! ? : ;
    public float pausaAposFrase = 0.6f;  // segundos de espera depois de terminar de digitar
    public AudioClip somDigitar;            // opcional: um "tique" curto
    [Range(0f, 1f)] public float volumeDigitar = 0.3f;

    [Header("Áudio")]
    public AudioSource fonte;
    public AudioClip toque;
    public AudioClip mensagem;
    public AudioClip sequenciaCarro;

    [Header("Tempos (segundos)")]
    public float fadeBotao = 0.8f;
    public float tempoToque = 4f;
    public float pausaAntesMensagem = 0.5f;
    public float fadePainel = 0.6f;
    public float duracaoSemAudio = 5f;
    public float pausaAposMensagem = 1.5f;
    public float duracaoFade = 1.5f;
    public float pausaAntesDoSom = 2f;   // segundos de tela preta antes do som das chaves

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

        if (botaoContinuar)
        {
            botaoContinuar.onClick.AddListener(Continuar);
            // Só dá para continuar se existe um jogo salvo e a cena ainda existe no Build
            botaoContinuar.interactable = PodeContinuar();
        }

        if (botaoOpcoes) botaoOpcoes.onClick.AddListener(AbrirOpcoes);
        if (botaoVoltarOpcoes) botaoVoltarOpcoes.onClick.AddListener(FecharOpcoes);
        if (botaoSair) botaoSair.onClick.AddListener(Sair);

        MostrarGrupo(painelOpcoes, false);
    }

    void Update()
    {
        // Esc fecha o painel de opções
        if (!iniciou && painelOpcoes != null && painelOpcoes.interactable
            && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            FecharOpcoes();
        }
    }

    // ---------- NOVO JOGO ----------

    void Jogar()
    {
        if (iniciou) return;
        iniciou = true;

        // Novo jogo começa do zero: apaga puzzles, itens e a última cena salva
        EstadoDoJogo.LimparTudo();

        StartCoroutine(Sequencia());
    }

    // ---------- CONTINUAR ----------

    bool PodeContinuar()
    {
        return EstadoDoJogo.TemJogoSalvo && Application.CanStreamedLevelBeLoaded(EstadoDoJogo.UltimaCena);
    }

    void Continuar()
    {
        if (iniciou || !PodeContinuar()) return;
        iniciou = true;
        StartCoroutine(ContinuarRoutine());
    }

    IEnumerator ContinuarRoutine()
    {
        TravarBotoes();
        yield return Fade(fade, 0, 1, duracaoFade);
        SceneManager.LoadScene(EstadoDoJogo.UltimaCena);
    }

    // ---------- OPÇÕES ----------

    void AbrirOpcoes()
    {
        if (iniciou || painelOpcoes == null) return;

        MostrarGrupo(grupoMenu, false);
        MostrarGrupo(painelOpcoes, true);
        Selectable primeiro = primeiroItemOpcoes != null
            ? primeiroItemOpcoes
            : painelOpcoes.GetComponentInChildren<Selectable>();
        Selecionar(primeiro);
    }

    void FecharOpcoes()
    {
        if (painelOpcoes == null) return;

        MostrarGrupo(painelOpcoes, false);
        MostrarGrupo(grupoMenu, true);
        Selecionar(botaoOpcoes);
    }

    // ---------- SAIR ----------

    void Sair()
    {
        if (iniciou) return;
        iniciou = true;
        StartCoroutine(SairRoutine());
    }

    IEnumerator SairRoutine()
    {
        TravarBotoes();
        yield return Fade(fade, 0, 1, duracaoFadeSaida);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- AUXILIARES ----------

    void TravarBotoes()
    {
        if (botaoJogar) botaoJogar.interactable = false;
        if (botaoContinuar) botaoContinuar.interactable = false;
        if (botaoOpcoes) botaoOpcoes.interactable = false;
        if (botaoSair) botaoSair.interactable = false;
    }

    static void MostrarGrupo(CanvasGroup g, bool mostrar)
    {
        if (g == null) return;
        g.alpha = mostrar ? 1f : 0f;
        g.interactable = mostrar;
        g.blocksRaycasts = mostrar;
    }

    static void Selecionar(Selectable s)
    {
        if (s != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    // ---------- SEQUÊNCIA DO NOVO JOGO (telefone) ----------

    IEnumerator Sequencia()
    {
        TravarBotoes();
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

        if (clique)
        {
            fonte.PlayOneShot(clique);
            yield return new WaitForSeconds(pausaAposClique);
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

        if (clique) fonte.PlayOneShot(clique);

        if (visor) visor.Apagar();
        if (painelDialogo) StartCoroutine(Fade(painelDialogo, 1, 0, fadePainel));

        yield return Fade(fade, 0, 1, duracaoFade);

        var op = SceneManager.LoadSceneAsync(cenaJogo);
        op.allowSceneActivation = false;

        yield return new WaitForSeconds(pausaAntesDoSom);   // NOVO: silêncio na tela preta

        if (sequenciaCarro)
        {
            fonte.Stop();
            fonte.PlayOneShot(sequenciaCarro);
            yield return new WaitForSeconds(sequenciaCarro.length);
        }

        while (op.progress < 0.9f) yield return null; 
        op.allowSceneActivation = true;
    }

    IEnumerator Legendas()
    {
        float t0 = Time.time;

        foreach (var l in legendas)
        {
            while (Time.time - t0 < l.tempo) yield return null;
            if (!legenda) continue;

            yield return StartCoroutine(Digitar(l.texto));   // espera terminar de digitar
            yield return new WaitForSeconds(pausaAposFrase); // segura a frase na tela
        }
    }

    IEnumerator Digitar(string texto)
    {
        legenda.text = texto;

        if (!efeitoDigitacao)
        {
            legenda.maxVisibleCharacters = 99999;
            yield break;
        }

        legenda.maxVisibleCharacters = 0;
        legenda.ForceMeshUpdate();
        int total = legenda.textInfo.characterCount;

        for (int i = 1; i <= total; i++)
        {
            legenda.maxVisibleCharacters = i;
            char c = legenda.textInfo.characterInfo[i - 1].character;

            if (somDigitar && fonte && !char.IsWhiteSpace(c) && i % 2 == 0)
                fonte.PlayOneShot(somDigitar, volumeDigitar);

            float espera = segundosPorLetra;
            if (".,!?;:".IndexOf(c) >= 0) espera += pausaPontuacao;
            yield return new WaitForSeconds(espera);
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