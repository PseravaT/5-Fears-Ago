using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ValvulaCofre : MonoBehaviour
{
    [Header("Válvula (a roda de 3 braços)")]
    [Tooltip("Objeto com o pivô no centro da roda")]
    [SerializeField] private Transform valvula;
    [Tooltip("Eixo da roda: o que sai da porta em direção a quem olha")]
    [SerializeField] private Vector3 eixoDaValvula = Vector3.forward;

    [Header("Porta")]
    [Tooltip("Objeto com o pivô na DOBRADIÇA (lado direito do cofre). A porta e a válvula ficam dentro dele")]
    [SerializeField] private Transform porta;
    [SerializeField] private Vector3 eixoDaPorta = Vector3.up;
    [SerializeField] private float anguloDeAberturaGraus = 100f;

    [Header("Porta 2 (opcional)")]
    [Tooltip("Segunda porta, com o pivô na dobradiça dela. Abre junto com a primeira")]
    [SerializeField] private Transform porta2;
    [SerializeField] private Vector3 eixoDaPorta2 = Vector3.up;
    [Tooltip("Use valor negativo se ela abrir para o lado oposto da primeira")]
    [SerializeField] private float anguloDeAberturaGraus2 = -100f;

    [Header("Tentativa errada (vai e volta)")]
    [SerializeField] private float anguloDoErroGraus = 35f;
    [SerializeField] private float duracaoDaIdaSegundos = 0.35f;
    [SerializeField] private float pausaNoFimDaIdaSegundos = 0.12f;
    [SerializeField] private float duracaoDaVoltaSegundos = 0.30f;

    [Header("Tentativa certa")]
    [SerializeField] private float duracaoDoGiroCompletoSegundos = 1.1f;
    [SerializeField] private float pausaAntesDaPortaSegundos = 0.25f;
    [SerializeField] private float duracaoDaAberturaSegundos = 1.4f;

    [Header("Sons")]
    [Tooltip("Toca na tentativa errada (maçaneta trancada)")]
    [SerializeField] private AudioClip somTentativaErrada;
    [Tooltip("Toca quando a válvula faz o giro completo")]
    [SerializeField] private AudioClip somGiroCompleto;
    [Tooltip("Toca quando a porta abre")]
    [SerializeField] private AudioClip somAbrindo;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("Salvar estado")]
    [Tooltip("ID para lembrar que este cofre já foi aberto. Se vazio, usa 'NomeDaCena_NomeDoObjeto'")]
    [SerializeField] private string idParaSalvar = "";

    public bool Ocupada { get; private set; }
    public bool Aberto { get; private set; }

    public event Action AoTerminarAnimacao;

    private Quaternion rotValvulaInicial = Quaternion.identity;
    private Quaternion rotPortaInicial = Quaternion.identity;
    private Quaternion rotPorta2Inicial = Quaternion.identity;
    private AudioSource fonte;

    /// <summary>Chave do EstadoDoJogo. Tira o "(Clone)" para a cópia da inspeção usar o mesmo ID do original.</summary>
    private string ChaveDeEstado
    {
        get
        {
            string id = string.IsNullOrWhiteSpace(idParaSalvar)
                ? SceneManager.GetActiveScene().name + "_" + name.Replace("(Clone)", "").Trim()
                : idParaSalvar.Trim();

            return "cofre_aberto_" + id.Replace(",", "_");
        }
    }

    private void Awake()
    {
        if (valvula != null) rotValvulaInicial = valvula.localRotation;
        if (porta != null) rotPortaInicial = porta.localRotation;
        if (porta2 != null) rotPorta2Inicial = porta2.localRotation;

        fonte = GetComponent<AudioSource>();
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
    }

    private void Start()
    {
        // Se o cofre já foi aberto antes (ex.: o jogador saiu da cena e voltou), ele já nasce aberto
        if (EstadoDoJogo.PuzzleConcluido(ChaveDeEstado))
            DefinirAberto();
    }

    /// <summary>correta = false faz o vai e volta; true faz o giro completo e abre as portas.</summary>
    public void Tentar(bool correta)
    {
        if (Ocupada || Aberto) return;
        StartCoroutine(correta ? AbrirRoutine() : ErrarRoutine());
    }

    /// <summary>Deixa as portas abertas na hora, sem animação nem som. Não grava nada no EstadoDoJogo.</summary>
    public void DefinirAberto()
    {
        StopAllCoroutines();
        Ocupada = false;
        Aberto = true;
        AplicarValvula(0f);
        AplicarPortas(1f);
    }

    /// <summary>Abre na hora e guarda no EstadoDoJogo, para o cofre continuar aberto ao voltar à cena.</summary>
    public void DefinirAbertoESalvar()
    {
        DefinirAberto();
        EstadoDoJogo.MarcarPuzzleConcluido(ChaveDeEstado);
    }

    private IEnumerator ErrarRoutine()
    {
        Ocupada = true;
        Tocar(somTentativaErrada);

        yield return Animar(0f, anguloDoErroGraus, duracaoDaIdaSegundos, AplicarValvula);
        yield return new WaitForSeconds(pausaNoFimDaIdaSegundos);
        yield return Animar(anguloDoErroGraus, 0f, duracaoDaVoltaSegundos, AplicarValvula);

        Ocupada = false;
        AoTerminarAnimacao?.Invoke();
    }

    private IEnumerator AbrirRoutine()
    {
        Ocupada = true;
        Tocar(somGiroCompleto);

        yield return Animar(0f, 360f, duracaoDoGiroCompletoSegundos, AplicarValvula);
        yield return new WaitForSeconds(pausaAntesDaPortaSegundos);

        Tocar(somAbrindo);
        yield return Animar(0f, 1f, duracaoDaAberturaSegundos, AplicarPortas);

        Aberto = true;
        Ocupada = false;

        // Guarda que o cofre foi aberto, para continuar aberto se a cena for recarregada
        EstadoDoJogo.MarcarPuzzleConcluido(ChaveDeEstado);

        AoTerminarAnimacao?.Invoke();
    }

    private IEnumerator Animar(float de, float para, float duracaoSegundos, Action<float> aplicar)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duracaoSegundos);
            aplicar(Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t))));
            yield return null;
        }
        aplicar(para);
    }

    private void AplicarValvula(float angulo)
    {
        if (valvula != null)
            valvula.localRotation = rotValvulaInicial * Quaternion.AngleAxis(angulo, eixoDaValvula);
    }

    /// <summary>progresso vai de 0 (fechadas) a 1 (totalmente abertas).</summary>
    private void AplicarPortas(float progresso)
    {
        if (porta != null)
            porta.localRotation = rotPortaInicial * Quaternion.AngleAxis(progresso * anguloDeAberturaGraus, eixoDaPorta);

        if (porta2 != null)
            porta2.localRotation = rotPorta2Inicial * Quaternion.AngleAxis(progresso * anguloDeAberturaGraus2, eixoDaPorta2);
    }

    private void Tocar(AudioClip clip)
    {
        if (clip != null && fonte != null) fonte.PlayOneShot(clip, volume);
    }
}