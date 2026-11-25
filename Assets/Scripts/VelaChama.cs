using UnityEngine;

/// <summary>
/// Coloque no objeto da vela. Faz a luz tremular e, se houver, a chama visual balançar e pulsar.
/// Usa ruído de Perlin: o movimento é suave e irregular, sem parecer um pisca-pisca.
/// </summary>
public class VelaChama : MonoBehaviour
{
    [Header("Luz")]
    [Tooltip("Point Light da vela (cor quente, ex.: 255, 150, 60)")]
    [SerializeField] private Light luz;
    [SerializeField] private float intensidadeBase = 1.5f;
    [Tooltip("Quanto a intensidade sobe e desce em torno da base")]
    [SerializeField] private float variacaoDeIntensidade = 0.35f;
    [Tooltip("Maior = tremula mais rápido")]
    [SerializeField] private float velocidade = 6f;
    [Tooltip("Quanto a luz se desloca (em metros). Faz as sombras dançarem")]
    [SerializeField] private float variacaoDePosicao = 0.01f;

    [Header("Chama visual (opcional)")]
    [Tooltip("Objeto da chama (partículas, quad ou modelo). Balança e pulsa junto com a luz")]
    [SerializeField] private Transform chama;
    [SerializeField] private float variacaoDeEscala = 0.12f;
    [Tooltip("Inclinação máxima da chama, em graus")]
    [SerializeField] private float balancoEmGraus = 4f;

    private Vector3 posicaoInicialDaLuz;
    private Vector3 escalaInicialDaChama;
    private Quaternion rotacaoInicialDaChama;
    private float semente;

    private void Awake()
    {
        // Cada vela começa em um ponto diferente do ruído, para duas velas nunca tremularem iguais
        semente = Random.value * 100f;

        if (luz != null) posicaoInicialDaLuz = luz.transform.localPosition;

        if (chama != null)
        {
            escalaInicialDaChama = chama.localScale;
            rotacaoInicialDaChama = chama.localRotation;
        }
    }

    private void Update()
    {
        float t = Time.time * velocidade + semente;

        // Três ruídos independentes, de -1 a 1
        float a = Mathf.PerlinNoise(t, 0f) * 2f - 1f;
        float b = Mathf.PerlinNoise(0f, t * 1.3f) * 2f - 1f;
        float c = Mathf.PerlinNoise(t * 0.7f, 5f) * 2f - 1f;

        if (luz != null)
        {
            luz.intensity = Mathf.Max(0f, intensidadeBase + a * variacaoDeIntensidade);
            luz.transform.localPosition = posicaoInicialDaLuz + new Vector3(b, c, 0f) * variacaoDePosicao;
        }

        if (chama != null)
        {
            // A chama estica mais na vertical do que na horizontal
            float pulso = 1f + a * variacaoDeEscala;
            chama.localScale = new Vector3(
                escalaInicialDaChama.x * (1f - a * variacaoDeEscala * 0.4f),
                escalaInicialDaChama.y * pulso,
                escalaInicialDaChama.z * (1f - a * variacaoDeEscala * 0.4f));

            chama.localRotation = rotacaoInicialDaChama * Quaternion.Euler(0f, 0f, b * balancoEmGraus);
        }
    }
}