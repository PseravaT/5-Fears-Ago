using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CenaCarro : MonoBehaviour
{
    [Header("Carro")]
    public Transform carro;
    public Transform inicio;
    public Transform fim;
    public Transform[] rodas;                       // opcional
    public Vector3 eixoRoda = Vector3.right;        // eixo em que as rodas giram
    public AudioSource somCarro;     // o Audio Source do carro (desmarque Play On Awake nele)
    public float raioRoda = 0.35f;
    public float balanco = 0.01f;                   // vibração vertical do carro

    [Header("Freada")]
    public AudioClip somFreada;                     // som do carro freando (vazio = sem freada)
    public AudioSource fonteFreada;                 // opcional; se vazio, cria um Audio Source 2D sozinho
    [Range(0f, 1f)] public float volumeFreada = 1f;
    public float fadeMotorFreada = 0.3f;            // segundos para o motor sumir quando a freada começa
    public float esperaMaximaFreada = 0f;           // 0 = espera o som inteiro; senão espera no máximo isso (s)
    public float pausaAposFreada = 0.3f;            // silêncio (s) depois da freada, antes de trocar de cena

    [Header("Timeline (opcional)")]
    public PlayableDirector diretor;                // se preenchido, usa a Timeline

    [Header("Tempos (segundos)")]
    public float fadeEntrada = 1.5f;
    public float duracao = 10f;                     // tempo que o carro leva de Inicio a Fim
    public float pausaFinal = 1f;
    public float fadeSaida = 2f;
    public float fadeSomEntrada = 0.4f;   // segundos para o volume do carro subir
    public float atrasoParaMover = 0f;    // segundos entre o fim do fade e o carro começar a andar


    [Header("Cena")]
    public CanvasGroup fade;
    public string proximaCena = "atual";
    public bool permitirPular = true;

    bool pular;

    void Awake()
    {
        // Fonte própria para a freada. Não pode ser o somCarro: o volume do motor
        // desce a zero e levaria o som da freada junto (PlayOneShot é multiplicado pelo volume da fonte).
        if (fonteFreada == null)
        {
            fonteFreada = gameObject.AddComponent<AudioSource>();
            fonteFreada.playOnAwake = false;
            fonteFreada.spatialBlend = 0f;           // 2D: sempre audível, não depende da distância
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (permitirPular && kb != null &&
            (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            pular = true;
    }

    IEnumerator Start()
    {
        fade.alpha = 1;
        fade.blocksRaycasts = true;
        yield return null;                      // deixa passar o 1º quadro (o mais pesado)
        yield return null;

        float volumeAlvo = somCarro ? somCarro.volume : 1f;   // lê o volume antes de zerar

        if (somCarro)
        {
            somCarro.volume = 0f;
            somCarro.Play();
            StartCoroutine(SubirSom(fadeSomEntrada, volumeAlvo));
        }

        yield return Fade(1, 0, fadeEntrada);

        if (atrasoParaMover > 0f) yield return new WaitForSeconds(atrasoParaMover);

        if (diretor != null)
        {
            diretor.Play();
            while (diretor.state == PlayState.Playing && !pular) yield return null;
        }
        else
        {
            yield return Mover();
        }

        if (!pular) yield return new WaitForSeconds(pausaFinal);
        yield return Fade(0, 1, fadeSaida);

        // Tela preta: começa a carregar a próxima cena em segundo plano, sem ativá-la ainda
        AsyncOperation carregamento = SceneManager.LoadSceneAsync(proximaCena);
        carregamento.allowSceneActivation = false;

        // A freada toca com a tela já preta; a cena só troca quando ela terminar
        if (!pular && somFreada)
        {
            yield return Frear();
            if (!pular && pausaAposFreada > 0f) yield return new WaitForSeconds(pausaAposFreada);
        }

        while (carregamento.progress < 0.9f) yield return null;   // garante que terminou de carregar
        carregamento.allowSceneActivation = true;
    }

    IEnumerator Frear()
    {
        fonteFreada.PlayOneShot(somFreada, volumeFreada);
        if (somCarro) StartCoroutine(DescerSom(fadeMotorFreada));

        // Espera o som da freada (ou o tempo máximo), a menos que o jogador pule
        float espera = esperaMaximaFreada > 0f ? Mathf.Min(somFreada.length, esperaMaximaFreada)
                                               : somFreada.length;
        float t = 0f;
        while (t < espera && !pular)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator Mover()
    {
        Vector3 a = inicio.position, b = fim.position;
        carro.position = a;
        Vector3 anterior = a;
        float t = 0;

        while (t < duracao && !pular)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracao);
            Vector3 p = Vector3.Lerp(a, b, k);
            p.y += Mathf.Sin(Time.time * 9f) * balanco;
            carro.position = p;

            float graus = Vector3.Distance(anterior, p) / raioRoda * Mathf.Rad2Deg;
            foreach (var r in rodas) if (r) r.Rotate(eixoRoda, graus, Space.Self);
            anterior = p;
            yield return null;
        }
    }

    IEnumerator Fade(float de, float para, float dur)
    {
        float t = 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            fade.alpha = Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        fade.alpha = para;
    }

    IEnumerator SubirSom(float dur, float alvo)
    {
        if (dur <= 0f) { somCarro.volume = alvo; yield break; }

        float t = 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            somCarro.volume = Mathf.Lerp(0f, alvo, t / dur);
            yield return null;
        }
        somCarro.volume = alvo;
    }

    IEnumerator DescerSom(float dur)
    {
        if (dur <= 0f) { somCarro.volume = 0f; yield break; }

        float inicioVolume = somCarro.volume;
        float t = 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            somCarro.volume = Mathf.Lerp(inicioVolume, 0f, t / dur);
            yield return null;
        }
        somCarro.volume = 0f;
    }
}