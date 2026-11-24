using System.Collections;
using UnityEngine;

/// <summary>
/// Sincroniza sons de passos subindo/descendo a escada nas cenas EscadaUp e EscadaDown.
/// Suporta 3 modos:
/// 1) Animation Event (chame TocarPasso() direto nos frames da animação da Main Camera)
/// 2) Tempos exatos em segundos (preencha temposExatosEmSegundos no Inspector)
/// 3) Ritmo automático (atrasoInicial + intervaloEntrePassos + quantidadeDePassos)
/// </summary>
public class SincronizadorSomEscada : MonoBehaviour
{
    public enum ModoSincronizacao
    {
        PorRitmoAutomatico,
        PorTemposExatos,
        ApenasAnimationEvent
    }

    [Header("Modo de Sincronização")]
    [SerializeField] private ModoSincronizacao modo = ModoSincronizacao.PorRitmoAutomatico;

    [Header("Áudio dos Passos na Escada")]
    [Tooltip("Opcional: se deixar vazio, o script cria um AudioSource automaticamente")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Adicione 1 ou mais áudios de passo na madeira/escada (sorteia para não repetir igual)")]
    [SerializeField] private AudioClip[] sonsDePassos;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.85f;
    [Tooltip("Pequena variação no tom de cada passo para soar natural")]
    [Range(0f, 0.25f)] [SerializeField] private float variacaoDePitch = 0.06f;

    [Header("Modo 1: Por Ritmo Automático")]
    [Min(0f)] [SerializeField] private float atrasoInicial = 0.2f;
    [Min(0.05f)] [SerializeField] private float intervaloEntrePassos = 0.45f;
    [Tooltip("Quantidade de passos a tocar (0 = toca continuamente até mudar de cena)")]
    [Min(0)] [SerializeField] private int quantidadeDePassos = 6;

    [Header("Modo 2: Por Tempos Exatos (em segundos)")]
    [Tooltip("Ex: 0.25, 0.70, 1.15, 1.65... cada valor dispara 1 passo naquele segundo")]
    [SerializeField] private float[] temposExatosEmSegundos;

    private int ultimoIndice = -1;

    void Awake()
    {
        if (audioSource == null && !TryGetComponent(out audioSource))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    void Start()
    {
        if (modo == ModoSincronizacao.PorRitmoAutomatico)
        {
            StartCoroutine(RotinaRitmoAutomatico());
        }
        else if (modo == ModoSincronizacao.PorTemposExatos)
        {
            StartCoroutine(RotinaTemposExatos());
        }
    }

    private IEnumerator RotinaRitmoAutomatico()
    {
        if (atrasoInicial > 0f)
            yield return new WaitForSeconds(atrasoInicial);

        int tocados = 0;
        while (quantidadeDePassos == 0 || tocados < quantidadeDePassos)
        {
            TocarPasso();
            tocados++;
            yield return new WaitForSeconds(intervaloEntrePassos);
        }
    }

    private IEnumerator RotinaTemposExatos()
    {
        if (temposExatosEmSegundos == null || temposExatosEmSegundos.Length == 0)
            yield break;

        float tempoDecorrido = 0f;
        for (int i = 0; i < temposExatosEmSegundos.Length; i++)
        {
            float alvo = Mathf.Max(tempoDecorrido, temposExatosEmSegundos[i]);
            float espera = alvo - tempoDecorrido;
            if (espera > 0f)
                yield return new WaitForSeconds(espera);

            tempoDecorrido = alvo;
            TocarPasso();
        }
    }

    /// <summary>
    /// Toca 1 passo sorteado. Pode ser chamado também por Animation Event no Animator da Main Camera.
    /// </summary>
    public void TocarPasso()
    {
        if (audioSource == null || sonsDePassos == null || sonsDePassos.Length == 0) return;

        int indice = 0;
        if (sonsDePassos.Length > 1)
        {
            do
            {
                indice = Random.Range(0, sonsDePassos.Length);
            }
            while (indice == ultimoIndice);
        }
        ultimoIndice = indice;

        AudioClip clip = sonsDePassos[indice];
        if (clip == null) return;

        audioSource.pitch = 1f + Random.Range(-variacaoDePitch, variacaoDePitch);
        audioSource.PlayOneShot(clip, volume);
    }
}
