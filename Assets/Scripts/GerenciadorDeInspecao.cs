using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Rendering;

/// <summary>O que a tecla Q faz durante a inspeção.</summary>
public enum ModoTeclaQ
{
    LerDetalhes,
    Focar,
    Nenhum
}

public class GerenciadorDeInspecao : MonoBehaviour
{
    public static GerenciadorDeInspecao Instancia;

    [Header("UI de Leitura (Tecla Q)")]
    public GameObject telaGuiasDeTecla;
    public GameObject painelTextoDetalhado;
    public TMPro.TextMeshProUGUI componenteTextoHistoria;
    public TMPro.TextMeshProUGUI textoBotaoQ;
    [Tooltip("Texto onde aparece 'Página 1 de X'. Crie um TextMeshPro dentro do painelTextoDetalhado e arraste aqui.")]
    public TMPro.TextMeshProUGUI textoContadorPaginas;
    [Tooltip("Se marcado, esconde o contador quando o texto tiver apenas 1 página.")]
    [SerializeField] private bool ocultarContadorSePaginaUnica = true;

    [Header("Textos da tecla Q")]
    [SerializeField] private string textoLerDetalhes = "pressione Q para ler detalhes";
    [SerializeField] private string textoVoltarDaLeitura = "pressione Q para voltar a inspeção";
    [SerializeField] private string textoFocar = "pressione Q para focar";
    [SerializeField] private string textoDesfocar = "pressione Q para sair do foco";

    [Header("Ícone da tecla Q (opcional, substitui o texto)")]
    [SerializeField] private UnityEngine.UI.Image iconeBotaoQ;
    [SerializeField] private Sprite spriteLerDetalhes;
    [SerializeField] private Sprite spriteVoltarDaLeitura;
    [SerializeField] private Sprite spriteFocar;
    [SerializeField] private Sprite spriteDesfocar;

    private bool lendoTelaPreta = false;
    private string[] paginasAtuais = new string[0];
    private int paginaAtual = 0;

    [Header("Configurações na Tela")]
    [Tooltip("Distância inicial que o objeto vai flutuar na frente da câmera")]
    [SerializeField] private float distanciaDaCamera = 1.5f;
    [SerializeField] private float velocidadeDeRotacao = 150f;

    [Tooltip("Guia 'passar página'. Aparece sozinho em qualquer inspeção cujo texto tenha mais de uma página")]
    [SerializeField] private GameObject guiaPassarPagina;
    [Tooltip("Guia 'E para colocar no inventário'. Aparece apenas quando há um item coletável com a tecla E disponível na inspeção.")]
    [SerializeField] private GameObject guiaColetarComE;

    [Header("Zoom (Roda do Mouse)")]
    [SerializeField] private float distanciaMinima = 0.5f;
    [SerializeField] private float distanciaMaxima = 3f;
    [Tooltip("Quanto a distância muda por clique da roda")]
    [SerializeField] private float passoDoZoom = 0.3f;
    [Tooltip("Tempo em segundos para o zoom acompanhar o alvo")]
    [SerializeField] private float suavizacaoDoZoomSegundos = 0.1f;

    [Header("Efeito de Blur (Fundo)")]
    [SerializeField] private Volume volumeDeBlur;

    [Header("Eventos Globais (Inspector)")]
    public UnityEvent aoIniciarInspecao;
    public UnityEvent aoTerminarInspecao;

    private GameObject objetoSendoInspecionado;
    private bool estaInspecionando = false;
    private int frameInicioInspecao = -1;
    private int ultimoFrameFechado = -1;
    private Vector2 posicaoAnteriorDoMouse;
    private Transform cameraTransform;

    private float distanciaAtual;
    private float distanciaAlvo;
    private float velocidadeZoom;

    // Estado inicial, usado pelo reset (tecla R) e pela volta do foco
    private Quaternion rotacaoInicialGuardada;
    private Vector2 deslocamentoXYInicial;
    private float distanciaInicialGuardada;

    private Coroutine rotinaPose;
    private System.Action aoColetarComTeclaE;

    // Tecla Q
    private ModoTeclaQ modoTeclaQ = ModoTeclaQ.LerDetalhes;
    private System.Action<bool> aoAlternarFoco;
    private bool emFoco;

