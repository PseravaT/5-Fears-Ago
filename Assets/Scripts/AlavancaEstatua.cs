
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AlavancaEstatua : MonoBehaviour
{
    public enum Acao { GirarDireita, GirarEsquerda, Confirmar }

    [Header("Função")]
    public Acao acao;
    public PuzzleRelogioHumano puzzle;

    [Header("Animação")]
    [Tooltip("Parte móvel da alavanca (o cabo). O pivô dela deve ficar na base.")]
    public Transform cabo;
    public Vector3 eixoRotacao = Vector3.right;
    [Tooltip("Ângulo da alavanca em repouso (para cima)")]
    public float anguloCima = -35f;
    [Tooltip("Ângulo da alavanca puxada (para baixo)")]
    public float anguloBaixo = 35f;
    [Tooltip("Tempos em segundos")]
    public float duracaoPuxar = 0.25f;
    public float tempoSegurando = 0.1f;
    public float duracaoRetorno = 0.35f;

    [Header("Sons (Opcional)")]
    public AudioClip somPuxar;
    public AudioClip somRetorno;
    [Range(0f, 1f)] public float volume = 1f;
    public AudioSource audioSource;

    [Header("Ao Concluir")]
    public UnityEvent aoConcluir;

    private Quaternion rotacaoBase;
    private bool animando;

    void Awake()
    {
        if (cabo == null) cabo = transform;
        rotacaoBase = cabo.localRotation;
        cabo.localRotation = RotacaoNoAngulo(anguloCima);

        if (audioSource == null && !TryGetComponent(out audioSource))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void Interagir()
    {
        if (animando || puzzle == null || puzzle.Ocupado) return;
        StartCoroutine(Puxar());
    }

    private IEnumerator Puxar()
    {
        animando = true;
        Tocar(somPuxar);

        yield return Animar(anguloCima, anguloBaixo, duracaoPuxar);

        ExecutarAcao();

        yield return new WaitForSeconds(tempoSegurando);

        Tocar(somRetorno);
        yield return Animar(anguloBaixo, anguloCima, duracaoRetorno);

        animando = false;

        // Evento executado ao terminar toda a animação
        aoConcluir?.Invoke();
    }

    private void ExecutarAcao()
    {
        switch (acao)
        {
            case Acao.GirarDireita:
                puzzle.Girar(+1);
                break;

            case Acao.GirarEsquerda:
                puzzle.Girar(-1);
                break;

            case Acao.Confirmar:
                puzzle.Confirmar();
                break;
        }
    }

    private IEnumerator Animar(float de, float para, float duracao)
    {
        float t = 0f;

        while (t < duracao)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracao));
            cabo.localRotation = RotacaoNoAngulo(Mathf.Lerp(de, para, k));
            yield return null;
        }

        cabo.localRotation = RotacaoNoAngulo(para);
    }

    private Quaternion RotacaoNoAngulo(float angulo)
    {
        return rotacaoBase * Quaternion.AngleAxis(angulo, eixoRotacao);
    }

    private void Tocar(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
