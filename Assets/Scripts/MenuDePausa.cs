using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de pausa (Esc ou Start do controle). NÃO vai na pasta Editor.
/// Use Tools > Menu > Criar Menu de Pausa (Esc) para montar tudo na cena.
///
/// Pausa o jogo (Time.timeScale = 0), mostra o cursor e oferece: continuar, resolução, tela, volume,
/// salvar, reiniciar a cena, voltar ao menu principal e sair.
/// </summary>
public class MenuDePausa : MonoBehaviour
{
    /// <summary>True enquanto o jogo está pausado. Outros scripts (câmera, personagem) podem checar isto.</summary>
    public static bool Pausado { get; private set; }

    /// <summary>Outros scripts podem ligar isto para impedir a pausa (ex.: durante uma cutscene ou um puzzle que usa Esc).</summary>
    public static bool Bloqueado;

    [Header("Interface")]
    [Tooltip("Objeto com todo o visual da pausa (começa escondido)")]
    [SerializeField] private GameObject painel;
    [SerializeField] private Button botaoContinuar;
    [SerializeField] private Button botaoSalvar;
    [SerializeField] private Button botaoReiniciar;
    [SerializeField] private Button botaoMenu;
    [SerializeField] private Button botaoSair;
    [Tooltip("Texto pequeno para avisos, ex.: 'Jogo salvo.'")]
    [SerializeField] private TMP_Text textoStatus;
    [Tooltip("Item selecionado ao abrir (para teclado/controle)")]
    [SerializeField] private Selectable primeiroItem;

    [Header("Cenas")]
    [SerializeField] private string cenaMenu = "Menu";

    [Header("Comportamento")]
    [Tooltip("Pausa também o áudio (falas, ambiente) enquanto o menu está aberto")]
    [SerializeField] private bool pausarAudio = true;
    [Tooltip("Mostra e destrava o cursor enquanto pausado, e devolve ao estado anterior ao continuar")]
    [SerializeField] private bool controlarCursor = true;

    [Header("Eventos (opcional)")]
    [Tooltip("Ex.: desligar o script de olhar com o mouse")]
    public UnityEvent aoPausar;
    [Tooltip("Ex.: ligar de volta o script de olhar com o mouse")]
    public UnityEvent aoRetomar;

    private float escalaAntes = 1f;
    private bool cursorVisivelAntes;
    private CursorLockMode travaAntes;

    private Coroutine rotinaAviso;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Resetar()
    {
        Pausado = false;
        Bloqueado = false;
    }

    void Awake()
    {
        Pausado = false;
        GarantirEventSystem();
        if (painel != null) painel.SetActive(false);
        if (textoStatus != null) textoStatus.text = "";

        if (botaoContinuar) botaoContinuar.onClick.AddListener(Retomar);
        if (botaoSalvar) botaoSalvar.onClick.AddListener(Salvar);
        if (botaoReiniciar) botaoReiniciar.onClick.AddListener(Reiniciar);
        if (botaoMenu) botaoMenu.onClick.AddListener(IrParaOMenu);
        if (botaoSair) botaoSair.onClick.AddListener(SairDoJogo);
    }

    void Update()
    {
        // E confirma o item selecionado (igual ao Enter) enquanto a pausa está aberta
        if (Pausado) ConfirmarComTecla.Verificar();

        bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool start = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        if (!esc && !start) return;

        if (Pausado) Retomar();
        else Pausar();
    }

    void OnDestroy()
    {
        // Se a cena sumir com o jogo pausado, devolve o tempo e o áudio ao normal
        if (!Pausado) return;

        Pausado = false;
        Time.timeScale = escalaAntes;
        AudioListener.pause = false;
    }

    /// <summary>A UI só responde se a cena tiver um EventSystem. Se faltar, cria um com o módulo do Input System.</summary>
    private static void GarantirEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Debug.Log("[MenuDePausa] A cena não tinha EventSystem; criei um automaticamente.", go);
    }

    /// <summary>
    /// A inspeção usa Esc para sair. Enquanto ela está aberta, ou acabou de fechar neste mesmo frame,
    /// a pausa não abre (isso também vale para o botão Start do controle).
    /// </summary>
    private static bool InspecaoTemPrioridade()
    {
        GerenciadorDeInspecao inspecao = GerenciadorDeInspecao.Instancia;
        if (inspecao != null && (inspecao.EstaInspecionando || inspecao.FechouNesteFrame)) return true;

        // Inventário aberto: o Esc não faz nada, só o I fecha
        return GerenciadorDeInventario.InventarioAberto;
    }

    // ---------- PAUSAR / RETOMAR ----------

    public void Pausar()
    {
        if (Pausado || Bloqueado || InspecaoTemPrioridade()) return;
        Pausado = true;

        escalaAntes = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        if (pausarAudio) AudioListener.pause = true;

        if (controlarCursor)
        {
            cursorVisivelAntes = Cursor.visible;
            travaAntes = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (painel != null) painel.SetActive(true);
        if (textoStatus != null) textoStatus.text = "";
        SelecionarPrimeiro();

        aoPausar?.Invoke();
    }

    public void Retomar()
    {
        if (!Pausado) return;

        RestaurarEstado();
        aoRetomar?.Invoke();
    }

    /// <summary>Volta tempo, áudio, cursor e esconde o painel. Usado ao continuar e antes de trocar de cena.</summary>
    private void RestaurarEstado()
    {
        Pausado = false;
        Time.timeScale = escalaAntes;
        AudioListener.pause = false;

        if (controlarCursor)
        {
            Cursor.visible = cursorVisivelAntes;
            Cursor.lockState = travaAntes;
        }

        if (painel != null) painel.SetActive(false);
    }

    private void SelecionarPrimeiro()
    {
        if (EventSystem.current == null) return;

        Selectable alvo = primeiroItem != null ? primeiroItem : botaoContinuar;
        if (alvo != null) EventSystem.current.SetSelectedGameObject(alvo.gameObject);
    }

    // ---------- AÇÕES ----------

    private void Salvar()
    {
        // Puzzles e inventário já são salvos na hora; aqui gravamos também a cena atual para o "Continuar"
        EstadoDoJogo.SalvarCena(SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        MostrarAviso("Jogo salvo (slot " + EstadoDoJogo.SlotAtual + ").");
    }

    private void Reiniciar()
    {
        string cena = SceneManager.GetActiveScene().name;
        RestaurarEstado();
        SceneManager.LoadScene(cena);
    }

    private void IrParaOMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(cenaMenu))
        {
            MostrarAviso("Cena do menu não está no Build.");
            Debug.LogError($"[MenuDePausa] A cena '{cenaMenu}' não está nas Build Settings.", this);
            return;
        }

        RestaurarEstado();

        // O menu principal precisa do cursor livre
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(cenaMenu);
    }

    private void SairDoJogo()
    {
        RestaurarEstado();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- AVISO ----------

    private void MostrarAviso(string mensagem)
    {
        if (textoStatus == null) return;

        if (rotinaAviso != null) StopCoroutine(rotinaAviso);
        rotinaAviso = StartCoroutine(AvisoRotina(mensagem));
    }

    private System.Collections.IEnumerator AvisoRotina(string mensagem)
    {
        textoStatus.text = mensagem;
        yield return new WaitForSecondsRealtime(2f);
        textoStatus.text = "";
        rotinaAviso = null;
    }
}