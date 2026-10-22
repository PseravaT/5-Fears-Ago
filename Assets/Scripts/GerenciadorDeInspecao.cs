using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class GerenciadorDeInspecao : MonoBehaviour
{
    public static GerenciadorDeInspecao Instancia;

    [Header("O Palco de Inspeção (Sala Escura)")]
    [SerializeField] private GameObject cameraDeInspecao; // A Câmera Virtual do Cinemachine
    [SerializeField] private Transform pontoDeInspecao; // Objeto vazio onde o clone vai flutuar

    [Header("Configurações de Rotação")]
    [SerializeField] private float velocidadeDeRotacao = 100f;

    [Header("Eventos Globais (Inspector)")]
    public UnityEvent aoIniciarInspecao; // Coloque o walking(false) e AlternarDots(false) aqui
    public UnityEvent aoTerminarInspecao; // Coloque o walking(true) e AlternarDots(true) aqui

    private GameObject objetoSendoInspecionado;
    private bool estaInspecionando = false;
    private Vector2 posicaoAnteriorDoMouse;

    void Awake()
    {
        // Transforma esse script num Singleton (Fácil de achar pelo jogo todo)
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        cameraDeInspecao.SetActive(false); // Garante que a câmera comece desligada
    }

    // Qualquer item do jogo vai chamar isso enviando seu modelo 3D
    public void IniciarInspecao(GameObject modelo3D)
    {
        if (estaInspecionando) return;

        cameraDeInspecao.SetActive(true);

        // Cria uma cópia exata do objeto no ponto central da câmera
        objetoSendoInspecionado = Instantiate(modelo3D, pontoDeInspecao.position, Quaternion.identity);
        objetoSendoInspecionado.transform.SetParent(pontoDeInspecao);

        // Prevenção: Se o objeto tiver física, desliga para ele não cair no abismo
        if (objetoSendoInspecionado.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true; 
        }

        aoIniciarInspecao?.Invoke(); // Trava o jogador
        estaInspecionando = true;
    }

    void Update()
    {
        if (!estaInspecionando) return;

        // --- LÓGICA DO VÍDEO (Adaptada para Novo Input System) ---

        // Quando clica: Grava a posição inicial do mouse
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            posicaoAnteriorDoMouse = Mouse.current.position.ReadValue();
        }

        // Enquanto segura o clique: Calcula a rotação
        if (Mouse.current.leftButton.isPressed && objetoSendoInspecionado != null)
        {
            Vector2 posicaoAtualDoMouse = Mouse.current.position.ReadValue();
            Vector2 diferencaMouse = posicaoAtualDoMouse - posicaoAnteriorDoMouse;

            // Invertemos X e Y como no vídeo para a rotação fazer sentido no mouse
            float rotacaoX = diferencaMouse.y;
            float rotacaoY = -diferencaMouse.x;

            Quaternion rotacao = Quaternion.Euler(
                rotacaoX * velocidadeDeRotacao * Time.deltaTime,
                rotacaoY * velocidadeDeRotacao * Time.deltaTime,
                0
            );

            // Aplica a rotação no objeto
            objetoSendoInspecionado.transform.rotation = rotacao * objetoSendoInspecionado.transform.rotation;

            posicaoAnteriorDoMouse = posicaoAtualDoMouse;
        }

        // --- SAIR DA INSPEÇÃO ---
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TerminarInspecao();
        }
    }

    private void TerminarInspecao()
    {
        if (objetoSendoInspecionado != null)
        {
            Destroy(objetoSendoInspecionado); // Apaga a cópia que estávamos olhando
        }

        cameraDeInspecao.SetActive(false); // Desliga a câmera e volta pro jogador
        aoTerminarInspecao?.Invoke(); // Destrava o jogador

        estaInspecionando = false;
    }
}