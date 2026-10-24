using UnityEngine;

// Áudio da sala: passos do jogador, relógio em loop e vento ambiente.
// Coloque em um objeto vazio da cena (ex.: "SomSala") e arraste os clipes no Inspector.
public class SomSala : MonoBehaviour
{
    [Header("Passos")]
    [Tooltip("Vários clipes curtos (3 a 5) para variar. Sem silêncio no início de cada clipe.")]
    public AudioClip[] passos;
    [Range(0f, 1f)] public float volumePassos = 0.6f;
    [Tooltip("Segundos entre um passo e o próximo andando para frente. Use (duração do clipe Walk) / (passos por ciclo).")]
    public float intervaloFrente = 0.7f;
    [Tooltip("Segundos entre passos andando de ré. Use (duração do clipe Backwards) / (passos por ciclo).")]
    public float intervaloRe = 0.6f;
    [Tooltip("Segundos até o primeiro passo depois de começar a andar (acerta com o pé tocando o chão).")]
    public float atrasoPrimeiroPasso = 0.25f;
    [Tooltip("Variação aleatória de pitch (mín, máx), para os passos não soarem idênticos.")]
    public Vector2 variacaoPitch = new Vector2(0.95f, 1.05f);
    [Tooltip("Valor mínimo (em módulo) do parâmetro Speed do Animator para contar como andando.")]
    public float velocidadeMinima = 0.1f;

    [Header("Relógio (loop)")]
    public AudioClip somRelogio;
    [Tooltip("Onde o relógio está na sala. Vazio = posição deste objeto.")]
    public Transform posicaoRelogio;
    [Range(0f, 1f)] public float volumeRelogio = 0.5f;
    [Tooltip("0 = som igual em qualquer lugar (2D). 1 = some com a distância até o AudioListener (3D).")]
    [Range(0f, 1f)] public float espacializacaoRelogio = 0.5f;
    public float distanciaMaximaRelogio = 15f;

    [Header("Vento (loop)")]
    public AudioClip somVento;
    [Range(0f, 1f)] public float volumeVento = 0.25f;
    [Tooltip("Quanto o volume do vento varia (0 = constante, 1 = vai quase a zero entre rajadas).")]
    [Range(0f, 1f)] public float oscilacaoVento = 0.4f;
    [Tooltip("Segundos de uma oscilação do vento até a próxima.")]
    public float periodoOscilacaoVento = 8f;

    [Header("Madeira (rangidos)")]
    [Tooltip("Vários clipes curtos de rangido. Um é sorteado a cada vez.")]
    public AudioClip[] madeira;
    [Range(0f, 1f)] public float volumeMadeira = 0.5f;
    [Tooltip("Intervalo aleatório (mín, máx), em segundos, entre rangidos ambientes da casa.")]
    public Vector2 intervaloMadeira = new Vector2(12f, 30f);
    [Tooltip("Chance (0 a 1) de um passo também fazer o piso ranger. 0 = só rangidos ambientes.")]
    [Range(0f, 1f)] public float chanceMadeiraNoPasso = 0.1f;
    [Tooltip("Variação aleatória de pitch (mín, máx) dos rangidos.")]
    public Vector2 variacaoPitchMadeira = new Vector2(0.9f, 1.1f);

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    AudioSource fontePassos, fonteRelogio, fonteVento, fonteMadeira;
    Animator animatorJogador;
    CharacterController controladorJogador;
    float timerPasso;
    int ultimoPasso = -1;
    int ultimaMadeira = -1;
    float timerMadeira;
    float madeiraOcupadaAte;   // PlayOneShot não liga isPlaying, então o fim do rangido é controlado por tempo
    float proximaBusca;

    void Awake()
    {
        fontePassos = CriarFonte("Passos", transform, 0f, false);
        fonteRelogio = CriarFonte("Relogio", posicaoRelogio != null ? posicaoRelogio : transform, espacializacaoRelogio, true);
        fonteVento = CriarFonte("Vento", transform, 0f, true);
        fonteMadeira = CriarFonte("Madeira", transform, 0f, false);
    }

