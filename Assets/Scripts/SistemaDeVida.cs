using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema Global de Vida do Personagem.
/// Conecta os erros dos puzzles aos estágios visuais e sonoros do DamageSystem,
/// mantendo a vida e o estágio de dano salvos entre todas as cenas (Sala, Cozinha, Portas, etc.).
/// </summary>
public class SistemaDeVida : MonoBehaviour
{
    public static SistemaDeVida Instancia { get; private set; }

    // Estado global estático: garante que a vida nunca se perca na troca de cenas,
    // mesmo que você coloque este script em objetos diferentes em cada cena para usar no Inspector.
    private static int s_vidaMaxima = 3;
    private static int s_estagioAtual = 0;
    private static bool s_inicializado = false;

    [Header("Configurações de Vida (1 Erro = +1 Estágio de Dano)")]
    [Tooltip("Quantidade de erros até perder (padrão = 3: 0=Normal, 1=Amarelo, 2=Vermelho, 3=Preto/Game Over)")]
    public int vidaMaxima = 3;

    [Tooltip("Apenas leitura no Play Mode: mostra quanto de vida resta no momento")]
    [SerializeField] private int vidaAtualInspector = 3;

    [Tooltip("Apenas leitura no Play Mode: mostra o estágio atual (0 = Sem dano, 1 = 1º Dano, 2 = 2º Dano, 3 = Morte)")]
    [SerializeField] private int estagioDeDanoInspector = 0;

    [Header("Integração com DamageSystem")]
    [Tooltip("Opcional: se deixar vazio, ele encontra o DamageSystem da cena automaticamente")]
    public DamageSystem damageSystem;

    [Tooltip("Se marcado, este objeto não é destruído ao trocar de cena")]
    public bool persistirEntreCenas = true;

    [Tooltip("Se marcado, reseta a vida automaticamente quando a cena 'Menu' é carregada")]
    public bool resetarNoMenu = true;

    [Header("Eventos Globais de Vida / Puzzles")]
    [Tooltip("Disparado sempre que o jogador erra um puzzle / perde 1 de vida")]
    public UnityEvent aoErrarPuzzle;

    [Tooltip("Disparado quando entra no Estágio 1 (1º erro - Tela Amarela)")]
    public UnityEvent aoEntrarEstagio1;

    [Tooltip("Disparado quando entra no Estágio 2 (2º erro - Tela Vermelha)")]
    public UnityEvent aoEntrarEstagio2;

    [Tooltip("Disparado no 3º erro quando a vida chega a 0 (Estágio 3 - Tela Preta / Game Over)")]
    public UnityEvent aoMorrer;

    [Tooltip("Disparado quando a vida é resetada ou totalmente curada")]
    public UnityEvent aoResetarVida;

    // Propriedades públicas para consulta via código
    public int VidaMaxima => s_vidaMaxima;
    public int VidaAtual => Mathf.Max(0, s_vidaMaxima - s_estagioAtual);
    public int EstagioAtual => s_estagioAtual;
    public bool EstaMorto => s_estagioAtual >= s_vidaMaxima;

    // Reseta as variáveis estáticas ao dar Play no Editor (mesmo com Domain Reload desligado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstaticosNoPlay()
    {
        s_vidaMaxima = 3;
        s_estagioAtual = 0;
        s_inicializado = false;
        Instancia = null;
    }

