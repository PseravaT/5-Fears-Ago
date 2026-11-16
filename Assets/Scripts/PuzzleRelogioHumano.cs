using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleRelogioHumano : MonoBehaviour
{
    [Header("Estátua")]
    public Transform estatua;
    public Vector3 eixoGiro = Vector3.up;
    [Tooltip("Hora para a qual a estátua aponta na rotação inicial (1 a 12)")]
    [Range(1, 12)] public int horaInicial = 12;
    [Tooltip("Tempo de cada giro de 30°, em segundos")]
    public float duracaoGiro = 1.4f;
    public bool inverterDirecao = false;

    [Header("Resistência (atrito)")]
    [Tooltip("1 = velocidade constante; 2 = acelera suave; 3 = começa bem lento e ganha velocidade até o fim")]
    [Range(1f, 5f)] public float aceleracao = 2.5f;
    [Tooltip("Amplitude da trepidação em graus (0 desliga)")]
    [Range(0f, 5f)] public float trepidacao = 1.2f;
    [Tooltip("Velocidade da trepidação (vezes por segundo)")]
    public float frequenciaTrepidacao = 12f;

    [Header("Sequência (5 horários, 1 a 12)")]
    public int[] sequencia = { 3, 7, 12, 5, 9 };
    public bool reiniciarAoErrar = true;

    [Header("Sons")]
    public AudioClip somClick;
    public AudioClip somErro;
    public AudioClip somGiro;
    [Range(0f, 1f)] public float volume = 1f;
    public AudioSource audioSource;

    [Header("Eventos")]
    [Tooltip("Chamado a cada horário correto; o parâmetro é o índice do horário (0 a 4)")]
    public UnityEvent<int> aoAcertar;
    [Tooltip("Ligue aqui a mecânica de sanidade/tensão")]
    public UnityEvent aoErrar;
    public UnityEvent aoConcluir;

    private int passo;       // quantos passos de 30° a estátua girou desde o início
    private int progresso;   // quantos horários corretos já foram confirmados
    private bool girando;
    private bool concluido;
    private Quaternion rotacaoInicial;

    public bool Ocupado => girando || concluido;
    public int Progresso => progresso;

    public int HoraAtual => (((horaInicial - 1 + passo) % 12) + 12) % 12 + 1;

    void Awake()
    {
        if (estatua != null) rotacaoInicial = estatua.localRotation;

        if (audioSource == null && !TryGetComponent(out audioSource))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>direcao: +1 = direita (+30°), -1 = esquerda (-30°). Retorna false se estiver ocupado.</summary>
    public bool Girar(int direcao)
    {
        if (Ocupado || estatua == null) return false;
        if (inverterDirecao) direcao = -direcao;

        passo += direcao;
        StartCoroutine(GirarEstatua());
        return true;
    }

    public bool Confirmar()
    {
        if (Ocupado || sequencia == null || sequencia.Length == 0) return false;

        if (HoraAtual == sequencia[progresso])
        {
            Tocar(somClick);
            aoAcertar?.Invoke(progresso);
            progresso++;

            if (progresso >= sequencia.Length)
            {
                concluido = true;
                aoConcluir?.Invoke();
            }
        }
        else
        {
            Tocar(somErro);
            aoErrar?.Invoke();
            if (reiniciarAoErrar) progresso = 0;
        }
        return true;
    }

    // Versão void para aparecer na lista de funções do UnityEvent (o Inspector não lista métodos com retorno)
    public void ConfirmarHorario()
    {
        Confirmar();
    }

    private IEnumerator GirarEstatua()
    {
        girando = true;
        Tocar(somGiro);

        Quaternion origem = estatua.localRotation;
        Quaternion destino = rotacaoInicial * Quaternion.AngleAxis(passo * 30f, eixoGiro);

        float t = 0f;
        while (t < duracaoGiro)
        {
            t += Time.deltaTime;
            float tn = Mathf.Clamp01(t / duracaoGiro);

            // Velocidade proporcional a tn^(aceleracao-1): parte quase parada e só aumenta até o fim
            float k = Mathf.Pow(tn, aceleracao);
            Quaternion rot = Quaternion.Slerp(origem, destino, k);

            // Trepidação de atrito: some no início e no fim, para a estátua assentar certinha
            if (trepidacao > 0f)
            {
                float ruido = (Mathf.PerlinNoise(Time.time * frequenciaTrepidacao, 0.37f) - 0.5f) * 2f;
                float envelope = Mathf.Sin(tn * Mathf.PI);
                rot *= Quaternion.AngleAxis(ruido * trepidacao * envelope, eixoGiro);
            }

            estatua.localRotation = rot;
            yield return null;
        }

        estatua.localRotation = destino;
        girando = false;
    }

    private void Tocar(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }
}