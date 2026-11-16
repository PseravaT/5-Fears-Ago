using System.Collections;
using UnityEngine;

public class BotaoConfirmarHorario : MonoBehaviour
{
    [Header("Função")]
    public PuzzleRelogioHumano puzzle;

    [Header("Animação")]
    [Tooltip("Parte que desce e sobe. Se vazio, usa o próprio objeto.")]
    public Transform botao;
    [Tooltip("Quanto o botão afunda, no eixo LOCAL do objeto (em unidades da Unity). Ex.: (0, -0.05, 0)")]
    public Vector3 deslocamento = new Vector3(0f, -0.05f, 0f);
    [Tooltip("Tempos em segundos")]
    public float duracaoDescer = 0.15f;
    public float tempoSegurando = 0.1f;
    public float duracaoSubir = 0.3f;

    [Header("Sons (Opcional)")]
    public AudioClip somApertar;
    public AudioClip somSoltar;
    [Range(0f, 1f)] public float volume = 1f;
    public AudioSource audioSource;

    private Vector3 posicaoRepouso;
    private bool animando;

    void Awake()
    {
        if (botao == null) botao = transform;
        posicaoRepouso = botao.localPosition;

        if (audioSource == null && !TryGetComponent(out audioSource))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    // Chame pelo evento On Interact do componente Interagivel
    public void Interagir()
    {
        if (animando || puzzle == null || puzzle.Ocupado) return;
        StartCoroutine(Apertar());
    }

    private IEnumerator Apertar()
    {
        animando = true;
        Tocar(somApertar);

        yield return Mover(posicaoRepouso, posicaoRepouso + deslocamento, duracaoDescer);

        // A confirmação acontece com o botão no fundo
        puzzle.Confirmar();

        yield return new WaitForSeconds(tempoSegurando);
        Tocar(somSoltar);
        yield return Mover(posicaoRepouso + deslocamento, posicaoRepouso, duracaoSubir);

        animando = false;
    }

    private IEnumerator Mover(Vector3 de, Vector3 para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracao));
            botao.localPosition = Vector3.Lerp(de, para, k);
            yield return null;
        }
        botao.localPosition = para;
    }

    private void Tocar(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }
}