using UnityEngine;

public class BalancoLustre : MonoBehaviour
{
    [Header("Balanço")]
    public float anguloMax = 2f;        // graus de inclinação máxima
    public float velocidade = 1.2f;     // rad/s (1.2 ≈ um ciclo a cada 5 s)
    public float giroVertical = 1f;     // leve torção no eixo vertical, em graus

    [Header("Rajadas de vento")]
    public float velocidadeRajada = 0.2f;
    [Range(0f, 1f)] public float variacaoRajada = 0.6f;

    [Header("Som (rangido)")]
    [Tooltip("Arraste aqui os clips de rangido. Um é sorteado a cada vez, sem repetir o anterior")]
    public AudioClip[] rangidos;
    [Tooltip("Se vazio, usa o AudioSource deste objeto. Se não houver, o script cria um 3D no ponto do lustre")]
    public AudioSource fonteDeSom;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("Tempo mínimo e máximo, em segundos, entre um rangido e o próximo")]
    public Vector2 intervaloSegundos = new Vector2(3f, 8f);
    [Tooltip("Só range quando a rajada de vento passa deste valor (0 a 1). Perto de 0 range sempre; perto de 1 só nas rajadas mais fortes")]
    [Range(0f, 1f)] public float rajadaMinima = 0.45f;
    [Tooltip("Variação aleatória do tom (1 = original). Disfarça a repetição")]
    public Vector2 variacaoPitch = new Vector2(0.95f, 1.05f);

    [Header("Posição e distância do jogador")]
    [Tooltip("Liga o volume por distância: quanto mais longe o jogador, mais baixo o rangido, até sumir")]
    public bool somReageAoJogador = true;
    [Tooltip("Arraste o jogador. Se vazio, usa o objeto com a tag 'Player' e, se não achar, a câmera principal")]
    public Transform jogador;
    [Tooltip("Objeto que marca onde o lustre está. Se vazio, usa este próprio objeto")]
    public Transform posicaoDoLustre;
    [Tooltip("Ajuste fino do ponto do som, em relação à posição do lustre (ex.: 0, -0.5, 0 para um pouco mais baixo)")]
    public Vector3 deslocamento = Vector3.zero;
    [Tooltip("Até esta distância (metros) o rangido fica no volume máximo")]
    public float distanciaMinima = 2f;
    [Tooltip("A partir desta distância (metros) o rangido some e nem toca")]
    public float distanciaMaxima = 15f;
    [Tooltip("Como o volume cai entre a distância mínima (esquerda) e a máxima (direita). Em cima é volume total, embaixo é silêncio")]
    public AnimationCurve curvaDeVolume = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    Quaternion rotInicial;
    float semente;
    float proximoRangido;
    int ultimoRangido = -1;
    float proximaBuscaDoJogador;
    float fatorDeDistancia = 1f;

    /// <summary>Ponto de onde o som sai e de onde a distância é medida.</summary>
    public Vector3 PosicaoDoSom
    {
        get
        {
            Transform referencia = posicaoDoLustre != null ? posicaoDoLustre : transform;
            return referencia.TransformPoint(deslocamento);
        }
    }

    /// <summary>Distância atual até o jogador, em metros. Vale -1 se não achou o jogador.</summary>
    public float DistanciaDoJogador
    {
        get
        {
            Transform j = ObterJogador();
            return j != null ? Vector3.Distance(j.position, PosicaoDoSom) : -1f;
        }
    }

    void Start()
    {
        rotInicial = transform.localRotation;
        semente = Random.value * 100f;

        PrepararFonteDeSom();
        AgendarProximoRangido();
    }

    void Update()
    {
        float t = Time.time;

        // a força do vento varia devagar, o balanço nunca fica mecânico
        float rajada = Mathf.PerlinNoise(t * velocidadeRajada, semente);
        float intensidade = Mathf.Lerp(1f - variacaoRajada, 1f, rajada);

        // dois eixos com velocidades diferentes, o lustre faz uma órbita suave
        float ax = Mathf.Sin(t * velocidade + semente) * anguloMax * intensidade;
        float az = Mathf.Sin(t * velocidade * 0.73f + semente * 1.7f) * anguloMax * intensidade;
        float ay = Mathf.Sin(t * velocidade * 0.4f + semente * 0.5f) * giroVertical * intensidade;

        transform.localRotation = rotInicial * Quaternion.Euler(ax, ay, az);

        AtualizarVolumePorDistancia();
        AtualizarRangido(rajada);
    }

    // ---------- DISTÂNCIA DO JOGADOR ----------

    Transform ObterJogador()
    {
        if (jogador != null) return jogador;

        // Procura no máximo uma vez por segundo, para não pesar
        if (Time.time < proximaBuscaDoJogador) return null;
        proximaBuscaDoJogador = Time.time + 1f;

        GameObject objetoJogador = GameObject.FindWithTag("Player");
        if (objetoJogador != null) jogador = objetoJogador.transform;
        else if (Camera.main != null) jogador = Camera.main.transform;

        return jogador;
    }

    /// <summary>0 = silêncio (longe), 1 = volume total (perto).</summary>
    float CalcularFatorDeDistancia()
    {
        if (!somReageAoJogador) return 1f;

        Transform j = ObterJogador();
        if (j == null) return 1f; // sem jogador, toca normalmente

        float distancia = Vector3.Distance(j.position, PosicaoDoSom);
        if (distancia <= distanciaMinima) return 1f;
        if (distancia >= distanciaMaxima) return 0f;

        float t = Mathf.InverseLerp(distanciaMinima, distanciaMaxima, distancia);
        return Mathf.Clamp01(curvaDeVolume.Evaluate(t));
    }

    void AtualizarVolumePorDistancia()
    {
        fatorDeDistancia = CalcularFatorDeDistancia();

        // O volume da fonte acompanha o jogador em tempo real, até com o rangido já tocando
        if (somReageAoJogador && fonteDeSom != null)
            fonteDeSom.volume = fatorDeDistancia;
    }

    // ---------- SOM ----------

    void PrepararFonteDeSom()
    {
        if (fonteDeSom == null) fonteDeSom = GetComponent<AudioSource>();
        if (fonteDeSom != null) return;

        // Sem AudioSource: cria um 3D num objeto filho, no ponto do lustre (+ deslocamento)
        Transform referencia = posicaoDoLustre != null ? posicaoDoLustre : transform;
        GameObject objetoSom = new GameObject("SomDoLustre");
        objetoSom.transform.SetParent(referencia, false);
        objetoSom.transform.localPosition = deslocamento;

        fonteDeSom = objetoSom.AddComponent<AudioSource>();
        fonteDeSom.playOnAwake = false;
        fonteDeSom.loop = false;
        fonteDeSom.spatialBlend = 1f; // 3D: o lado de onde vem o som segue o ouvinte

        if (somReageAoJogador)
        {
            // A distância já é tratada por este script (pelo jogador): desliga a queda de volume
            // da própria fonte para não atenuar duas vezes
            fonteDeSom.rolloffMode = AudioRolloffMode.Custom;
            fonteDeSom.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));
        }
        else
        {
            fonteDeSom.rolloffMode = AudioRolloffMode.Linear;
            fonteDeSom.minDistance = distanciaMinima;
            fonteDeSom.maxDistance = distanciaMaxima;
        }
    }

    void AtualizarRangido(float rajada)
    {
        if (fonteDeSom == null || rangidos == null || rangidos.Length == 0) return;
        if (Time.time < proximoRangido) return;

        // Jogador longe demais ou vento fraco demais: nem toca, tenta de novo em meio segundo
        if ((somReageAoJogador && fatorDeDistancia <= 0.01f) || rajada < rajadaMinima)
        {
            proximoRangido = Time.time + 0.5f;
            return;
        }

        TocarRangido(rajada);
        AgendarProximoRangido();
    }

    void TocarRangido(float rajada)
    {
        int indice = Random.Range(0, rangidos.Length);

        // Não repete o mesmo clip em sequência
        if (rangidos.Length > 1 && indice == ultimoRangido)
            indice = (indice + 1 + Random.Range(0, rangidos.Length - 1)) % rangidos.Length;

        ultimoRangido = indice;
        AudioClip clip = rangidos[indice];
        if (clip == null) return;

        // Rajada mais forte, rangido mais alto (a distância entra pelo volume da própria fonte)
        float volumeFinal = volume * Mathf.Lerp(0.6f, 1f, rajada);

        fonteDeSom.pitch = Random.Range(variacaoPitch.x, variacaoPitch.y);
        fonteDeSom.PlayOneShot(clip, volumeFinal);
    }

    void AgendarProximoRangido()
    {
        proximoRangido = Time.time + Random.Range(intervaloSegundos.x, intervaloSegundos.y);
    }

    // ---------- EDITOR ----------

    void OnValidate()
    {
        intervaloSegundos.x = Mathf.Max(0.1f, intervaloSegundos.x);
        intervaloSegundos.y = Mathf.Max(intervaloSegundos.x, intervaloSegundos.y);
        variacaoPitch.x = Mathf.Max(0.1f, variacaoPitch.x);
        variacaoPitch.y = Mathf.Max(variacaoPitch.x, variacaoPitch.y);
        distanciaMinima = Mathf.Max(0f, distanciaMinima);
        distanciaMaxima = Mathf.Max(distanciaMinima + 0.1f, distanciaMaxima);
    }

    // Com o lustre selecionado, mostra o ponto do som e os raios de distância na janela Scene
    void OnDrawGizmosSelected()
    {
        Vector3 ponto = PosicaoDoSom;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(ponto, 0.1f);

        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);   // verde: volume máximo até aqui
        Gizmos.DrawWireSphere(ponto, distanciaMinima);

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);   // vermelho: silêncio a partir daqui
        Gizmos.DrawWireSphere(ponto, distanciaMaxima);
    }
}