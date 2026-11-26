using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloque no MÓVEL que vai se mover. Quando o puzzle indicado é concluído:
/// 1) espera o puzzle, o diálogo e a inspeção fecharem; 2) toca um som e mostra uma mensagem;
/// 3) quando a mensagem fecha, o móvel desliza até a nova posição.
/// A posição final fica salva (EstadoDoJogo): ao voltar para a cena, o móvel já está no lugar novo.
/// </summary>
public class MoverMovelAposPuzzle : MonoBehaviour
{
    [Header("Quando mover")]
    [Tooltip("O 'Id Unico' do puzzle do quarto. O móvel se move quando esse puzzle for concluído")]
    [SerializeField] private string idDoPuzzle = "";
    [Tooltip("Desligado: se o puzzle já estava concluído quando a cena abriu (ex.: foi concluído em outra cena), o móvel aparece direto no lugar novo. Ligado: toca o som, a mensagem e a animação ao entrar na cena")]
    [SerializeField] private bool tocarSequenciaAoEntrarNaCena = false;
    [Tooltip("Pausa, em segundos, depois de concluir o puzzle e antes de começar")]
    [SerializeField] private float atrasoAntesDoSomSegundos = 0.5f;

    [Header("Som e mensagem (tocam ANTES de mover)")]
    [SerializeField] private AudioClip somAntes;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("Se vazio, usa o AudioSource deste objeto ou cria um")]
    [SerializeField] private AudioSource fonteDeSom;
    [Tooltip("Ligado: o som sai do móvel (3D, diminui com a distância). Desligado: toca igual em qualquer lugar (2D). Só vale para a fonte criada pelo script")]
    [SerializeField] private bool somEspacial = false;
    [Tooltip("Texto mostrado no diálogo do jogo. Vazio = só toca o som")]
    [SerializeField, TextArea(2, 4)] private string mensagem = "";
    [Tooltip("Se a mensagem não for fechada nesse tempo, o móvel se move mesmo assim")]
    [SerializeField] private float tempoLimiteDaMensagemSegundos = 30f;
    [Tooltip("Sem mensagem: espera esse tempo depois do som antes de mover")]
    [SerializeField] private float pausaSemMensagemSegundos = 1.5f;

    [Header("Movimento")]
    [Tooltip("Quanto o móvel anda, em metros. X positivo = para a direita, quando o espaço é 'World' e a sala está virada para o eixo Z")]
    [SerializeField] private Vector3 deslocamento = new Vector3(1.5f, 0f, 0f);
    [Tooltip("World: eixos da cena. Self: eixos do próprio móvel (o X dele aponta para a direita DELE)")]
    [SerializeField] private Space espaco = Space.World;
    [Tooltip("Opcional: um objeto vazio que marca onde o móvel deve parar. Se preenchido, ignora o deslocamento. Não coloque como filho do móvel")]
    [SerializeField] private Transform destino;
    [SerializeField] private float duracaoSegundos = 3f;
    [Tooltip("Como o movimento acelera e freia ao longo do tempo (esquerda = começo, direita = fim)")]
    [SerializeField] private AnimationCurve curva = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Opcional: som de arrastar, tocado quando o móvel começa a se mover")]
    [SerializeField] private AudioClip somDoMovimento;

    [Header("Salvar estado")]
    [Tooltip("ID para lembrar que o móvel já foi movido. Se vazio, usa 'NomeDaCena_NomeDoObjeto'")]
    [SerializeField] private string idParaSalvar = "";

    [Header("Eventos")]
    [Tooltip("Ex.: desativar um colisor, ligar uma luz")]
    public UnityEvent aoComecarAMover;
    [Tooltip("Ex.: liberar a passagem, ligar um Interagivel que estava atrás do móvel")]
    public UnityEvent aoTerminarDeMover;

    private Vector3 posInicialLocal;
    private Vector3 posFinalLocal;
    private bool emSequencia;
    private bool jaMovido;

    private string ChaveDoMovel
    {
        get
        {
            string id = string.IsNullOrWhiteSpace(idParaSalvar)
                ? SceneManager.GetActiveScene().name + "_" + name
                : idParaSalvar.Trim();
            return "movel_movido_" + id.Replace(",", "_");
        }
    }

    void Awake()
    {
        posInicialLocal = transform.localPosition;
        posFinalLocal = CalcularFinalLocal(posInicialLocal);

        if (fonteDeSom == null) fonteDeSom = GetComponent<AudioSource>();
        if (fonteDeSom == null)
        {
            fonteDeSom = gameObject.AddComponent<AudioSource>();
            fonteDeSom.playOnAwake = false;
            fonteDeSom.loop = false;
            fonteDeSom.spatialBlend = somEspacial ? 1f : 0f;
            fonteDeSom.rolloffMode = AudioRolloffMode.Linear;
            fonteDeSom.minDistance = 2f;
            fonteDeSom.maxDistance = 25f;
        }
    }

    void OnEnable()
    {
        EstadoDoJogo.AoConcluirPuzzle += AoConcluirPuzzle;
    }

    void OnDisable()
    {
        EstadoDoJogo.AoConcluirPuzzle -= AoConcluirPuzzle;
    }

    void Start()
    {
        // Já foi movido antes (voltou para a cena): aparece direto no lugar novo
        if (EstadoDoJogo.PuzzleConcluido(ChaveDoMovel))
        {
            MoverNaHoraParaOFinal();
            return;
        }

        // O puzzle já estava concluído quando a cena abriu (ex.: concluído em outra cena)
        if (!string.IsNullOrWhiteSpace(idDoPuzzle) && EstadoDoJogo.PuzzleConcluido(idDoPuzzle.Trim()))
        {
            if (tocarSequenciaAoEntrarNaCena)
            {
                IniciarSequencia();
            }
            else
            {
                MoverNaHoraParaOFinal();
                EstadoDoJogo.MarcarPuzzleConcluido(ChaveDoMovel);
            }
        }
    }

    private void AoConcluirPuzzle(string id)
    {
        if (string.IsNullOrWhiteSpace(idDoPuzzle)) return;
        if (id != idDoPuzzle.Trim()) return;

        IniciarSequencia();
    }

    /// <summary>Também pode ser chamado por um UnityEvent (ex.: o evento de conclusão do puzzle).</summary>
    public void IniciarSequencia()
    {
        if (emSequencia || jaMovido || !isActiveAndEnabled) return;
        StartCoroutine(Sequencia());
    }

    private void MoverNaHoraParaOFinal()
    {
        transform.localPosition = posFinalLocal;
        jaMovido = true;
    }

    // ---------- SEQUÊNCIA ----------

    private IEnumerator Sequencia()
    {
        emSequencia = true;

        if (atrasoAntesDoSomSegundos > 0f)
            yield return new WaitForSeconds(atrasoAntesDoSomSegundos);

        // Só começa quando a tela estiver livre (puzzle, diálogo, inspeção e inventário fechados)
        float livreDesde = -1f;
        while (true)
        {
            if (TelaOcupada())
            {
                livreDesde = -1f;
            }
            else
            {
                if (livreDesde < 0f) livreDesde = Time.time;
                if (Time.time - livreDesde >= 0.4f) break; // 0,4 s livre: evita sobrepor um diálogo que abre logo em seguida
            }
            yield return null;
        }

        Tocar(somAntes);
        yield return MostrarMensagem();
        yield return Mover();

        emSequencia = false;
    }

    private static bool TelaOcupada()
    {
        if (PuzzleIngredientes.Instancia != null && PuzzleIngredientes.Instancia.EstaAtivo) return true;
        if (GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo) return true;
        if (GerenciadorDeInspecao.Instancia != null && GerenciadorDeInspecao.Instancia.EstaInspecionando) return true;
        return GerenciadorDeInventario.InventarioAberto;
    }

    private IEnumerator MostrarMensagem()
    {
        GerenciadorDeDialogo dialogo = GerenciadorDeDialogo.Instancia;

        if (string.IsNullOrWhiteSpace(mensagem) || dialogo == null)
        {
            if (pausaSemMensagemSegundos > 0f)
                yield return new WaitForSeconds(pausaSemMensagemSegundos);
            yield break;
        }

        bool fechou = false;
        UnityAction aoFechar = () => fechou = true;

        dialogo.aoFecharDialogo.AddListener(aoFechar);
        dialogo.MostrarTexto(mensagem);

        // Espera o jogador fechar a mensagem (ou o tempo limite)
        float limite = Time.time + tempoLimiteDaMensagemSegundos;
        while (!fechou && Time.time < limite)
            yield return null;

        dialogo.aoFecharDialogo.RemoveListener(aoFechar);
    }

    private IEnumerator Mover()
    {
        aoComecarAMover?.Invoke();
        Tocar(somDoMovimento);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duracaoSegundos);
            float progresso = curva.Evaluate(Mathf.Clamp01(t));
            transform.localPosition = Vector3.LerpUnclamped(posInicialLocal, posFinalLocal, progresso);
            yield return null;
        }

        transform.localPosition = posFinalLocal;
        jaMovido = true;
        EstadoDoJogo.MarcarPuzzleConcluido(ChaveDoMovel);

        aoTerminarDeMover?.Invoke();
    }

    private void Tocar(AudioClip clip)
    {
        if (clip == null || fonteDeSom == null) return;
        fonteDeSom.PlayOneShot(clip, volume);
    }

    // ---------- POSIÇÃO FINAL ----------

    private Vector3 CalcularFinalLocal(Vector3 inicialLocal)
    {
        Transform pai = transform.parent;

        if (destino != null)
            return pai != null ? pai.InverseTransformPoint(destino.position) : destino.position;

        Vector3 deslocamentoNoMundo = espaco == Space.Self ? transform.TransformDirection(deslocamento) : deslocamento;
        Vector3 deslocamentoLocal = pai != null ? pai.InverseTransformVector(deslocamentoNoMundo) : deslocamentoNoMundo;
        return inicialLocal + deslocamentoLocal;
    }

    void OnValidate()
    {
        duracaoSegundos = Mathf.Max(0.1f, duracaoSegundos);
        atrasoAntesDoSomSegundos = Mathf.Max(0f, atrasoAntesDoSomSegundos);
        pausaSemMensagemSegundos = Mathf.Max(0f, pausaSemMensagemSegundos);
        tempoLimiteDaMensagemSegundos = Mathf.Max(1f, tempoLimiteDaMensagemSegundos);
    }

    // Com o móvel selecionado, mostra na janela Scene onde ele vai parar
    void OnDrawGizmosSelected()
    {
        Transform pai = transform.parent;
        Vector3 inicialLocal = Application.isPlaying ? posInicialLocal : transform.localPosition;
        Vector3 finalLocal = Application.isPlaying ? posFinalLocal : CalcularFinalLocal(inicialLocal);

        Vector3 inicialMundo = pai != null ? pai.TransformPoint(inicialLocal) : inicialLocal;
        Vector3 finalMundo = pai != null ? pai.TransformPoint(finalLocal) : finalLocal;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawLine(inicialMundo, finalMundo);
        Gizmos.DrawSphere(finalMundo, 0.08f);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) limites.Encapsulate(renderers[i].bounds);

        // Caixa que mostra o móvel no lugar novo
        Gizmos.DrawWireCube(limites.center + (finalMundo - transform.position), limites.size);
    }
}