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

    private bool lendoTelaPreta = false;

    [Header("Configurações na Tela")]
    [Tooltip("Distância que o objeto vai flutuar na frente da câmera")]
    [SerializeField] private float distanciaDaCamera = 1.5f;
    [SerializeField] private float velocidadeDeRotacao = 150f;

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

    public bool EstaInspecionando => estaInspecionando;
    public bool FechouNesteFrame => Time.frameCount == ultimoFrameFechado;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    // Sobrecargas por compatibilidade
    public void IniciarInspecao(GameObject modelo3D)
    {
        IniciarInspecao(modelo3D, Quaternion.identity, "");
    }

    public void IniciarInspecao(GameObject modelo3D, string textoItem)
    {
        IniciarInspecao(modelo3D, Quaternion.identity, textoItem);
    }

    public void IniciarInspecao(GameObject modelo3D, Quaternion rotacaoInicial, string textoItem)
    {
        if (estaInspecionando) return;

        cameraTransform = Camera.main.transform;

        objetoSendoInspecionado = Instantiate(modelo3D);

        // Posição e rotação em coordenadas LOCAIS da câmera:
        // o objeto sempre nasce na frente dela e com a orientação escolhida
        objetoSendoInspecionado.transform.SetParent(cameraTransform, false);
        objetoSendoInspecionado.transform.localPosition = new Vector3(0f, 0f, distanciaDaCamera);
        objetoSendoInspecionado.transform.localRotation = rotacaoInicial;

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
        if (componenteTextoHistoria != null)
        {
            componenteTextoHistoria.text = textoItem;
        }

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

    void Update()
    {
        if (!estaInspecionando) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            posicaoAnteriorDoMouse = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.isPressed && objetoSendoInspecionado != null && !lendoTelaPreta)
        {
            Vector2 posicaoAtual = Mouse.current.position.ReadValue();
            Vector2 dif = posicaoAtual - posicaoAnteriorDoMouse;

            float rotX = dif.y;
            float rotY = -dif.x;

            objetoSendoInspecionado.transform.Rotate(cameraTransform.up, rotY * velocidadeDeRotacao * Time.deltaTime, Space.World);
            objetoSendoInspecionado.transform.Rotate(cameraTransform.right, rotX * velocidadeDeRotacao * Time.deltaTime, Space.World);

            posicaoAnteriorDoMouse = posicaoAtual;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            lendoTelaPreta = !lendoTelaPreta;

            if (painelTextoDetalhado != null)
                painelTextoDetalhado.SetActive(lendoTelaPreta);

            if (textoBotaoQ != null)
            {
                textoBotaoQ.text = lendoTelaPreta
                    ? "pressione Q para voltar a inspeção"
                    : "pressione Q para ler detalhes";
            }
        }

        if (Keyboard.current != null && Time.frameCount > frameInicioInspecao)
        {
            bool apertouEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
            bool dialogoAtivo = GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo;

            if (apertouEsc || (!dialogoAtivo && Keyboard.current.eKey.wasPressedThisFrame))
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
        if (painelTextoDetalhado != null) painelTextoDetalhado.SetActive(false);
        if (telaGuiasDeTecla != null) telaGuiasDeTecla.SetActive(false);

        aoTerminarInspecao?.Invoke();
    }

    void OnDisable()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(TerminarInspecao);
        }
    }
}