    void Awake()
    {
        if (!s_inicializado)
        {
            s_vidaMaxima = Mathf.Max(1, vidaMaxima);
            s_estagioAtual = 0;
            s_inicializado = true;
        }

        if (Instancia == null)
        {
            Instancia = this;
            if (persistirEntreCenas && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        AtualizarInspector();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += AoCarregarCena;
        VincularDamageSystem();
        AtualizarInspector();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
    }

    void Update()
    {
        AtualizarInspector();
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (resetarNoMenu && cena.name == "Menu")
        {
            ResetarVida();
            return;
        }

        VincularDamageSystem();
    }

    private DamageSystem ObterDamageSystem()
    {
        if (damageSystem != null) return damageSystem;
        if (DamageSystem.Instancia != null)
        {
            damageSystem = DamageSystem.Instancia;
            return damageSystem;
        }

        damageSystem = Object.FindFirstObjectByType<DamageSystem>();
        return damageSystem;
    }

    private void VincularDamageSystem()
    {
        DamageSystem ds = ObterDamageSystem();
        if (ds != null)
        {
            // Sincroniza o maxHits do DamageSystem com a vida máxima global
            s_vidaMaxima = Mathf.Max(1, ds.maxHits);
            vidaMaxima = s_vidaMaxima;

            // Se a cena acabou de carregar e o jogador já tinha dano acumulado de outra sala,
            // aplica o estágio atual no DamageSystem sem tocar o som de impacto novamente
            if (ds.Hits != s_estagioAtual)
            {
                ds.SetDamageStage(s_estagioAtual, false);
            }
        }
    }

    // =========================================================================
    // FUNÇÕES PÚBLICAS PARA USAR NOS PUZZLES (INSPECTOR OU CÓDIGO C#)
    // =========================================================================

    /// <summary>
    /// Chame esta função quando o jogador cometer 1 erro em um puzzle.
    /// Pode ser conectada direto no On Interact () do Inspector ou chamada por código.
    /// </summary>
    public void RegistrarErroPuzzle()
    {
        TomarDano(1);
    }

    /// <summary>
    /// Reduz a vida do jogador na quantidade informada e avança o estágio do DamageSystem.
    /// </summary>
    public void TomarDano(int quantidadeDeErros = 1)
    {
        if (quantidadeDeErros <= 0 || EstaMorto) return;

        int estagioAnterior = s_estagioAtual;
        s_estagioAtual = Mathf.Min(s_estagioAtual + quantidadeDeErros, s_vidaMaxima);
        int diferenca = s_estagioAtual - estagioAnterior;

        AtualizarInspector();

        // Aciona o DamageSystem visual e sonoro
        DamageSystem ds = ObterDamageSystem();
        if (ds != null && ds.Hits < s_estagioAtual)
        {
            ds.TakeDamage(diferenca);
        }

        DispararEventosDeEstagio(estagioAnterior, s_estagioAtual);
    }

    /// <summary>
    /// Define diretamente em qual estágio de dano o jogador deve ficar (0 = Normal, 1 = Amarelo, 2 = Vermelho, 3 = Morte).
    /// </summary>
    public void DefinirEstagioDeDano(int novoEstagio)
    {
        novoEstagio = Mathf.Clamp(novoEstagio, 0, s_vidaMaxima);
        if (novoEstagio == s_estagioAtual) return;

        int estagioAnterior = s_estagioAtual;
        s_estagioAtual = novoEstagio;
        AtualizarInspector();

        DamageSystem ds = ObterDamageSystem();
        if (ds != null)
        {
            ds.SetDamageStage(s_estagioAtual, novoEstagio > estagioAnterior);
        }

        if (novoEstagio == 0)
            aoResetarVida?.Invoke();
        else if (novoEstagio > estagioAnterior)
            DispararEventosDeEstagio(estagioAnterior, novoEstagio);
    }

    /// <summary>
    /// Cura o jogador, regredindo estágios de dano (ex: 1 de cura volta do Vermelho para o Amarelo).
    /// </summary>
    public void Curar(int quantidade = 1)
    {
        if (quantidade <= 0 || s_estagioAtual <= 0) return;

        s_estagioAtual = Mathf.Max(0, s_estagioAtual - quantidade);
        AtualizarInspector();

        DamageSystem ds = ObterDamageSystem();
        if (ds != null && ds.Hits > s_estagioAtual)
        {
            ds.Heal(ds.Hits - s_estagioAtual);
        }

        if (s_estagioAtual == 0)
            aoResetarVida?.Invoke();
    }

    /// <summary>
    /// Restaura a vida inteira do jogador (volta ao Estágio 0 sem dano).
    /// </summary>
    public void ResetarVida()
    {
        s_estagioAtual = 0;
        AtualizarInspector();

        DamageSystem ds = ObterDamageSystem();
        if (ds != null && ds.Hits > 0)
        {
            ds.ResetDamage();
        }

        aoResetarVida?.Invoke();
    }

    // =========================================================================
    // ATALHOS ESTÁTICOS (Para chamar de qualquer script com 1 linha de código)
    // =========================================================================

    /// <summary>
    /// Atalho estático: chame SistemaDeVida.RegistrarErroGlobal() de qualquer script de puzzle.
    /// </summary>
    public static void RegistrarErroGlobal(int erros = 1)
    {
        if (Instancia != null)
        {
            Instancia.TomarDano(erros);
            return;
        }

        // Caso não exista objeto SistemaDeVida na cena, ainda assim atualiza e aciona o DamageSystem
        if (!s_inicializado)
        {
            s_vidaMaxima = 3;
            s_estagioAtual = 0;
            s_inicializado = true;
        }

        if (s_estagioAtual >= s_vidaMaxima) return;
        s_estagioAtual = Mathf.Min(s_estagioAtual + erros, s_vidaMaxima);

        DamageSystem ds = DamageSystem.Instancia != null
            ? DamageSystem.Instancia
            : Object.FindFirstObjectByType<DamageSystem>();

        if (ds != null && ds.Hits < s_estagioAtual)
        {
            ds.TakeDamage(s_estagioAtual - ds.Hits);
        }
    }

    // Usado pelo DamageSystem quando você aperta as teclas de teste (T / R) direto nele
    public static void SincronizarPeloDamageSystem(int hitsAtuais, int maxHitsConfigurado)
    {
        s_vidaMaxima = Mathf.Max(1, maxHitsConfigurado);
        s_inicializado = true;

        int anterior = s_estagioAtual;
        s_estagioAtual = Mathf.Clamp(hitsAtuais, 0, s_vidaMaxima);

        if (Instancia != null)
        {
            Instancia.vidaMaxima = s_vidaMaxima;
            Instancia.AtualizarInspector();

            if (s_estagioAtual > anterior)
                Instancia.DispararEventosDeEstagio(anterior, s_estagioAtual);
            else if (s_estagioAtual == 0 && anterior > 0)
                Instancia.aoResetarVida?.Invoke();
        }
    }

    private void DispararEventosDeEstagio(int anterior, int atual)
    {
        if (atual <= anterior) return;

        aoErrarPuzzle?.Invoke();

        if (atual == 1)
            aoEntrarEstagio1?.Invoke();
        else if (atual == 2)
            aoEntrarEstagio2?.Invoke();

        if (atual >= s_vidaMaxima)
            aoMorrer?.Invoke();
    }

    private void AtualizarInspector()
    {
        vidaAtualInspector = VidaAtual;
        estagioDeDanoInspector = EstagioAtual;
    }

    [ContextMenu("Testar: Errar Puzzle (+1 Estágio de Dano)")]
    private void TestarErroInspector() => RegistrarErroPuzzle();

    [ContextMenu("Testar: Resetar Vida (Estágio 0)")]
    private void TestarResetInspector() => ResetarVida();
}

