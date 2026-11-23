using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Rendering;

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
    [SerializeField] private bool ocultarContadorSePaginaUnica = false;

    private bool lendoTelaPreta = false;
    private string[] paginasAtuais = new string[0];
    private int paginaAtual = 0;

    [Header("Configurações na Tela")]
    [Tooltip("Distância inicial que o objeto vai flutuar na frente da câmera")]
    [SerializeField] private float distanciaDaCamera = 1.5f;
    [SerializeField] private float velocidadeDeRotacao = 150f;

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

    public bool EstaInspecionando => estaInspecionando;
    public bool FechouNesteFrame => Time.frameCount == ultimoFrameFechado;
    public float DistanciaAtualDoObjeto => distanciaAtual;
    public GameObject ObjetoInspecionado => objetoSendoInspecionado;
    public Quaternion RotacaoInicial => rotacaoInicialGuardada;

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
        IniciarInspecao(modelo3D, Quaternion.identity, "", Vector3.zero);
    }

    public void IniciarInspecao(GameObject modelo3D, string textoItem)
    {
        IniciarInspecao(modelo3D, Quaternion.identity, textoItem, Vector3.zero);
    }

    public void IniciarInspecao(GameObject modelo3D, Quaternion rotacaoInicial, string textoItem)
    {
        IniciarInspecao(modelo3D, rotacaoInicial, textoItem, Vector3.zero);
    }

    public void IniciarInspecao(GameObject modelo3D, Quaternion rotacaoInicial, string textoItem, Vector3 posicaoInicial)
    {
        if (estaInspecionando) return;

        cameraTransform = Camera.main.transform;

        objetoSendoInspecionado = Instantiate(modelo3D);

        // Z da posição inicial soma na distância; X e Y deslocam o objeto na tela.
        distanciaAlvo = Mathf.Clamp(distanciaDaCamera + posicaoInicial.z, distanciaMinima, distanciaMaxima);
        distanciaAtual = distanciaAlvo;
        velocidadeZoom = 0f;

        // Posição e rotação em coordenadas LOCAIS da câmera
        objetoSendoInspecionado.transform.SetParent(cameraTransform, false);
        objetoSendoInspecionado.transform.localPosition = new Vector3(posicaoInicial.x, posicaoInicial.y, distanciaAtual);
        objetoSendoInspecionado.transform.localRotation = rotacaoInicial;

        // Guarda o estado inicial
        rotacaoInicialGuardada = rotacaoInicial;
        deslocamentoXYInicial = new Vector2(posicaoInicial.x, posicaoInicial.y);
        distanciaInicialGuardada = distanciaAlvo;

        ControlesTravados = false;
        EmTransicao = false;

        if (objetoSendoInspecionado.TryGetComponent<Rigidbody>(out var rb))
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
        if (textoBotaoQ != null) textoBotaoQ.text = "pressione Q para ler detalhes";

        if (GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(TerminarInspecao);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(TerminarInspecao);
        }

        aoIniciarInspecao?.Invoke();
        estaInspecionando = true;
        frameInicioInspecao = Time.frameCount;
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

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            lendoTelaPreta = !lendoTelaPreta;

            if (painelTextoDetalhado != null)
                painelTextoDetalhado.SetActive(lendoTelaPreta);

            if (lendoTelaPreta)
            {
                AtualizarExibicaoPagina();
            }

            if (textoBotaoQ != null)
            {
                textoBotaoQ.text = lendoTelaPreta
                    ? "pressione Q para voltar a inspeção"
                    : "pressione Q para ler detalhes";
            }
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

            if (apertouEsc || (!dialogoAtivo && !ControlesTravados && Keyboard.current.eKey.wasPressedThisFrame))
            {
                TerminarInspecao();
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
            bool esconder = ocultarContadorSePaginaUnica && paginasAtuais.Length <= 1;
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