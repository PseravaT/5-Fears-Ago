using System;
using System.Collections;
using UnityEngine;

public class TamborPuzzle : MonoBehaviour
{
    [Tooltip("Ângulo (em graus) de cada face, em ordem crescente. Elemento 0 = branco, 1..5 = opções")]
    [SerializeField] private float[] angulosDasFaces = { 0f, 40f, 110f, 180f, 250f, 320f };

    [SerializeField] private Vector3 eixoLocal = Vector3.forward;

    [Tooltip("Soma este ângulo (em graus) a todas as faces, caso o conjunto todo esteja deslocado")]
    [SerializeField] private float anguloDeAjusteGraus = 0f;

    [SerializeField] private float duracaoGiroSegundos = 0.35f;

    [Header("Som")]
    [SerializeField] private AudioClip[] sonsDeGiro;
    [SerializeField, Range(0f, 1f)] private float volumeGiro = 0.8f;
    [Tooltip("Variação aleatória do pitch, para os giros não soarem idênticos")]
    [SerializeField, Range(0f, 0.3f)] private float variacaoDePitch = 0.05f;

    /// <summary>Face de destino. Muda assim que o giro COMEÇA; a animação termina depois.</summary>
    public int IndiceAtual { get; private set; }
    public bool Girando { get; private set; }

    public event Action<TamborPuzzle> AoIniciarGiro;
    public event Action<TamborPuzzle> AoTerminarGiro;

    private Quaternion rotacaoBase;
    private float anguloAcumulado;
    private AudioSource fonte;
    private int ultimoSomTocado = -1;
    private bool visualDesligado;

    private int QuantidadeFaces => angulosDasFaces.Length;

    private void Awake()
    {
        rotacaoBase = transform.localRotation;

        fonte = GetComponent<AudioSource>();
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f; // 2D: o jogador ouve igual em qualquer posição
    }

    #if UNITY_EDITOR
    private void Update()
    {
        if (!Girando && !visualDesligado) AplicarAngulo(anguloAcumulado);
    }
    #endif

    /// <summary>passo = +1 avança, -1 volta. Retorna false se já estiver girando.</summary>
    public bool Girar(int passo)
    {
        if (Girando || passo == 0 || QuantidadeFaces < 2) return false;

        int origem = IndiceAtual;
        int destino = ((origem + passo) % QuantidadeFaces + QuantidadeFaces) % QuantidadeFaces;

        // O estado lógico muda já: se a cópia for destruída no meio do giro, o passo não se perde
        IndiceAtual = destino;
        Girando = true;

        TocarSomGiro();
        AoIniciarGiro?.Invoke(this);

        StartCoroutine(GirarRoutine(passo, origem, destino));
        return true;
    }

    /// <summary>Vai direto para a face, sem animação, sem som e sem disparar eventos. Usado para sincronizar painéis.</summary>
    public void DefinirIndiceImediato(int indice, bool atualizarVisual = true)
    {
        StopAllCoroutines();
        Girando = false;

        indice = ((indice % QuantidadeFaces) + QuantidadeFaces) % QuantidadeFaces;
        IndiceAtual = indice;
        anguloAcumulado = angulosDasFaces[indice];

        visualDesligado = !atualizarVisual;
        if (atualizarVisual) AplicarAngulo(anguloAcumulado);
    }

    private void TocarSomGiro()
    {
        if (sonsDeGiro == null || sonsDeGiro.Length == 0 || fonte == null) return;

        int indice = UnityEngine.Random.Range(0, sonsDeGiro.Length);
        if (sonsDeGiro.Length > 1 && indice == ultimoSomTocado)
            indice = (indice + 1) % sonsDeGiro.Length;   // nunca repete o mesmo seguido
        ultimoSomTocado = indice;

        fonte.pitch = 1f + UnityEngine.Random.Range(-variacaoDePitch, variacaoDePitch);
        fonte.PlayOneShot(sonsDeGiro[indice], volumeGiro);
    }

    private IEnumerator GirarRoutine(int passo, int origem, int destinoIndice)
    {
        // Distância angular real entre as duas faces, sempre no sentido do giro
        float atual = angulosDasFaces[origem];
        float destino = angulosDasFaces[destinoIndice];
        float delta = passo > 0
            ? Mod360(destino - atual)
            : -Mod360(atual - destino);

        float de = anguloAcumulado;
        float para = de + delta;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracaoGiroSegundos;
            float suave = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            AplicarAngulo(Mathf.Lerp(de, para, suave));
            yield return null;
        }

        anguloAcumulado = para;
        AplicarAngulo(para);

        Girando = false;
        AoTerminarGiro?.Invoke(this);
    }

    // Resultado entre 0 (exclusive) e 360 (inclusive); 0 vira 360 para sempre haver giro
    private static float Mod360(float graus)
    {
        float r = ((graus % 360f) + 360f) % 360f;
        return Mathf.Approximately(r, 0f) ? 360f : r;
    }

    private void AplicarAngulo(float angulo)
    {
        transform.localRotation = rotacaoBase * Quaternion.AngleAxis(angulo + anguloDeAjusteGraus, eixoLocal);
    }
}