    public bool EstaInspecionando => estaInspecionando;
    public bool FechouNesteFrame => Time.frameCount == ultimoFrameFechado;
    /// <summary>true só no frame em que a inspeção abriu. Use para ignorar o E que abriu a inspeção.</summary>
    public bool IniciouNesteFrame => estaInspecionando && Time.frameCount == frameInicioInspecao;
    public float DistanciaAtualDoObjeto => distanciaAtual;
    public GameObject ObjetoInspecionado => objetoSendoInspecionado;
    public Quaternion RotacaoInicial => rotacaoInicialGuardada;

    /// <summary>true enquanto a Q está no estado "focado" (modo Focar).</summary>
    public bool EmFoco => emFoco;

    /// <summary>Quando true, rotação, zoom e reset ficam bloqueados (usado no foco do painel).</summary>
    public bool ControlesTravados { get; private set; }
    public bool EmTransicao { get; private set; }

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    // Sobrecargas por compatibilidade
    public void IniciarInspecao(GameObject modelo3D)
    {
        IniciarInspecao(modelo3D, Quaternion.identity, "", Vector3.zero, Vector3.one);
    }

    public void IniciarInspecao(GameObject modelo3D, string textoItem)
    {
        IniciarInspecao(modelo3D, Quaternion.identity, textoItem, Vector3.zero, Vector3.one);
    }

    public void IniciarInspecao(GameObject modelo3D, Quaternion rotacaoInicial, string textoItem)
    {
        IniciarInspecao(modelo3D, rotacaoInicial, textoItem, Vector3.zero, Vector3.one);
    }

    public void IniciarInspecao(GameObject modelo3D, Quaternion rotacaoInicial, string textoItem, Vector3 posicaoInicial)
    {
        IniciarInspecao(modelo3D, rotacaoInicial, textoItem, posicaoInicial, Vector3.one);
    }

    /// <param name="escalaInicial">Multiplica a escala original do prefab (1,1,1 = tamanho original)</param>
    public void IniciarInspecao(GameObject modelo3D, Quaternion rotacaoInicial, string textoItem, Vector3 posicaoInicial, Vector3 escalaInicial)
    {
        if (estaInspecionando) return;
        if (modelo3D == null)
        {
            Debug.LogWarning("[GerenciadorDeInspecao] Modelo 3D vazio, a inspeção não foi aberta.", this);
            return;
        }

        cameraTransform = Camera.main.transform;

        // Raiz vazia: o modelo é recentralizado dentro dela, então o pivô (o ponto em volta do qual
        // o objeto gira e é posicionado) fica no centro visual, mesmo que o pivô do prefab esteja fora do mesh
        GameObject raiz = new GameObject("InspecaoPivo");
        raiz.transform.position = Vector3.zero;
        raiz.transform.rotation = Quaternion.identity;

        GameObject clone = Instantiate(modelo3D, raiz.transform);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        clone.SetActive(true);
        LimparScriptsDeCenaDoClone(clone);

        Renderer[] rends = clone.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            clone.transform.localPosition = -b.center;
        }

        objetoSendoInspecionado = raiz;

        // Z da posição inicial soma na distância; X e Y deslocam o objeto na tela.
        distanciaAlvo = Mathf.Clamp(distanciaDaCamera + posicaoInicial.z, distanciaMinima, distanciaMaxima);
        distanciaAtual = distanciaAlvo;
        velocidadeZoom = 0f;

        // Posição e rotação em coordenadas LOCAIS da câmera
        objetoSendoInspecionado.transform.SetParent(cameraTransform, false);
        objetoSendoInspecionado.transform.localPosition = new Vector3(posicaoInicial.x, posicaoInicial.y, distanciaAtual);
        objetoSendoInspecionado.transform.localRotation = rotacaoInicial;

        // Escala: multiplica a escala original pela escala escolhida
        objetoSendoInspecionado.transform.localScale = Vector3.Scale(objetoSendoInspecionado.transform.localScale, escalaInicial);

        // Guarda o estado inicial
        rotacaoInicialGuardada = rotacaoInicial;
        deslocamentoXYInicial = new Vector2(posicaoInicial.x, posicaoInicial.y);
        distanciaInicialGuardada = distanciaAlvo;

