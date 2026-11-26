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

    [Header("Painel de Slots (novo jogo / continuar)")]
    [Tooltip("CanvasGroup do painel dos slots (começa escondido). Se ficar vazio, o novo jogo começa direto no slot 1")]
    public CanvasGroup painelSlots;
    [Tooltip("Um botão por slot, na ordem: slot 1, slot 2, slot 3")]
    public Button[] botoesSlot;
    public Button botaoVoltarSlots;
    [Tooltip("Opcional: título do painel, que alterna entre NOVO JOGO e CONTINUAR")]
    public TMP_Text tituloSlots;
    [Tooltip("Segundos para clicar de novo e confirmar que vai sobrescrever um slot")]
    public float segundosParaConfirmar = 3f;

    [Header("Saída")]
    public float duracaoFadeSaida = 0.6f;

    [Header("Telefone")]
    public VisorTelefone visor;

    [Header("Caixa postal")]
    public AudioClip clique;
    [Range(0f, 1f)] public float volumeClique = 1f;
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
    [Range(0f, 1f)] public float volumeToque = 1f;
    public AudioClip mensagem;
    [Range(0f, 1f)] public float volumeMensagem = 1f;
    public AudioClip sequenciaCarro;
    [Range(0f, 1f)] public float volumeSequenciaCarro = 1f;

    [Header("Tempos (segundos)")]
    public float fadeBotao = 0.8f;
    public float tempoToque = 4f;
    public float pausaAntesMensagem = 0.5f;
    public float fadePainel = 0.6f;
    public float duracaoSemAudio = 5f;
    public float pausaAposMensagem = 1.5f;
    public float duracaoFade = 1.5f;
    public float pausaAntesDoSom = 2f;   // segundos de tela preta antes do som das chaves

    [Header("Pular (só vale na sequência do Novo Jogo)")]
    public Key teclaPular = Key.Space;
    [Tooltip("Cena carregada quando o jogador pula. Se ficar vazio, usa a 'Cena Jogo'.")]
    public string cenaAoPular = "";
    [Tooltip("Tempo segurando a tecla para pular (0 = pula na hora)")]
    public float segundosParaPular = 0.8f;
    [Tooltip("Fade para preto ao pular")]
    public float duracaoFadePular = 0.5f;
    [Tooltip("Dica na tela. Se ficar vazio, o script cria uma sozinho (texto + barra) no canto inferior direito. Só aparece no Novo Jogo.")]
    public GameObject indicadorPular;
    [Tooltip("Texto da dica (só vale quando o indicador é criado automaticamente)")]
    public string textoIndicador = "segure ESPAÇO para pular";

    [Header("Cena")]
    public string cenaJogo = "atual";

    [System.Serializable]
    public class Linha { public float tempo; [TextArea] public string texto; }
    public Linha[] legendas;

    bool iniciou;

    // Slots
    bool modoNovoJogo;                 // true: o painel de slots está escolhendo onde começar; false: escolhendo qual carregar
    int slotAguardandoConfirmacao;     // 0 = nenhum
    Coroutine rotinaConfirmacaoSlot;

    // Pular
    bool podePular;
    bool aguardandoSoltar;
    float segurado;
    AsyncOperation opCena;
    RectTransform barraPular;   // barra que enche enquanto a tecla é segurada

    // Cena para onde o pulo leva (cenaAoPular, ou cenaJogo se estiver vazio)
    string DestinoPular
    {
        get { return string.IsNullOrEmpty(cenaAoPular) ? cenaJogo : cenaAoPular; }
    }

    // true se pular leva para a mesma cena do fim normal da sequência
    bool PularVaiParaCenaJogo
    {
        get { return DestinoPular == cenaJogo; }
    }

    void Start()
    {
        fade.alpha = 0;
        fade.blocksRaycasts = false;
        if (painelDialogo) { painelDialogo.alpha = 0; painelDialogo.blocksRaycasts = false; }
        if (legenda) legenda.text = "";
        if (indicadorPular == null) CriarIndicadorPular();
        if (indicadorPular) indicadorPular.SetActive(false);

        botaoJogar.onClick.AddListener(Jogar);

        if (botaoContinuar)
        {
            botaoContinuar.onClick.AddListener(Continuar);
            // Só dá para continuar se algum slot tem jogo salvo e a cena ainda existe no Build
            botaoContinuar.interactable = PodeContinuar();
        }

        if (botaoOpcoes) botaoOpcoes.onClick.AddListener(AbrirOpcoes);
        if (botaoVoltarOpcoes) botaoVoltarOpcoes.onClick.AddListener(FecharOpcoes);
        if (botaoSair) botaoSair.onClick.AddListener(Sair);

        if (botoesSlot != null)
        {
            for (int i = 0; i < botoesSlot.Length; i++)
            {
                if (botoesSlot[i] == null) continue;

                int slot = i + 1; // cópia local para o lambda
                botoesSlot[i].onClick.AddListener(() => EscolherSlot(slot));
            }
        }
        if (botaoVoltarSlots) botaoVoltarSlots.onClick.AddListener(FecharSlots);

        MostrarGrupo(painelOpcoes, false);
        MostrarGrupo(painelSlots, false);
    }

    void Update()
    {
        // Esc fecha o painel de opções ou o de slots
        if (!iniciou && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (painelOpcoes != null && painelOpcoes.interactable) FecharOpcoes();
            else if (painelSlots != null && painelSlots.interactable) FecharSlots();
        }

        // E confirma o item selecionado (igual ao Enter), em qualquer painel do menu
        if (!iniciou) ConfirmarComTecla.Verificar();

        VerificarPular();
    }

    // ---------- PULAR (só no Novo Jogo) ----------

    void VerificarPular()
    {
        if (!podePular || Keyboard.current == null) return;

        var tecla = Keyboard.current[teclaPular];

        // Se o jogador ativou o botão com a mesma tecla, espera soltar antes de contar
        if (aguardandoSoltar)
        {
            if (!tecla.isPressed) aguardandoSoltar = false;
            return;
        }

        if (tecla.isPressed)
        {
            segurado += Time.unscaledDeltaTime;
            AtualizarBarraPular();
            if (segurado >= segundosParaPular) PularSequencia();
        }
        else
        {
            segurado = 0f;
            AtualizarBarraPular();
        }
    }

    void AtualizarBarraPular()
    {
        if (barraPular == null) return;
        float p = segundosParaPular <= 0f ? 1f : Mathf.Clamp01(segurado / segundosParaPular);
        barraPular.anchorMax = new Vector2(p, 1f);
    }

    // Cria a dica "segure ESPAÇO para pular" com uma barra de progresso, no canto inferior direito
    void CriarIndicadorPular()
    {
        Canvas canvas = fade != null ? fade.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return;

        // Raiz
        var raiz = new GameObject("IndicadorPular", typeof(RectTransform), typeof(CanvasGroup));
        raiz.transform.SetParent(canvas.transform, false);
        var rtRaiz = (RectTransform)raiz.transform;
        rtRaiz.anchorMin = rtRaiz.anchorMax = new Vector2(1f, 0f);
        rtRaiz.pivot = new Vector2(1f, 0f);
        rtRaiz.anchoredPosition = new Vector2(-40f, 40f);
        rtRaiz.sizeDelta = new Vector2(560f, 64f);
        var cg = raiz.GetComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Texto (usa a mesma fonte da legenda, para combinar com o jogo)
        var goTexto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        goTexto.transform.SetParent(raiz.transform, false);
        var rtTexto = (RectTransform)goTexto.transform;
        rtTexto.anchorMin = Vector2.zero;
        rtTexto.anchorMax = Vector2.one;
        rtTexto.offsetMin = new Vector2(0f, 14f);
        rtTexto.offsetMax = Vector2.zero;
        var txt = goTexto.GetComponent<TextMeshProUGUI>();
        txt.text = textoIndicador;
        txt.fontSize = 30f;
        txt.alignment = TextAlignmentOptions.BottomRight;
        txt.color = Color.white;
        txt.raycastTarget = false;
        if (legenda != null && legenda.font != null) txt.font = legenda.font;

        // Fundo da barra
        var goFundo = new GameObject("FundoBarra", typeof(RectTransform), typeof(Image));
        goFundo.transform.SetParent(raiz.transform, false);
        var rtFundo = (RectTransform)goFundo.transform;
        rtFundo.anchorMin = new Vector2(0f, 0f);
        rtFundo.anchorMax = new Vector2(1f, 0f);
        rtFundo.pivot = new Vector2(0.5f, 0f);
        rtFundo.anchoredPosition = Vector2.zero;
        rtFundo.sizeDelta = new Vector2(0f, 6f);
        var imgFundo = goFundo.GetComponent<Image>();
        imgFundo.color = new Color(1f, 1f, 1f, 0.25f);
        imgFundo.raycastTarget = false;

        // Preenchimento (cresce da esquerda para a direita)
        var goBarra = new GameObject("Barra", typeof(RectTransform), typeof(Image));
        goBarra.transform.SetParent(goFundo.transform, false);
        barraPular = (RectTransform)goBarra.transform;
        barraPular.anchorMin = Vector2.zero;
        barraPular.anchorMax = new Vector2(0f, 1f);
        barraPular.offsetMin = Vector2.zero;
        barraPular.offsetMax = Vector2.zero;
        var imgBarra = goBarra.GetComponent<Image>();
        imgBarra.color = Color.white;
        imgBarra.raycastTarget = false;

        raiz.transform.SetAsLastSibling();
        indicadorPular = raiz;
    }

    void PularSequencia()
    {
        podePular = false;
        StopAllCoroutines();               // para a sequência, legendas, digitação e fades
        StartCoroutine(PularRoutine());
    }

    IEnumerator PularRoutine()
    {
        if (indicadorPular) indicadorPular.SetActive(false);

        if (fonte) fonte.Stop();           // toque, mensagem, tique e som do carro
        if (visor) visor.Apagar();
        if (legenda) legenda.text = "";

        grupoMenu.alpha = 0f;
        grupoMenu.blocksRaycasts = false;

        // O fade do painel de slots pode ter sido interrompido no meio: garante que ele fica escondido
        if (painelSlots != null)
        {
            painelSlots.alpha = 0f;
            painelSlots.blocksRaycasts = false;
        }

        if (painelDialogo) StartCoroutine(Fade(painelDialogo, painelDialogo.alpha, 0f, duracaoFadePular));

        yield return Fade(fade, fade.alpha, 1f, duracaoFadePular);

        if (opCena != null && PularVaiParaCenaJogo)
        {
            // A cena normal já estava carregando em segundo plano, só libera
            opCena.allowSceneActivation = true;
        }
        else
        {
            // Cena escolhida em "Cena Ao Pular" (nada foi pré-carregado nesse caso)
            SceneManager.LoadScene(DestinoPular);
        }
    }

    // ---------- NOVO JOGO ----------

    void Jogar()
    {
        if (iniciou) return;

        // Sem painel de slots configurado, começa direto no slot 1 (como era antes)
        if (painelSlots == null || botoesSlot == null || botoesSlot.Length == 0)
        {
            IniciarNovoJogo(1);
            return;
        }

        AbrirSlots(true);
    }

    void IniciarNovoJogo(int slot)
    {
        if (iniciou) return;
        iniciou = true;

        // Novo jogo começa do zero no slot escolhido: apaga puzzles, itens e a última cena salva dele
        EstadoDoJogo.NovoJogoNoSlot(slot);

        StartCoroutine(Sequencia());
    }

    // ---------- CONTINUAR ----------

    /// <summary>Dá para continuar se algum slot tem jogo salvo e a cena dele ainda existe no Build.</summary>
    bool PodeContinuar()
    {
        for (int s = 1; s <= EstadoDoJogo.TotalDeSlots; s++)
        {
            if (SlotPodeCarregar(s)) return true;
        }
        return false;
    }

    static bool SlotPodeCarregar(int slot)
    {
        return EstadoDoJogo.TemJogoNoSlot(slot)
            && Application.CanStreamedLevelBeLoaded(EstadoDoJogo.UltimaCenaDoSlot(slot));
    }

    void Continuar()
    {
        if (iniciou || !PodeContinuar()) return;

        // Sem painel de slots configurado, usa o slot atual (ou o primeiro que tiver jogo)
        if (painelSlots == null || botoesSlot == null || botoesSlot.Length == 0)
        {
            int slot = SlotPodeCarregar(EstadoDoJogo.SlotAtual) ? EstadoDoJogo.SlotAtual : PrimeiroSlotCarregavel();
            CarregarSlot(slot);
            return;
        }

        AbrirSlots(false);
    }

    static int PrimeiroSlotCarregavel()
    {
        for (int s = 1; s <= EstadoDoJogo.TotalDeSlots; s++)
        {
            if (SlotPodeCarregar(s)) return s;
        }
        return EstadoDoJogo.SlotAtual;
    }

    void CarregarSlot(int slot)
    {
        if (iniciou || !SlotPodeCarregar(slot)) return;
        iniciou = true;

        EstadoDoJogo.SelecionarSlot(slot);
        StartCoroutine(ContinuarRoutine());
    }

    IEnumerator ContinuarRoutine()
    {
        TravarBotoes();
        yield return Fade(fade, 0, 1, duracaoFade);
        SceneManager.LoadScene(EstadoDoJogo.UltimaCena);
    }

    // ---------- SLOTS ----------

    void AbrirSlots(bool novoJogo)
    {
        if (iniciou || painelSlots == null) return;

        modoNovoJogo = novoJogo;
        CancelarConfirmacaoSlot();

        DefinirTituloSlots(novoJogo ? "NOVO JOGO" : "CONTINUAR");
        AtualizarSlots();

        MostrarGrupo(grupoMenu, false);
        MostrarGrupo(painelSlots, true);

        // Seleciona o primeiro slot que dá para usar (no Continuar, o primeiro com jogo salvo)
        Selectable alvo = botaoVoltarSlots;
        foreach (Button b in botoesSlot)
        {
            if (b != null && b.interactable) { alvo = b; break; }
        }
        Selecionar(alvo);
    }

    void FecharSlots()
    {
        if (painelSlots == null) return;

        CancelarConfirmacaoSlot();
        MostrarGrupo(painelSlots, false);
        MostrarGrupo(grupoMenu, true);
        Selecionar(modoNovoJogo ? botaoJogar : (botaoContinuar != null && botaoContinuar.interactable ? botaoContinuar : botaoJogar));
    }

    /// <summary>Atualiza o texto e a disponibilidade de cada botão de slot.</summary>
    void AtualizarSlots()
    {
        for (int i = 0; i < botoesSlot.Length; i++)
        {
            Button b = botoesSlot[i];
            if (b == null) continue;

            int slot = i + 1;
            bool temJogo = EstadoDoJogo.TemJogoNoSlot(slot);

            TMP_Text rotulo = RotuloDe(b);
            if (rotulo != null)
            {
                AjustarRotuloDoSlot(rotulo);

                string conteudo = temJogo ? EstadoDoJogo.DataDoSlot(slot) : "Vazio";
                if (string.IsNullOrEmpty(conteudo)) conteudo = "Jogo salvo";
                rotulo.text = "Slot " + slot + "  -  " + conteudo;
            }

            // Novo jogo: qualquer slot serve. Continuar: só os que têm jogo salvo.
            b.interactable = modoNovoJogo || SlotPodeCarregar(slot);
        }
    }

    /// <summary>Troca o texto do título e de todas as camadas dele (o título tem 2 textos sobrepostos para dar relevo).</summary>
    void DefinirTituloSlots(string texto)
    {
        if (tituloSlots == null) return;

        foreach (TMP_Text t in tituloSlots.GetComponentsInChildren<TMP_Text>(true))
            t.text = texto;
    }

    /// <summary>Deixa o texto do slot encolher até caber, para nunca passar por baixo da seta.</summary>
    static void AjustarRotuloDoSlot(TMP_Text rotulo)
    {
        if (rotulo.enableAutoSizing) return;

        float tamanho = rotulo.fontSize;
        rotulo.enableAutoSizing = true;
        rotulo.fontSizeMax = tamanho;
        rotulo.fontSizeMin = tamanho * 0.5f;
    }

    void EscolherSlot(int slot)
    {
        if (iniciou) return;

        if (!modoNovoJogo)
        {
            CarregarSlot(slot);
            return;
        }

        // Novo jogo em um slot que já tem jogo: pede para clicar de novo para sobrescrever
        if (EstadoDoJogo.TemJogoNoSlot(slot) && slotAguardandoConfirmacao != slot)
        {
            PedirConfirmacaoSlot(slot);
            return;
        }

        CancelarConfirmacaoSlot();
        IniciarNovoJogo(slot);
    }

    void PedirConfirmacaoSlot(int slot)
    {
        CancelarConfirmacaoSlot();
        slotAguardandoConfirmacao = slot;

        TMP_Text rotulo = RotuloDe(botoesSlot[slot - 1]);
        if (rotulo != null) rotulo.text = "Sobrescrever? Clique de novo";

        rotinaConfirmacaoSlot = StartCoroutine(ExpirarConfirmacaoSlot());
    }

    IEnumerator ExpirarConfirmacaoSlot()
    {
        yield return new WaitForSeconds(segundosParaConfirmar);
        rotinaConfirmacaoSlot = null;
        CancelarConfirmacaoSlot();
    }

    void CancelarConfirmacaoSlot()
    {
        if (rotinaConfirmacaoSlot != null)
        {
            StopCoroutine(rotinaConfirmacaoSlot);
            rotinaConfirmacaoSlot = null;
        }

        bool tinha = slotAguardandoConfirmacao != 0;
        slotAguardandoConfirmacao = 0;
        if (tinha && botoesSlot != null) AtualizarSlots(); // volta o texto normal
    }

    /// <summary>Texto do botão: o filho "Texto" (criado pelo criador de painéis) ou o primeiro TMP que achar.</summary>
    static TMP_Text RotuloDe(Button botao)
    {
        if (botao == null) return null;

        Transform t = botao.transform.Find("Texto");
        if (t != null)
        {
            TMP_Text achado = t.GetComponent<TMP_Text>();
            if (achado != null) return achado;
        }

        return botao.GetComponentInChildren<TMP_Text>(true);
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

        if (botoesSlot != null)
        {
            foreach (Button b in botoesSlot)
                if (b != null) b.interactable = false;
        }
        if (botaoVoltarSlots) botaoVoltarSlots.interactable = false;
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

        // A partir daqui o jogador pode pular
        segurado = 0f;
        aguardandoSoltar = true;
        podePular = true;
        AtualizarBarraPular();
        if (indicadorPular) indicadorPular.SetActive(true);

        // O painel visível agora é o dos slots (o menu principal está escondido). Faz o fade dele.
        CanvasGroup painelVisivel = (painelSlots != null && painelSlots.alpha > 0f) ? painelSlots : grupoMenu;
        yield return Fade(painelVisivel, 1, 0, fadeBotao);
        grupoMenu.blocksRaycasts = false;
        if (painelSlots != null) painelSlots.blocksRaycasts = false;

        if (visor) visor.Chamando();

        float volumeBaseFonte = fonte != null ? fonte.volume : 1f;

        if (toque != null)
        {
            fonte.clip = toque;
            fonte.volume = volumeBaseFonte * volumeToque;
            fonte.loop = true;
            fonte.Play();
            yield return new WaitForSeconds(tempoToque);
            fonte.Stop();
            fonte.loop = false;
            fonte.volume = volumeBaseFonte;
        }

        if (clique)
        {
            fonte.PlayOneShot(clique, volumeClique);
            yield return new WaitForSeconds(pausaAposClique);
        }

        yield return new WaitForSeconds(pausaAntesMensagem);

        if (visor) visor.NovaMensagem();

        if (mensagem != null)
        {
            fonte.PlayOneShot(mensagem, volumeMensagem);
        }

        if (painelDialogo) StartCoroutine(Fade(painelDialogo, 0, 1, fadePainel));
        StartCoroutine(Legendas());

        float duracao = mensagem != null ? mensagem.length : duracaoSemAudio;
        yield return new WaitForSeconds(duracao + pausaAposMensagem);

        if (clique) fonte.PlayOneShot(clique, volumeClique);

        if (visor) visor.Apagar();
        if (painelDialogo) StartCoroutine(Fade(painelDialogo, 1, 0, fadePainel));

        // Já vai para a tela preta: some a dica de pular (o pular continua valendo)
        if (indicadorPular) indicadorPular.SetActive(false);

        yield return Fade(fade, 0, 1, duracaoFade);

        // Só pré-carrega a cena normal se o pulo também vai para ela.
        // Se o pulo vai para outra cena, carregar em segundo plano (com
        // allowSceneActivation = false) poderia travar o LoadScene do pulo.
        if (PularVaiParaCenaJogo)
        {
            opCena = SceneManager.LoadSceneAsync(cenaJogo);
            opCena.allowSceneActivation = false;
        }

        yield return new WaitForSeconds(pausaAntesDoSom);   // silêncio na tela preta

        if (sequenciaCarro)
        {
            fonte.Stop();
            fonte.PlayOneShot(sequenciaCarro, volumeSequenciaCarro);
            yield return new WaitForSeconds(sequenciaCarro.length);
        }

        podePular = false;

        if (opCena == null)
        {
            // Não foi pré-carregada: carrega agora
            opCena = SceneManager.LoadSceneAsync(cenaJogo);
            opCena.allowSceneActivation = false;
        }

        while (opCena.progress < 0.9f) yield return null;
        opCena.allowSceneActivation = true;
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