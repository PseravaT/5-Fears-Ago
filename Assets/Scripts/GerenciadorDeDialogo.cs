using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GerenciadorDeDialogo : MonoBehaviour
{
    public static GerenciadorDeDialogo Instancia;

    [Header("Elementos da Interface")]
    [SerializeField] private GameObject painelBalao;
    [SerializeField] private TextMeshProUGUI componenteDeTexto;
    [Tooltip("Objeto do ícone do triângulo que indica que o jogador pode avançar o diálogo")]
    [SerializeField] private GameObject iconeContinuar;

    [Header("Configurações de Tempo")]
    [SerializeField] private float tempoParaAparecer = 1.5f; // Tempo que o balão espera antes de surgir
    [SerializeField] private float delayEntreLetras = 0.05f; // Velocidade padrão da máquina de escrever
    [Tooltip("Velocidade da digitação enquanto estiver segurando a tecla E (ex: 0.01s = 5x mais rápido)")]
    [SerializeField] private float delayAoAcelerar = 0.01f;

    [Header("Animação do Ícone (Triângulo)")]
    [SerializeField] private float velocidadeFlutuacao = 6f;
    [SerializeField] private float amplitudeFlutuacao = 5f;

    [Header("Input (Opcional - Novo Input System)")]
    [Tooltip("Opcional: Ação de interação do Novo Input System. A tecla 'E' já funciona automaticamente.")]
    [SerializeField] private InputActionReference acaoInteragir;

    [Header("Debug")]
    [Tooltip("Mostra logs no Console para descobrir quem abre/fecha o diálogo. Desmarque quando resolver.")]
    [SerializeField] private bool logsDeDebug = true;

    [Header("Eventos")]
    public UnityEvent aoFecharDialogo;

    private Coroutine corrotinaAtual;
    private readonly List<string> filaDeFalas = new List<string>();
    private int indiceFalaAtual = 0;
    private bool aguardandoInput = false;
    private bool dialogoAtivo = false;
    private int ultimoFrameFechado = -1;
    private Vector3 posicaoInicialIcone;
    private bool posicaoIconeSalva = false;

    // Identifica cada diálogo iniciado. Quem agenda um "esconder depois" guarda o id
    // e só esconde se o diálogo atual ainda for o dele.
    private int idDialogoAtual = 0;
    public int IdDialogoAtual => idDialogoAtual;

    // Textos já exibidos por MostrarTextoUmaVez (para falas de "primeira interação")
    private readonly HashSet<string> falasJaMostradas = new HashSet<string>();

    public bool DialogoEstaAtivo => dialogoAtivo;
    public bool FechouNesteFrame => Time.frameCount == ultimoFrameFechado;

    void Awake()
    {
        if (Instancia == null) Instancia = this;

        SalvarPosicaoInicialIcone();
        ValidarPainel();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        // Se o objeto estava inativo e foi ativado por MostrarTexto, o diálogo já começou:
        // não pode esconder o balão aqui, senão apaga a fala que acabou de iniciar.
        if (dialogoAtivo) return;

        if (painelBalao != null) painelBalao.SetActive(false);
        if (componenteDeTexto != null) componenteDeTexto.text = "";
        DefinirIconeVisivel(false);
    }

    private void Log(string mensagem)
    {
        if (logsDeDebug)
            Debug.Log($"[Dialogo] {mensagem} (frame {Time.frameCount})", this);
    }

    /// <summary>
    /// Avisa se o painel é este mesmo objeto ou um pai dele: desativar o painel
    /// desativaria o gerenciador e mataria as corrotinas.
    /// </summary>
    private void ValidarPainel()
    {
        if (painelBalao == null) return;

        if (transform.IsChildOf(painelBalao.transform))
        {
            Debug.LogError(
                "[GerenciadorDeDialogo] 'painelBalao' é este objeto ou um pai dele. " +
                "Ao desativar o painel, o gerenciador também é desativado e as corrotinas param. " +
                "Mova este script para um objeto que fique sempre ativo (ex: um GameObject vazio).",
                this);
        }
    }

    /// <summary>
    /// Garante que este objeto e todos os pais estão ativos, para que StartCoroutine funcione.
    /// </summary>
    private bool GarantirObjetoAtivo()
    {
        if (gameObject.activeInHierarchy) return true;

        Transform t = transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }

        return gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Monta o caminho do objeto na Hierarchy marcando pais desativados (usado só em logs).
    /// </summary>
    private static string ObterCaminho(Transform t)
    {
        string caminho = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            caminho = t.name + (t.gameObject.activeSelf ? "" : "(INATIVO)") + "/" + caminho;
        }
        return caminho;
    }

    private void SalvarPosicaoInicialIcone()
    {
        if (iconeContinuar != null && !posicaoIconeSalva)
        {
            posicaoInicialIcone = iconeContinuar.transform.localPosition;
            posicaoIconeSalva = true;
        }
    }

    private void DefinirIconeVisivel(bool visivel)
    {
        if (iconeContinuar == null) return;

        SalvarPosicaoInicialIcone();
        iconeContinuar.transform.localPosition = posicaoInicialIcone;
        iconeContinuar.SetActive(visivel);
    }

    void Update()
    {
        // 1. Animação matemática de flutuação do ícone do triângulo enquanto estiver visível
        if (iconeContinuar != null && iconeContinuar.activeSelf)
        {
            float deslocamentoY = Mathf.Sin(Time.time * velocidadeFlutuacao) * amplitudeFlutuacao;
            iconeContinuar.transform.localPosition = posicaoInicialIcone + new Vector3(0f, deslocamentoY, 0f);
        }

        // 2. Avanço por Input quando a frase atual terminou de ser digitada
        if (dialogoAtivo && aguardandoInput && JogadorPressionouAvancar())
        {
            AvancarParaProximaFala();
        }
    }

    private bool JogadorPressionouAvancar()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (acaoInteragir != null && acaoInteragir.action != null && acaoInteragir.action.WasPressedThisFrame())
            return true;

        return false;
    }

    private bool JogadorEstaSegurandoAcelerar()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;

        if (acaoInteragir != null && acaoInteragir.action != null && acaoInteragir.action.IsPressed())
            return true;

        return false;
    }

    /// <summary>
    /// Recebe um array de strings para exibir múltiplas falas em sequência.
    /// </summary>
    public void MostrarTexto(string[] mensagens)
    {
        if (mensagens == null || mensagens.Length == 0) return;

        IniciarSequenciaDeFalas(mensagens);
    }

    /// <summary>
    /// Recebe uma lista de strings para exibir múltiplas falas em sequência.
    /// </summary>
    public void MostrarTexto(List<string> mensagens)
    {
        if (mensagens == null || mensagens.Count == 0) return;

        IniciarSequenciaDeFalas(mensagens);
    }

    /// <summary>
    /// Sobrecarga para uma única string (mantém compatibilidade com o Inspector / scripts existentes).
    /// Dica: se usar '|' no texto do Inspector, ele divide automaticamente em várias falas!
    /// </summary>
    public void MostrarTexto(string mensagem)
    {
        if (string.IsNullOrEmpty(mensagem)) return;

        string[] partes = mensagem.Contains("|")
            ? mensagem.Split('|')
            : new string[] { mensagem };

        IniciarSequenciaDeFalas(partes);
    }

    /// <summary>
    /// Mostra o texto só na primeira vez. Nas chamadas seguintes com o mesmo texto não faz nada.
    /// Pode ser chamado direto do UnityEvent no Inspector (no lugar de MostrarTexto).
    /// </summary>
    public void MostrarTextoUmaVez(string mensagem)
    {
        if (string.IsNullOrEmpty(mensagem)) return;
        if (!falasJaMostradas.Add(mensagem))
        {
            // Já foi mostrada: pula a fala, mas avisa como se o diálogo tivesse fechado,
            // para o que depende disso (ex: abrir o HUD) continuar funcionando.
            Log("MostrarTextoUmaVez: fala já mostrada, pulando e disparando aoFecharDialogo.");
            aoFecharDialogo?.Invoke();
            return;
        }

        MostrarTexto(mensagem);
    }

    private void IniciarSequenciaDeFalas(IEnumerable<string> mensagens)
    {
        Log($"IniciarSequenciaDeFalas chamado. ativoNaHierarquia={gameObject.activeInHierarchy} " +
            $"caminho={ObterCaminho(transform)}");

        // Evita o erro "Coroutine couldn't be started because the game object is inactive"
        if (!GarantirObjetoAtivo())
        {
            Debug.LogError(
                "[GerenciadorDeDialogo] Não foi possível ativar o objeto para iniciar o diálogo. " +
                $"cenaValida={gameObject.scene.IsValid()} " +
                $"cena='{gameObject.scene.name}' " +
                $"activeSelf={gameObject.activeSelf} " +
                $"caminho={ObterCaminho(transform)}",
                this);
            return;
        }

        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
            corrotinaAtual = null;
        }

        filaDeFalas.Clear();
        foreach (string msg in mensagens)
        {
            if (!string.IsNullOrEmpty(msg))
                filaDeFalas.Add(msg.Trim());
        }

        if (filaDeFalas.Count == 0) return;

        indiceFalaAtual = 0;
        aguardandoInput = false;
        idDialogoAtual++; // novo diálogo = novo id
        dialogoAtivo = true;

        // Inicia a primeira fala respeitando o tempoParaAparecer (ex: viagem da câmera Cinemachine)
        corrotinaAtual = StartCoroutine(RotinaMostrarFala(filaDeFalas[indiceFalaAtual], usarDelayInicial: true));
    }

    private void AvancarParaProximaFala()
    {
        aguardandoInput = false;
        DefinirIconeVisivel(false);

        indiceFalaAtual++;

        if (indiceFalaAtual < filaDeFalas.Count)
        {
            if (corrotinaAtual != null)
            {
                StopCoroutine(corrotinaAtual);
            }

            corrotinaAtual = StartCoroutine(RotinaMostrarFala(filaDeFalas[indiceFalaAtual], usarDelayInicial: false));
        }
        else
        {
            EsconderTexto();
        }
    }

    private IEnumerator RotinaMostrarFala(string mensagem, bool usarDelayInicial)
    {
        aguardandoInput = false;
        DefinirIconeVisivel(false);

        if (usarDelayInicial)
        {
            // 1. Garante que a caixa preta está invisível enquanto a câmera viaja
            if (painelBalao != null) painelBalao.SetActive(false);
            if (componenteDeTexto != null) componenteDeTexto.text = "";

            // 2. Espera o tempo configurado (ex: o tempo da transição do Cinemachine)
            if (tempoParaAparecer > 0f)
                yield return new WaitForSeconds(tempoParaAparecer);
        }

        // 3. Ativa a caixa preta e limpa o texto anterior
        if (painelBalao != null) painelBalao.SetActive(true);
        if (componenteDeTexto != null) componenteDeTexto.text = "";

        Log($"Painel ligado. painelAtivoNaHierarquia={(painelBalao != null && painelBalao.activeInHierarchy)}");

        // 4. Efeito de digitação letra a letra (máquina de escrever)
        if (componenteDeTexto != null)
        {
            foreach (char letra in mensagem)
            {
                componenteDeTexto.text += letra;
                float espera = JogadorEstaSegurandoAcelerar() ? delayAoAcelerar : delayEntreLetras;
                yield return new WaitForSeconds(espera);
            }
        }

        // Aguarda o jogador SOLTAR o E antes de liberar o avanço, caso estivesse segurando para acelerar
        while (JogadorEstaSegurandoAcelerar())
        {
            yield return null;
        }

        // Aguarda 1 frame para evitar que o clique anterior avance acidentalmente
        yield return null;

        // 5. Frase concluída: exibe o triângulo e aguarda o jogador pressionar 'E' ou clicar
        DefinirIconeVisivel(true);
        aguardandoInput = true;
        corrotinaAtual = null;
    }

    /// <summary>
    /// Esconde só se o diálogo ainda for o mesmo que quem chamou iniciou.
    /// Use no lugar de EsconderTexto() em timers agendados (ex: TriggerDeFala).
    /// </summary>
    public void EsconderTextoSeForOId(int id)
    {
        if (!dialogoAtivo || id != idDialogoAtual) return;
        EsconderTexto();
    }

    public void EsconderTexto()
    {
        // O stack trace mostra quem fechou o diálogo
        Log("EsconderTexto chamado.\n" + System.Environment.StackTrace);

        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
            corrotinaAtual = null;
        }

        aguardandoInput = false;
        dialogoAtivo = false;
        ultimoFrameFechado = Time.frameCount;
        filaDeFalas.Clear();

        DefinirIconeVisivel(false);
        if (painelBalao != null) painelBalao.SetActive(false);
        if (componenteDeTexto != null) componenteDeTexto.text = "";

        aoFecharDialogo?.Invoke();
    }
}