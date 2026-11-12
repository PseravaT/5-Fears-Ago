using UnityEngine;

// Coloque nos blobs (frutas/bolhas grandes) que ficam dentro do caldeirão.
// Cada blob sobe e desce com uma fase aleatória, então eles não se movem em sincronia.
public class BoiarNoLiquido : MonoBehaviour
{
    [Header("Movimento vertical")]
    [Tooltip("Quanto o blob sobe e desce (unidades locais)")]
    [SerializeField] private float amplitude = 0.01f;
    [Tooltip("Segundos para um ciclo completo de sobe e desce")]
    [SerializeField] private float periodoEmSegundos = 2.5f;

    [Header("Balanço")]
    [Tooltip("Inclinação máxima em graus")]
    [SerializeField] private float inclinacaoGraus = 4f;

    private Vector3 posicaoBase;
    private Quaternion rotacaoBase;
    private float fase;

    void Awake()
    {
        posicaoBase = transform.localPosition;
        rotacaoBase = transform.localRotation;
        fase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        float angulo = Time.time * (Mathf.PI * 2f / Mathf.Max(periodoEmSegundos, 0.01f)) + fase;

        transform.localPosition = posicaoBase + Vector3.up * (Mathf.Sin(angulo) * amplitude);

        float inclinacaoX = Mathf.Sin(angulo * 0.8f) * inclinacaoGraus;
        float inclinacaoZ = Mathf.Cos(angulo * 1.1f) * inclinacaoGraus;
        transform.localRotation = rotacaoBase * Quaternion.Euler(inclinacaoX, 0f, inclinacaoZ);
    }
}
