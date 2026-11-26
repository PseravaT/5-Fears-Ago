using UnityEngine;

/// <summary>
/// Coloque no objeto que toca o som (geladeira, cortina, rádio...). O volume muda com a distância
/// até o JOGADOR (não até a câmera), com curva ajustável, abafamento opcional e pan opcional.
///
/// No AudioSource deste objeto deixe Spatial Blend = 0 (2D): quem controla o espaço aqui é o script.
/// Assim o som continua estéreo e a posição da câmera não interfere.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SomPorDistancia : MonoBehaviour
{
    [Header("Jogador")]
    [Tooltip("Se vazio, procura o objeto que tem o PlayerInteractor")]
    [SerializeField] private Transform jogador;

    [Header("Volume")]
    [Tooltip("Até esta distância o som toca no volume máximo")]
    [SerializeField] private float distanciaCheia = 2f;
    [Tooltip("A partir desta distância o som fica em silêncio")]
    [SerializeField] private float distanciaMaxima = 12f;
    [SerializeField, Range(0f, 1f)] private float volumeMaximo = 1f;
    [Tooltip("Eixo X: 0 = perto (distância cheia), 1 = longe (distância máxima). Eixo Y: volume de 0 a 1")]
    [SerializeField] private AnimationCurve curva = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [Tooltip("Quanto maior, mais rápido o volume acompanha o jogador. 0 = muda na hora")]
    [SerializeField] private float suavizacao = 6f;

    [Header("Abafamento (opcional)")]
    [Tooltip("Longe o som perde os agudos, como se estivesse em outro cômodo")]
    [SerializeField] private bool abafarComDistancia = true;
    [SerializeField] private float frequenciaPerto = 22000f;
    [SerializeField] private float frequenciaLonge = 1200f;

    [Header("Lado do som (opcional)")]
    [Tooltip("O som vem mais da esquerda ou da direita, conforme a posição dele na tela")]
    [SerializeField] private bool panoramaPelaPosicao = false;
    [SerializeField, Range(0f, 1f)] private float intensidadeDoPan = 0.6f;

    private AudioSource fonte;
    private AudioLowPassFilter filtro;
    private float fatorAtual;
    private bool iniciou;

    private void Awake()
    {
        fonte = GetComponent<AudioSource>();
        fonte.spatialBlend = 0f;

        if (abafarComDistancia)
        {
            filtro = GetComponent<AudioLowPassFilter>();
            if (filtro == null) filtro = gameObject.AddComponent<AudioLowPassFilter>();
        }
    }

    private void Start()
    {
        if (jogador == null)
        {
            PlayerInteractor p = FindFirstObjectByType<PlayerInteractor>();
            if (p != null) jogador = p.transform;
        }

        if (jogador == null)
            Debug.LogWarning("[SomPorDistancia] Não achei o jogador. Arraste o Player para o campo 'Jogador'.", this);
    }

    private void Update()
    {
        if (jogador == null) return;

        float distancia = Vector3.Distance(jogador.position, transform.position);
        float t = Mathf.InverseLerp(distanciaCheia, distanciaMaxima, distancia);   // 0 perto, 1 longe
        float alvo = Mathf.Clamp01(curva.Evaluate(t));

        // No primeiro frame vai direto ao valor certo (senão o som "sobe" do zero ao entrar na cena)
        if (!iniciou || suavizacao <= 0f)
        {
            fatorAtual = alvo;
            iniciou = true;
        }
        else
        {
            fatorAtual = Mathf.Lerp(fatorAtual, alvo, 1f - Mathf.Exp(-suavizacao * Time.unscaledDeltaTime));
        }

        fonte.volume = fatorAtual * volumeMaximo;

        if (filtro != null)
            filtro.cutoffFrequency = Mathf.Lerp(frequenciaLonge, frequenciaPerto, fatorAtual);

        if (panoramaPelaPosicao && Camera.main != null)
        {
            Vector3 direcao = transform.position - jogador.position;
            direcao.y = 0f;

            // Perto do jogador o pan some (senão o som pula de lado ao passar por cima)
            float proximidade = Mathf.InverseLerp(0f, Mathf.Max(0.01f, distanciaCheia), direcao.magnitude);
            float lado = Vector3.Dot(direcao.normalized, Camera.main.transform.right);
            fonte.panStereo = Mathf.Clamp(lado * intensidadeDoPan * proximidade, -1f, 1f);
        }
    }

    // Mostra as duas distâncias na Scene: verde = volume cheio, vermelho = silêncio
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, distanciaCheia);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, distanciaMaxima);
    }
}
