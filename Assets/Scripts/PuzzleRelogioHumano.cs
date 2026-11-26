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

    [Header("Retorno ao Errar / Acertar")]
    [Tooltip("Tempo em segundos de atraso antes de começar a voltar")]
    public float atrasoAntesDeVoltar = 0.3f;
    [Tooltip("Ao errar, a estátua volta passo a passo (um giro de 30° por vez, cada um com a duração de 'Duracao Giro'). 1 = mesma velocidade do giro; 2 = o dobro da velocidade; 0.5 = metade.")]
    [Min(0.1f)] public float multiplicadorVelocidadeVolta = 1f;
    [Tooltip("Ligado: volta pelo caminho mais curto (no máximo 6 passos). Desligado: desfaz exatamente os passos que foram girados.")]
    public bool voltarPeloCaminhoMaisCurto = true;
    [Tooltip("Duração, em segundos, do giro de volta após acertar")]
    public float duracaoVolta = 2f;

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
    [Tooltip("Chamado sempre que a estátua muda a hora apontada (1 a 12)")]
    public UnityEvent<int> aoMudarHora;
    [Tooltip("Chamado a cada horário correto; o parâmetro é o índice do horário (0 a 4)")]
    public UnityEvent<int> aoAcertar;
    [Tooltip("Ligue aqui a mecânica de sanidade/tensão")]
    public UnityEvent aoErrar;
    public UnityEvent aoConcluir;

    private int passo;       // quantos passos de 30° a estátua girou desde o início
    private int progresso;   // quantos horários corretos já foram confirmados
    private bool girando;
    private bool concluido;
    private bool voltando;
    private Quaternion rotacaoInicial;

    public bool Ocupado => girando || voltando || concluido;
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
        aoMudarHora?.Invoke(HoraAtual);
        StartCoroutine(GirarEstatua(duracaoGiro));
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

            bool ultimo = progresso >= sequencia.Length;
            if (ultimo)
            {
                concluido = true;
                aoConcluir?.Invoke();
            }
        }
        else
        {
            Tocar(somErro);
            if (reiniciarAoErrar) progresso = 0;

            // Aplica 1 de dano no sistema global de vida / DamageSystem
            if (SistemaDeVida.Instancia != null)
            {
                SistemaDeVida.Instancia.RegistrarErroPuzzle();
            }
            else
            {
                SistemaDeVida.RegistrarErroGlobal(1);
            }

            aoErrar?.Invoke();

            // Volta à posição inicial (horaInicial), passo a passo, na mesma velocidade do giro
            StartCoroutine(VoltarParaHoraInicial());
        }
        return true;
    }

    /// <summary>
    /// Desfaz os giros um passo (30°) de cada vez, repetindo o mesmo movimento do giro normal
    /// (mesma duração, mesma curva de atrito e trepidação, e o som de giro a cada passo).
    /// Se a estátua girou 3 passos e cada giro leva 3 s, a volta leva 9 s.
    /// </summary>
    private IEnumerator VoltarParaHoraInicial()
    {
        voltando = true;

        if (atrasoAntesDeVoltar > 0f)
            yield return new WaitForSeconds(atrasoAntesDeVoltar);

        // Quantos passos faltam para voltar ao passo 0 (horaInicial)
        int delta = -passo;
        if (voltarPeloCaminhoMaisCurto)
        {
            delta = ((-passo % 12) + 12) % 12;
            if (delta > 6) delta -= 12;
        }

        if (delta != 0 && estatua != null)
        {
            int direcao = delta > 0 ? 1 : -1;
            int quantidade = Mathf.Abs(delta);
            float duracaoPorPasso = duracaoGiro / Mathf.Max(0.1f, multiplicadorVelocidadeVolta);

            for (int i = 0; i < quantidade; i++)
            {
                passo += direcao;
                aoMudarHora?.Invoke(HoraAtual);
                yield return GirarEstatua(duracaoPorPasso);
            }
        }

        // Garante que o passo zerou na posição inicial exata
        passo = 0;
        if (estatua != null) estatua.localRotation = rotacaoInicial;
        voltando = false;
    }

    // Versão void para aparecer na lista de funções do UnityEvent (o Inspector não lista métodos com retorno)
    public void ConfirmarHorario()
    {
        Confirmar();
    }

    private IEnumerator VoltarAoDoze(bool ultimo)
    {
        voltando = true;

        if (atrasoAntesDeVoltar > 0f)
            yield return new WaitForSeconds(atrasoAntesDeVoltar);

        // Passos (de 30°) entre a posição atual e as 12 horas, pelo caminho mais curto
        int alvo = 12 - horaInicial;
        int delta = (((alvo - passo) % 12) + 12) % 12;
        if (delta > 6) delta -= 12;

        if (delta != 0 && estatua != null)
        {
            passo += delta;
            aoMudarHora?.Invoke(HoraAtual);
            yield return GirarEstatua(duracaoVolta);
        }

        voltando = false;
        if (ultimo) aoConcluir?.Invoke();
    }

    private IEnumerator GirarEstatua(float duracao)
    {
        girando = true;
        Tocar(somGiro);

        Quaternion origem = estatua.localRotation;
        Quaternion destino = rotacaoInicial * Quaternion.AngleAxis(passo * 30f, eixoGiro);

        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            float tn = Mathf.Clamp01(t / duracao);

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