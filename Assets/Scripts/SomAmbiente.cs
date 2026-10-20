using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SomAmbiente : MonoBehaviour
{
    public float volumeFinal = 0.15f;
    public float fadeEntrada = 2f;                 // segundos
    public bool comecarEmPontoAleatorio = true;

    [Header("Variação lenta de volume")]
    public float variacao = 0.15f;                 // 0 = desligado
    public float periodo = 25f;                    // segundos de um ciclo

    AudioSource fonte;
    float entrada;

    void Awake()
    {
        fonte = GetComponent<AudioSource>();
        fonte.loop = true;
        fonte.spatialBlend = 0f;
        fonte.playOnAwake = false;
    }

    void Start()
    {
        if (comecarEmPontoAleatorio && fonte.clip)
            fonte.time = Random.Range(0f, fonte.clip.length * 0.9f);
        fonte.volume = 0f;
        fonte.Play();
    }

    void Update()
    {
        entrada = fadeEntrada > 0f ? Mathf.Clamp01(entrada + Time.deltaTime / fadeEntrada) : 1f;
        float onda = 1f + variacao * Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(1f, periodo));
        fonte.volume = volumeFinal * entrada * onda;
    }
}