        ControlesTravados = false;
        EmTransicao = false;
        aoColetarComTeclaE = null;

        // Tecla Q volta ao padrão (quem quiser outra função chama DefinirTeclaQ depois)
        modoTeclaQ = ModoTeclaQ.LerDetalhes;
        aoAlternarFoco = null;
        emFoco = false;

        foreach (var rb in objetoSendoInspecionado.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
        }

        MudarLayerRecursivo(objetoSendoInspecionado.transform, LayerMask.NameToLayer("Inspecao"));

        if (volumeDeBlur != null) volumeDeBlur.weight = 1f;

        // --- PREPARAÇÃO DA UI ---
        if (componenteTextoHistoria == null && painelTextoDetalhado != null)
        {
            componenteTextoHistoria = painelTextoDetalhado.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        }
        if (textoContadorPaginas == null && painelTextoDetalhado != null)
        {
            foreach (var tmp in painelTextoDetalhado.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                if (tmp != componenteTextoHistoria &&
                    (tmp.name.IndexOf("Pagina", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     tmp.name.IndexOf("Contador", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    textoContadorPaginas = tmp;
                    break;
                }
            }
        }

        ConfigurarPaginasTexto(textoItem);

        if (telaGuiasDeTecla != null) telaGuiasDeTecla.SetActive(true);
        lendoTelaPreta = false;

        if (painelTextoDetalhado != null) painelTextoDetalhado.SetActive(false);
        AtualizarTextoDaTeclaQ();

        if (GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(TerminarInspecao);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(TerminarInspecao);
        }

        aoIniciarInspecao?.Invoke();
        estaInspecionando = true;
        frameInicioInspecao = Time.frameCount;
        AtualizarGuiasCentrais();
    }

    private void LimparScriptsDeCenaDoClone(GameObject clone)
    {
        if (clone == null) return;

        // Se o usuário arrastou um objeto da cena para ModeloDoObjeto, o clone não pode rodar
        // scripts de cena que desativam/movem o objeto ou conflitam com o InteractionDotManager.
        foreach (var comp in clone.GetComponentsInChildren<LiberarAposPuzzle>(true)) { comp.enabled = false; Destroy(comp); }
        foreach (var comp in clone.GetComponentsInChildren<Interagivel>(true)) { comp.enabled = false; Destroy(comp); }
        foreach (var comp in clone.GetComponentsInChildren<ItemParaInspecionar>(true)) { comp.enabled = false; Destroy(comp); }
        foreach (var comp in clone.GetComponentsInChildren<ItemColetavel>(true)) { comp.enabled = false; Destroy(comp); }
        foreach (var comp in clone.GetComponentsInChildren<BoiarNoLiquido>(true)) { comp.enabled = false; Destroy(comp); }
        foreach (var col in clone.GetComponentsInChildren<Collider>(true)) col.enabled = false;
    }

    private void MudarLayerRecursivo(Transform obj, int novaLayer)
    {
        if (novaLayer == -1) return;

        obj.gameObject.layer = novaLayer;
        foreach (Transform filho in obj)
        {
            MudarLayerRecursivo(filho, novaLayer);
        }
    }

    public void ResetarModelo()
    {
        if (objetoSendoInspecionado == null) return;

        objetoSendoInspecionado.transform.localRotation = rotacaoInicialGuardada;

        // O zoom volta suavemente pelo SmoothDamp; X e Y voltam na hora
        distanciaAlvo = distanciaInicialGuardada;
        Vector3 pos = objetoSendoInspecionado.transform.localPosition;
        pos.x = deslocamentoXYInicial.x;
        pos.y = deslocamentoXYInicial.y;
        objetoSendoInspecionado.transform.localPosition = pos;
    }

    /// <summary>Define uma ação opcional executada ao pressionar E (ex.: coletar o item para o inventário). Depois da ação a inspeção fecha. Sem ação, o E não faz nada: só o ESC sai.</summary>
    public void DefinirAcaoAoPressionarE(System.Action acao)
    {
        Debug.Log($"[Inspecao] Acao do E definida: {(acao == null ? "nenhuma" : acao.Method.DeclaringType + "." + acao.Method.Name)}\n{System.Environment.StackTrace}");
        aoColetarComTeclaE = acao;
        AtualizarGuiasCentrais();
    }

    private void AtualizarGuiasCentrais()
    {
        bool lendoComPaginas = estaInspecionando && lendoTelaPreta && paginasAtuais.Length > 1;
        if (guiaPassarPagina != null && guiaPassarPagina.activeSelf != lendoComPaginas)
            guiaPassarPagina.SetActive(lendoComPaginas);

        bool mostrarColeta = estaInspecionando && !lendoTelaPreta && aoColetarComTeclaE != null;
        if (guiaColetarComE != null && guiaColetarComE.activeSelf != mostrarColeta)
            guiaColetarComE.SetActive(mostrarColeta);
    }

    /// <summary>Limpa a ação do E apenas se ela ainda pertencer ao mesmo método/alvo passado.</summary>
    public void LimparAcaoAoPressionarESeFor(System.Action acao)
    {
        if (aoColetarComTeclaE == acao)
            DefinirAcaoAoPressionarE(null);
    }

    /// <summary>
    /// Escolhe o que a tecla Q faz nesta inspeção. Chame logo depois de IniciarInspecao.
    /// No modo Focar, aoAlternarFoco recebe true ao focar e false ao desfocar.
    /// </summary>
    public void DefinirTeclaQ(ModoTeclaQ modo, System.Action<bool> aoAlternarFoco = null)
    {
        modoTeclaQ = modo;
        this.aoAlternarFoco = aoAlternarFoco;
        emFoco = false;

        // Se estava lendo e trocou de modo, fecha o painel de leitura
        if (modo != ModoTeclaQ.LerDetalhes && lendoTelaPreta)
        {
            lendoTelaPreta = false;
            if (painelTextoDetalhado != null) painelTextoDetalhado.SetActive(false);
        }

        AtualizarTextoDaTeclaQ();
        AtualizarGuiasCentrais();
    }

    private void AtualizarTextoDaTeclaQ()
    {
        bool mostrar = modoTeclaQ == ModoTeclaQ.LerDetalhes || modoTeclaQ == ModoTeclaQ.Focar;

        if (textoBotaoQ != null)
        {
            textoBotaoQ.gameObject.SetActive(mostrar);
            if (modoTeclaQ == ModoTeclaQ.LerDetalhes)
                textoBotaoQ.text = lendoTelaPreta ? textoVoltarDaLeitura : textoLerDetalhes;
            else if (modoTeclaQ == ModoTeclaQ.Focar)
                textoBotaoQ.text = emFoco ? textoDesfocar : textoFocar;
        }

        if (iconeBotaoQ != null)
        {
            iconeBotaoQ.gameObject.SetActive(mostrar);
            Sprite alvo = null;
            if (modoTeclaQ == ModoTeclaQ.LerDetalhes)
                alvo = lendoTelaPreta ? spriteVoltarDaLeitura : spriteLerDetalhes;
            else if (modoTeclaQ == ModoTeclaQ.Focar)
                alvo = emFoco ? spriteDesfocar : spriteFocar;

            if (alvo != null)
                iconeBotaoQ.sprite = alvo;
        }
    }

    private void AoApertarQ()
    {
        switch (modoTeclaQ)
        {
            case ModoTeclaQ.LerDetalhes:
                lendoTelaPreta = !lendoTelaPreta;

                if (painelTextoDetalhado != null)
                    painelTextoDetalhado.SetActive(lendoTelaPreta);

                if (lendoTelaPreta)
                    AtualizarExibicaoPagina();
                break;

            case ModoTeclaQ.Focar:
                // Não deixa apertar no meio da animação de pose
                if (EmTransicao) return;

                emFoco = !emFoco;
                aoAlternarFoco?.Invoke(emFoco);
                break;

            default:
                return;
        }

        AtualizarTextoDaTeclaQ();
        AtualizarGuiasCentrais();
    }

    /// <summary>Trava ou libera rotação, zoom e reset (usado enquanto a válvula anima). Ignorado durante uma transição de pose.</summary>
    public void TravarControles(bool valor)
    {
        if (EmTransicao) return;
        ControlesTravados = valor;
    }

    // ---------- POSE (foco no painel) ----------

    /// <summary>Move e gira o modelo suavemente até a pose. Durante e depois (se travarDepois) os controles ficam bloqueados.</summary>
    public void IrParaPose(Quaternion rotacaoLocal, float distancia, Vector2 deslocamentoXY, float duracaoSegundos, bool travarDepois)
    {
        if (objetoSendoInspecionado == null) return;

        if (rotinaPose != null) StopCoroutine(rotinaPose);
        rotinaPose = StartCoroutine(IrParaPoseRoutine(rotacaoLocal, distancia, deslocamentoXY, duracaoSegundos, travarDepois));
    }

    /// <summary>Volta à pose com que a inspeção começou e libera os controles.</summary>
    public void VoltarParaPoseInicial(float duracaoSegundos)
    {
        IrParaPose(rotacaoInicialGuardada, distanciaInicialGuardada, deslocamentoXYInicial, duracaoSegundos, false);
    }

    private IEnumerator IrParaPoseRoutine(Quaternion rotacaoPara, float distancia, Vector2 deslocamentoXY, float duracaoSegundos, bool travarDepois)
    {
        ControlesTravados = true;
        EmTransicao = true;

        Transform t = objetoSendoInspecionado.transform;
        Vector3 posDe = t.localPosition;
        Quaternion rotDe = t.localRotation;
        Vector3 posPara = new Vector3(deslocamentoXY.x, deslocamentoXY.y, distancia);

        float progresso = 0f;
        while (progresso < 1f)
        {
            if (objetoSendoInspecionado == null) yield break;

            progresso += Time.deltaTime / Mathf.Max(0.01f, duracaoSegundos);
            float suave = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progresso));

            t.localPosition = Vector3.Lerp(posDe, posPara, suave);
            t.localRotation = Quaternion.Slerp(rotDe, rotacaoPara, suave);
            distanciaAtual = t.localPosition.z; // a luz de inspeção acompanha
            yield return null;
        }

        t.localPosition = posPara;
        t.localRotation = rotacaoPara;

        distanciaAlvo = distancia;
        distanciaAtual = distancia;
        velocidadeZoom = 0f;

        ControlesTravados = travarDepois;
        EmTransicao = false;
        rotinaPose = null;
    }

