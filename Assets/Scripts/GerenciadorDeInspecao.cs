using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Rendering; // Necessário para controlar o Blur (Volume) no URP

public class GerenciadorDeInspecao : MonoBehaviour
{
    public static GerenciadorDeInspecao Instancia;

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
    private Vector2 posicaoAnteriorDoMouse;
    private Transform cameraTransform;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }


    public void IniciarInspecao(GameObject modelo3D)
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

        // --- NOVA LINHA: Muda a layer do objeto e de todos os filhos dele ---
        MudarLayerRecursivo(objetoSendoInspecionado.transform, LayerMask.NameToLayer("Inspecao"));

        if (volumeDeBlur != null) volumeDeBlur.weight = 1f;

        aoIniciarInspecao?.Invoke();
        estaInspecionando = true;
    }

    // --- NOVA FUNÇÃO: Coloque isso no final do script ---
    // Essa função garante que se o seu machado for feito de 2 partes (cabo e lâmina), as duas sejam iluminadas
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

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            posicaoAnteriorDoMouse = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.isPressed && objetoSendoInspecionado != null)
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

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TerminarInspecao();
        }
    }

    private void TerminarInspecao()
    {
        if (objetoSendoInspecionado != null)
        {
            Destroy(objetoSendoInspecionado);
        }

        // Desliga o desfoque de fundo (peso 0)
        if (volumeDeBlur != null) volumeDeBlur.weight = 0f;

        aoTerminarInspecao?.Invoke();
        estaInspecionando = false;
    }
}