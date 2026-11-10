using UnityEngine;
using UnityEngine.InputSystem; // Mantendo o Novo Input System
using UnityEngine.Events;
using UnityEngine.Rendering; // Necessário para controlar o Blur (Volume) no URP

public class GerenciadorDeInspecao : MonoBehaviour
{
    public static GerenciadorDeInspecao Instancia;

    [Header("UI de Leitura (Tecla Q)")]
    public GameObject telaGuiasDeTecla;     // O objeto pai que liga/desliga o "Esc" e o "Q"
    public GameObject painelTextoDetalhado; // A tela preta com transparência
    public TMPro.TextMeshProUGUI componenteTextoHistoria; // Onde vai aparecer o texto do item
    public TMPro.TextMeshProUGUI textoBotaoQ; // O texto do canto inferior direito

    private bool lendoTelaPreta = false;

    [Header("Configurações na Tela")]
    [Tooltip("Distância que o objeto vai flutuar na frente da câmera")]
    [SerializeField] private float distanciaDaCamera = 1.5f; 
    [SerializeField] private float velocidadeDeRotacao = 150f;

    [Header("Efeito de Blur (Fundo)")]
    [SerializeField] private Volume volumeDeBlur; // O volume do URP que fará o desfoque

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

    public void IniciarInspecao(GameObject modelo3D, string textoItem = "")
    {
        if (estaInspecionando) return;

        cameraTransform = Camera.main.transform;
        Vector3 posicaoSpawn = cameraTransform.position + (cameraTransform.forward * distanciaDaCamera);

        objetoSendoInspecionado = Instantiate(modelo3D, posicaoSpawn, Quaternion.identity);
        objetoSendoInspecionado.transform.SetParent(cameraTransform);

        if (objetoSendoInspecionado.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true; 
        }

        // Muda a layer do objeto e de todos os filhos dele para a luz de inspeção os pegar
        MudarLayerRecursivo(objetoSendoInspecionado.transform, LayerMask.NameToLayer("Inspecao"));

        if (volumeDeBlur != null) volumeDeBlur.weight = 1f;

        // --- PREPARAÇÃO DA UI AO INICIAR A INSPEÇÃO ---
        if (componenteTextoHistoria != null)
        {
            componenteTextoHistoria.text = textoItem;
        }
        else if (painelTextoDetalhado != null)
        {
            // Fallback automático se esqueceu de arrastar o TextMeshPro no Inspector
            componenteTextoHistoria = painelTextoDetalhado.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (componenteTextoHistoria != null)
                componenteTextoHistoria.text = textoItem;
        }

        if (telaGuiasDeTecla != null) telaGuiasDeTecla.SetActive(true);
        lendoTelaPreta = false;
        
        if (painelTextoDetalhado != null) painelTextoDetalhado.SetActive(false);
        if (textoBotaoQ != null) textoBotaoQ.text = "pressione Q para ler detalhes";

        // Conecta ao fim do diálogo para que terminar a última fala (com E/clique) também saia da inspeção
        // apenas se o diálogo começou DURANTE ou DEPOIS do início da inspeção (não fecha o 3D logo ao nascer!)
        if (GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(TerminarInspecao);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(TerminarInspecao);
        }

        aoIniciarInspecao?.Invoke();
        estaInspecionando = true;
        frameInicioInspecao = Time.frameCount;
    }

    // Sobrecarga para compatibilidade
    public void IniciarInspecao(GameObject modelo3D)
    {
        IniciarInspecao(modelo3D, "");
    }

    private void MudarLayerRecursivo(Transform obj, int novaLayer)
    {
        if (novaLayer == -1) return; // Prevenção de erro caso esqueça de criar a layer
        
        obj.gameObject.layer = novaLayer;
        foreach (Transform filho in obj)
        {
            MudarLayerRecursivo(filho, novaLayer);
        }
    }

    void Update()
    {
        if (!estaInspecionando) return;

        // Salva a posição inicial do clique do mouse
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            posicaoAnteriorDoMouse = Mouse.current.position.ReadValue();
        }

        // Só permite girar o objeto se a tela preta com texto NÃO estiver aberta
        if (Mouse.current.leftButton.isPressed && objetoSendoInspecionado != null && !lendoTelaPreta)
        {
            Vector2 posicaoAtual = Mouse.current.position.ReadValue();
            Vector2 dif = posicaoAtual - posicaoAnteriorDoMouse;

            float rotX = dif.y;
            float rotY = -dif.x;

            // Roda o objeto baseando-se no eixo da câmera, para o mouse não bugar quando olhar de lado
            objetoSendoInspecionado.transform.Rotate(cameraTransform.up, rotY * velocidadeDeRotacao * Time.deltaTime, Space.World);
            objetoSendoInspecionado.transform.Rotate(cameraTransform.right, rotX * velocidadeDeRotacao * Time.deltaTime, Space.World);

            posicaoAnteriorDoMouse = posicaoAtual;
        }

        // --- CONTROLE DO BOTÃO Q (TELA PRETA) ---
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            lendoTelaPreta = !lendoTelaPreta; // Alterna entre ligado e desligado
            
            if (painelTextoDetalhado != null) 
                painelTextoDetalhado.SetActive(lendoTelaPreta); 

            if (textoBotaoQ != null)
            {
                // Muda a frase do botão Q dependendo da tela
                if (lendoTelaPreta)
                    textoBotaoQ.text = "pressione Q para voltar a inspeção";
                else
                    textoBotaoQ.text = "pressione Q para ler detalhes";
            }
        }

        // --- CONTROLE DO BOTÃO ESC OU E (SAIR DA INSPEÇÃO) ---
        if (Keyboard.current != null && Time.frameCount > frameInicioInspecao)
        {
            bool apertouEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
            bool dialogoAtivo = GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo;

            // Se apertar ESC (a qualquer momento) ou apertar E (quando não houver mais falas de diálogo ativas)
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

            // Se saiu da inspeção (ex: via ESC) enquanto o diálogo ainda estava aberto, fecha o diálogo também
            if (GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
            {
                GerenciadorDeDialogo.Instancia.EsconderTexto();
            }
        }

        // 1. Destrói o modelo 3D flutuante
        if (objetoSendoInspecionado != null)
        {
            Destroy(objetoSendoInspecionado);
        }

        // 2. Desliga o desfoque de fundo (peso 0)
        if (volumeDeBlur != null) volumeDeBlur.weight = 0f;

        // 3. Desliga a UI de inspeção
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
