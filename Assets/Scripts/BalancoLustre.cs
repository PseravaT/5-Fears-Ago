using UnityEngine;

public class BalancoLustre : MonoBehaviour
{
    [Header("Balanço")]
    public float anguloMax = 2f;        // graus de inclinação máxima
    public float velocidade = 1.2f;     // rad/s (1.2 ≈ um ciclo a cada 5 s)
    public float giroVertical = 1f;     // leve torção no eixo vertical, em graus

    [Header("Rajadas de vento")]
    public float velocidadeRajada = 0.2f;
    [Range(0f, 1f)] public float variacaoRajada = 0.6f;

    Quaternion rotInicial;
    float semente;

    void Start()
    {
        rotInicial = transform.localRotation;
        semente = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time;

        // a força do vento varia devagar, o balanço nunca fica mecânico
        float rajada = Mathf.PerlinNoise(t * velocidadeRajada, semente);
        float intensidade = Mathf.Lerp(1f - variacaoRajada, 1f, rajada);

        // dois eixos com velocidades diferentes, o lustre faz uma órbita suave
        float ax = Mathf.Sin(t * velocidade + semente) * anguloMax * intensidade;
        float az = Mathf.Sin(t * velocidade * 0.73f + semente * 1.7f) * anguloMax * intensidade;
        float ay = Mathf.Sin(t * velocidade * 0.4f + semente * 0.5f) * giroVertical * intensidade;

        transform.localRotation = rotInicial * Quaternion.Euler(ax, ay, az);
    }
}