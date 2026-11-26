using System.Collections;
using UnityEngine;

/// <summary>
/// Faz a lâmpada piscar como se estivesse com defeito:
/// fica acesa por um tempo, dá uma rajada de piscadas rápidas e,
/// às vezes, apaga por um instante mais longo antes de voltar.
/// Coloque no objeto da lâmpada (ou em qualquer objeto e arraste a Light).
/// </summary>
public class LampadaDefeito : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Se vazio, procura uma Light neste objeto ou nos filhos.")]
    [SerializeField] private Light luz;
    [Tooltip("Opcional: o mesh do bulbo, para o brilho (emissão) apagar junto com a luz.")]
    [SerializeField] private Renderer bulboRenderer;
    [Tooltip("Índice do material do bulbo, caso o renderer tenha mais de um.")]
    [SerializeField] private int indiceMaterial = 0;

    [Header("Tempo aceso entre defeitos (segundos)")]
    [SerializeField] private float acesaMin = 1.5f;
    [SerializeField] private float acesaMax = 6f;

    [Header("Rajada de piscadas")]
    [SerializeField] private int piscadasMin = 2;
    [SerializeField] private int piscadasMax = 6;
    [Tooltip("Duração de cada estado (aceso/apagado) dentro da rajada.")]
    [SerializeField] private float duracaoPiscadaMin = 0.03f;
    [SerializeField] private float duracaoPiscadaMax = 0.15f;
    [Tooltip("Nas piscadas, a luz acende com intensidade entre este valor e 1 (1 = sempre total).")]
    [SerializeField, Range(0f, 1f)] private float intensidadeMinimaNaPiscada = 0.4f;

    [Header("Apagão longo")]
    [Tooltip("Chance (0 a 1) de, depois da rajada, a lâmpada ficar apagada por mais tempo.")]
    [SerializeField, Range(0f, 1f)] private float chanceApagaoLongo = 0.25f;
    [SerializeField] private float apagaoMin = 0.5f;
    [SerializeField] private float apagaoMax = 1.5f;

    [Header("Som (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Tocado sempre que a lâmpada acende durante o defeito (estalo/zumbido curto).")]
    [SerializeField] private AudioClip[] sonsPiscada;
    [SerializeField] private Vector2 variacaoPitch = new Vector2(0.9f, 1.1f);

    private float intensidadeOriginal;
    private Material materialBulbo;
    private Color emissaoOriginal = Color.white;
    private bool temEmissao;

    private static readonly int IdEmissao = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        if (luz == null) luz = GetComponentInChildren<Light>();
        if (luz != null) intensidadeOriginal = luz.intensity;

        if (bulboRenderer != null)
        {
            var mats = bulboRenderer.materials; // instancia os materiais deste objeto
            indiceMaterial = Mathf.Clamp(indiceMaterial, 0, mats.Length - 1);
            materialBulbo = mats[indiceMaterial];

            if (materialBulbo.HasProperty(IdEmissao))
            {
                temEmissao = true;
                emissaoOriginal = materialBulbo.GetColor(IdEmissao);
                materialBulbo.EnableKeyword("_EMISSION");
            }
        }
    }

    void OnEnable()
    {
        if (luz == null) return;
        StartCoroutine(RotinaDefeito());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (luz != null) Definir(1f); // deixa a lâmpada normal ao desativar o script
    }

    private IEnumerator RotinaDefeito()
    {
        Definir(1f);

        while (true)
        {
            // 1) Fica acesa normalmente por um tempo
            yield return new WaitForSeconds(Random.Range(acesaMin, acesaMax));

            // 2) Rajada de piscadas rápidas
            int piscadas = Random.Range(piscadasMin, piscadasMax + 1);
            for (int i = 0; i < piscadas; i++)
            {
                Definir(0f);
                yield return new WaitForSeconds(Random.Range(duracaoPiscadaMin, duracaoPiscadaMax));

                Definir(Random.Range(intensidadeMinimaNaPiscada, 1f));
                TocarSomPiscada();
                yield return new WaitForSeconds(Random.Range(duracaoPiscadaMin, duracaoPiscadaMax));
            }

            // 3) Às vezes apaga por mais tempo antes de voltar
            if (Random.value < chanceApagaoLongo)
            {
                Definir(0f);
                yield return new WaitForSeconds(Random.Range(apagaoMin, apagaoMax));
            }

            Definir(1f);
            TocarSomPiscada();
        }
    }

    /// <summary>0 = apagada, 1 = intensidade original.</summary>
    private void Definir(float fator)
    {
        luz.intensity = intensidadeOriginal * fator;

        if (temEmissao)
            materialBulbo.SetColor(IdEmissao, emissaoOriginal * fator);
    }

    private void TocarSomPiscada()
    {
        if (audioSource == null || sonsPiscada == null || sonsPiscada.Length == 0) return;

        AudioClip clip = sonsPiscada[Random.Range(0, sonsPiscada.Length)];
        if (clip == null) return;

        audioSource.pitch = Random.Range(variacaoPitch.x, variacaoPitch.y);
        audioSource.PlayOneShot(clip);
    }
}