    void Start()
    {
        timerPasso = atrasoPrimeiroPasso;
        timerMadeira = Random.Range(intervaloMadeira.x, intervaloMadeira.y);

        if (somRelogio != null)
        {
            fonteRelogio.clip = somRelogio;
            fonteRelogio.volume = volumeRelogio;
            fonteRelogio.rolloffMode = AudioRolloffMode.Linear;
            fonteRelogio.minDistance = 1f;
            fonteRelogio.maxDistance = distanciaMaximaRelogio;
            fonteRelogio.time = Random.Range(0f, somRelogio.length);   // não começa sempre no mesmo ponto
            fonteRelogio.Play();
        }

        if (somVento != null)
        {
            fonteVento.clip = somVento;
            fonteVento.volume = volumeVento;
            fonteVento.time = Random.Range(0f, somVento.length);
            fonteVento.Play();
        }
    }

    void Update()
    {
        AtualizarVento();
        AtualizarMadeira();
        AtualizarPassos();
    }

    AudioSource CriarFonte(string nome, Transform pai, float espacializacao, bool loop)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var fonte = go.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = loop;
        fonte.spatialBlend = espacializacao;
        return fonte;
    }

    void AtualizarVento()
    {
        if (somVento == null) return;

        float ruido = Mathf.PerlinNoise(Time.time / Mathf.Max(0.1f, periodoOscilacaoVento), 0f);
        fonteVento.volume = volumeVento * Mathf.Lerp(1f - oscilacaoVento, 1f, ruido);
    }

    void AtualizarPassos()
    {
        if (passos == null || passos.Length == 0) return;

        if (animatorJogador == null)
        {
            ProcurarJogador();
            return;
        }

        // O script de movimento escreve Speed no Animator: positivo = frente, negativo = ré, ~0 = parado ou girando
        float speed = animatorJogador.GetFloat(SpeedHash);
        bool andando = Mathf.Abs(speed) > velocidadeMinima &&
                       (controladorJogador == null || controladorJogador.isGrounded);

        if (!andando)
        {
            timerPasso = atrasoPrimeiroPasso;
            return;
        }

        timerPasso -= Time.deltaTime;
        if (timerPasso <= 0f)
        {
            TocarPasso();
            timerPasso = speed < 0f ? intervaloRe : intervaloFrente;
        }
    }

    void TocarPasso()
    {
        int i = Random.Range(0, passos.Length);
        if (passos.Length > 1 && i == ultimoPasso) i = (i + 1) % passos.Length;   // não repete o mesmo clipe
        ultimoPasso = i;

        fontePassos.pitch = Random.Range(variacaoPitch.x, variacaoPitch.y);
        fontePassos.PlayOneShot(passos[i], volumePassos);

        if (Random.value < chanceMadeiraNoPasso) TocarMadeira();
    }

    void AtualizarMadeira()
    {
        if (madeira == null || madeira.Length == 0) return;

        timerMadeira -= Time.deltaTime;
        if (timerMadeira <= 0f)
        {
            TocarMadeira();
            timerMadeira = Random.Range(intervaloMadeira.x, intervaloMadeira.y);
        }
    }

    void TocarMadeira()
    {
        if (madeira == null || madeira.Length == 0) return;
        if (Time.time < madeiraOcupadaAte) return;   // não sobrepõe dois rangidos

        int i = Random.Range(0, madeira.Length);
        if (madeira.Length > 1 && i == ultimaMadeira) i = (i + 1) % madeira.Length;
        ultimaMadeira = i;

        fonteMadeira.pitch = Random.Range(variacaoPitchMadeira.x, variacaoPitchMadeira.y);
        fonteMadeira.panStereo = Random.Range(-0.7f, 0.7f);   // vem de um lado diferente a cada vez
        fonteMadeira.PlayOneShot(madeira[i], volumeMadeira);
        madeiraOcupadaAte = Time.time + madeira[i].length / fonteMadeira.pitch;
    }

    // O jogador pode ser criado depois (spawn), então a busca é repetida a cada 0,5 s até achar
    void ProcurarJogador()
    {
        if (Time.time < proximaBusca) return;
        proximaBusca = Time.time + 0.5f;

        var jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador == null) return;

        animatorJogador = jogador.GetComponentInChildren<Animator>();
        controladorJogador = jogador.GetComponent<CharacterController>();
    }
}