    void Update()
    {
        if (!estaInspecionando) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            posicaoAnteriorDoMouse = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.isPressed && objetoSendoInspecionado != null && !lendoTelaPreta && !ControlesTravados)
        {
            Vector2 posicaoAtual = Mouse.current.position.ReadValue();
            Vector2 dif = posicaoAtual - posicaoAnteriorDoMouse;

            float rotX = dif.y;
            float rotY = -dif.x;

            objetoSendoInspecionado.transform.Rotate(cameraTransform.up, rotY * velocidadeDeRotacao * Time.deltaTime, Space.World);
            objetoSendoInspecionado.transform.Rotate(cameraTransform.right, rotX * velocidadeDeRotacao * Time.deltaTime, Space.World);

            posicaoAnteriorDoMouse = posicaoAtual;
        }

        // --- ZOOM ---
        if (objetoSendoInspecionado != null && !lendoTelaPreta && !ControlesTravados)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // No Windows, cada clique da roda vem como 120; normalizamos para 1 por clique
                distanciaAlvo -= (scroll / 120f) * passoDoZoom;
                distanciaAlvo = Mathf.Clamp(distanciaAlvo, distanciaMinima, distanciaMaxima);
            }

            distanciaAtual = Mathf.SmoothDamp(distanciaAtual, distanciaAlvo, ref velocidadeZoom, suavizacaoDoZoomSegundos);

            // Só o Z muda; X e Y (posição inicial) são preservados
            Vector3 pos = objetoSendoInspecionado.transform.localPosition;
            pos.z = distanciaAtual;
            objetoSendoInspecionado.transform.localPosition = pos;
        }

        // --- RESET (R) ---
        if (Keyboard.current.rKey.wasPressedThisFrame && !lendoTelaPreta && !ControlesTravados)
        {
            ResetarModelo();
        }

        // --- TECLA Q (função depende do modo escolhido) ---
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            AoApertarQ();
        }

        // --- NAVEGAÇÃO ENTRE PÁGINAS (W / A = Voltar | S / D = Avançar) ---
        if (lendoTelaPreta && paginasAtuais.Length > 1 && Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.aKey.wasPressedThisFrame ||
                Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                MudarPagina(-1);
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame ||
                     Keyboard.current.dKey.wasPressedThisFrame ||
                     Keyboard.current.downArrowKey.wasPressedThisFrame ||
                     Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                MudarPagina(+1);
            }
        }

        if (Keyboard.current != null && Time.frameCount > frameInicioInspecao)
        {
            bool apertouEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
            bool dialogoAtivo = GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo;

            if (apertouEsc)
            {
                // Única forma de sair da inspeção
                TerminarInspecao();
            }
            else if (!dialogoAtivo && !lendoTelaPreta && !ControlesTravados && aoColetarComTeclaE != null
                     && Keyboard.current.eKey.wasPressedThisFrame)
            {
                // O E só serve para executar a ação registrada (ex.: coletar o item). Sem ação, não faz nada.
                var acaoColeta = aoColetarComTeclaE;
                aoColetarComTeclaE = null;

                // Fecha a inspeção atual ANTES, para a ação poder abrir outra (ex.: a do item coletado)
                TerminarInspecao();
                acaoColeta.Invoke();
            }
        }
    }

    public void TerminarInspecao()
    {
        if (!estaInspecionando) return;

        estaInspecionando = false;
        ultimoFrameFechado = Time.frameCount;

        if (rotinaPose != null)
        {
            StopCoroutine(rotinaPose);
            rotinaPose = null;
        }
        ControlesTravados = false;
        EmTransicao = false;
        aoColetarComTeclaE = null;

        modoTeclaQ = ModoTeclaQ.LerDetalhes;
        aoAlternarFoco = null;
        emFoco = false;

        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(TerminarInspecao);

            if (GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
            {
                GerenciadorDeDialogo.Instancia.EsconderTexto();
            }
        }

        if (objetoSendoInspecionado != null)
        {
            Destroy(objetoSendoInspecionado);
        }

        if (volumeDeBlur != null) volumeDeBlur.weight = 0f;

        lendoTelaPreta = false;
        paginasAtuais = new string[0];
        paginaAtual = 0;
        if (painelTextoDetalhado != null) painelTextoDetalhado.SetActive(false);
        if (telaGuiasDeTecla != null) telaGuiasDeTecla.SetActive(false);
        if (guiaPassarPagina != null) guiaPassarPagina.SetActive(false);
        if (guiaColetarComE != null) guiaColetarComE.SetActive(false);

        aoTerminarInspecao?.Invoke();
    }

    private void ConfigurarPaginasTexto(string textoCompleto)
    {
        if (string.IsNullOrEmpty(textoCompleto))
        {
            paginasAtuais = new string[] { "" };
        }
        else
        {
            // Separa páginas usando '|' ou '---'
            string[] bruto = textoCompleto.Contains("|")
                ? textoCompleto.Split('|')
                : textoCompleto.Split(new[] { "---" }, System.StringSplitOptions.None);

            for (int i = 0; i < bruto.Length; i++)
            {
                bruto[i] = bruto[i].Trim();
            }

            paginasAtuais = bruto.Length > 0 ? bruto : new string[] { "" };
        }

        paginaAtual = 0;
        AtualizarExibicaoPagina();
    }

    private void MudarPagina(int delta)
    {
        if (paginasAtuais == null || paginasAtuais.Length <= 1) return;

        int novaPagina = Mathf.Clamp(paginaAtual + delta, 0, paginasAtuais.Length - 1);
        if (novaPagina != paginaAtual)
        {
            paginaAtual = novaPagina;
            AtualizarExibicaoPagina();
        }
    }

    private void AtualizarExibicaoPagina()
    {
        if (paginasAtuais == null || paginasAtuais.Length == 0) return;

        if (componenteTextoHistoria != null)
        {
            componenteTextoHistoria.text = paginasAtuais[paginaAtual];
        }

        if (textoContadorPaginas != null)
        {
            bool esconder = paginasAtuais.Length <= 1;
            textoContadorPaginas.gameObject.SetActive(!esconder);
            if (!esconder)
            {
                textoContadorPaginas.text = $"Página {paginaAtual + 1} de {paginasAtuais.Length}";
            }
        }
    }

    void OnDisable()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(TerminarInspecao);
        }
    }
}