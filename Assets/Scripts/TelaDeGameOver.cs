using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Tela de game over. NÃO vai na pasta Editor.
/// Use Tools > Menu > Criar Tela de Game Over para montar tudo no Canvas da cena.
///
/// Quando o DamageSystem termina de escurecer a tela (evento onGameOver), mostra uma animação
/// (vídeo ou sequência de imagens) e, depois de alguns segundos, os botões
/// "Tentar novamente" e "Voltar ao menu".
///
/// Este script precisa ficar num objeto que NUNCA começa desativado (o painel é que começa escondido).
/// O VideoPlayer também fica neste mesmo objeto.
/// </summary>
public class TelaDeGameOver : MonoBehaviour
{
    [Header("Interface")]
    [Tooltip("Objeto com todo o visual (começa escondido). Fica DENTRO deste objeto, nunca é ele mesmo")]
    [SerializeField] private GameObject painel;
    [Tooltip("CanvasGroup do painel, usado para o fade de entrada (opcional)")]
    [SerializeField] private CanvasGroup grupoDoPainel;
    [Tooltip("Objeto que contém os dois botões (aparece só depois do tempo configurado)")]
    [SerializeField] private GameObject areaDosBotoes;
    [SerializeField] private Button botaoTentarNovamente;
    [SerializeField] private Button botaoMenu;

    [Header("Animação em vídeo")]
    [Tooltip("VideoPlayer deste mesmo objeto")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Arraste o vídeo aqui (mp4 ou webm). Gif não funciona na Unity: converta para vídeo")]
    [SerializeField] private VideoClip videoClip;
    [Tooltip("RawImage que mostra o vídeo")]
    [SerializeField] private RawImage imagemDoVideo;
    [SerializeField] private bool repetirVideo = false;

    [Header("Animação em imagens (alternativa ao vídeo)")]
    [Tooltip("Image que mostra a sequência. Só é usada se a lista abaixo tiver sprites")]
    [SerializeField] private Image imagemDosQuadros;
    [SerializeField] private Sprite[] quadros;
    [SerializeField] private float quadrosPorSegundo = 12f;
    [SerializeField] private bool repetirQuadros = true;

    [Header("Tempos (segundos)")]
    [Tooltip("Tempo para o vídeo/animação aparecer aos poucos")]
    [SerializeField] private float duracaoDoFade = 1f;
    [Tooltip("Tempo, contado depois do fade, até os botões aparecerem")]
    [SerializeField] private float segundosAteOsBotoes = 3f;
    [Tooltip("Se ligado (e o vídeo não repete), os botões só aparecem quando o vídeo termina")]
    [SerializeField] private bool esperarFimDoVideo = false;

    [Header("Comportamento")]
    [SerializeField] private string cenaMenu = "Menu";
    [Tooltip("Cena carregada ao clicar em 'Tentar novamente', não importa onde o jogador morreu. Use o nome EXATO da cena da sala. Vazio = recarrega a cena atual")]
    [SerializeField] private string cenaDoRecomeco = "Casa";
    [Tooltip("Congela o jogo (Time.timeScale = 0) enquanto a tela está aberta. O áudio não é pausado")]
    [SerializeField] private bool congelarJogo = true;
    [Tooltip("A tecla E também confirma o botão selecionado (igual ao menu de pausa)")]
    [SerializeField] private bool confirmarComE = true;

    [Header("Eventos (opcional)")]
    [Tooltip("Ex.: desligar o script de olhar com o mouse")]
    public UnityEvent aoMostrar;

    private bool aberta;
    private bool saindo;
    private bool botoesVisiveis;
    private bool videoTerminou;
    private bool inscrito;

    private float escalaAntes = 1f;
    private bool cursorVisivelAntes;
    private CursorLockMode travaAntes;

    private RenderTexture texturaDoVideo;

    private void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();

        if (painel != null) painel.SetActive(false);

        if (botaoTentarNovamente != null) botaoTentarNovamente.onClick.AddListener(TentarNovamente);
        if (botaoMenu != null) botaoMenu.onClick.AddListener(VoltarAoMenu);
    }

    private void Start()
    {
        PrepararVideo();
        Inscrever();
    }

    private void Update()
    {
        if (botoesVisiveis && confirmarComE) ConfirmarComTecla.Verificar();
    }

    private void OnDestroy()
    {
        if (DamageSystem.Instancia != null)
            DamageSystem.Instancia.onGameOver.RemoveListener(Mostrar);

        if (videoPlayer != null) videoPlayer.loopPointReached -= AoTerminarVideo;

        // Se a cena sumir com a tela aberta, devolve o tempo ao normal
        if (aberta) DevolverEstado();

        if (texturaDoVideo != null)
        {
            texturaDoVideo.Release();
            Destroy(texturaDoVideo);
        }
    }

    // ---------- LIGAÇÃO COM O DAMAGESYSTEM ----------

    /// <summary>O DamageSystem dispara onGameOver quando a tela terminou de ficar preta.</summary>
    private void Inscrever()
    {
        if (inscrito) return;

        DamageSystem dano = DamageSystem.Instancia;
        if (dano == null)
        {
            Debug.LogWarning("[TelaDeGameOver] Não achei o DamageSystem. Ligue o evento 'On Game Over' dele em TelaDeGameOver.Mostrar.", this);
            return;
        }

        dano.onGameOver.RemoveListener(Mostrar);
        dano.onGameOver.AddListener(Mostrar);
        inscrito = true;
    }

    // ---------- VÍDEO ----------

    /// <summary>Prepara o vídeo desde o início da cena, para não haver atraso quando o jogador perder.</summary>
    private void PrepararVideo()
    {
        if (videoPlayer == null || videoClip == null) return;

        videoPlayer.playOnAwake = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = repetirVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.waitForFirstFrame = true;

        texturaDoVideo = new RenderTexture((int)videoClip.width, (int)videoClip.height, 0);
        texturaDoVideo.Create();
        videoPlayer.targetTexture = texturaDoVideo;

        if (imagemDoVideo != null)
        {
            imagemDoVideo.texture = texturaDoVideo;

            // Mantém a proporção do vídeo, sem esticar
            AspectRatioFitter ajuste = imagemDoVideo.GetComponent<AspectRatioFitter>();
            if (ajuste != null) ajuste.aspectRatio = (float)videoClip.width / videoClip.height;
        }

        videoPlayer.loopPointReached += AoTerminarVideo;
        videoPlayer.Prepare();
    }

    private void AoTerminarVideo(VideoPlayer origem)
    {
        videoTerminou = true;
    }

    private IEnumerator ToqueDoVideo()
    {
        if (videoPlayer == null || videoClip == null) yield break;

        float limite = 5f;
        while (!videoPlayer.isPrepared && limite > 0f)
        {
            limite -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("[TelaDeGameOver] O vídeo não ficou pronto a tempo.", this);
            yield break;
        }

        videoTerminou = false;
        if (imagemDoVideo != null) imagemDoVideo.gameObject.SetActive(true);
        videoPlayer.Play();
    }

    private IEnumerator ToqueDosQuadros()
    {
        if (imagemDosQuadros == null || quadros == null || quadros.Length == 0) yield break;

        imagemDosQuadros.gameObject.SetActive(true);
        float passo = 1f / Mathf.Max(1f, quadrosPorSegundo);
        int indice = 0;

        while (true)
        {
            imagemDosQuadros.sprite = quadros[indice];
            yield return new WaitForSecondsRealtime(passo);

            indice++;
            if (indice >= quadros.Length)
            {
                if (!repetirQuadros) yield break; // fica no último quadro
                indice = 0;
            }
        }
    }

    // ---------- MOSTRAR ----------

    /// <summary>Chamado pelo DamageSystem (onGameOver). Pode ser chamado também por um UnityEvent.</summary>
    public void Mostrar()
    {
        if (aberta) return;
        aberta = true;
        saindo = false;
        botoesVisiveis = false;

        // O menu de pausa não pode abrir por cima da tela de game over
        MenuDePausa.Bloqueado = true;

        if (congelarJogo)
        {
            escalaAntes = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        cursorVisivelAntes = Cursor.visible;
        travaAntes = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (grupoDoPainel != null) grupoDoPainel.alpha = 0f;
        if (areaDosBotoes != null) areaDosBotoes.SetActive(false);
        if (painel != null) painel.SetActive(true);

        aoMostrar?.Invoke();
        StartCoroutine(Sequencia());
    }

    private IEnumerator Sequencia()
    {
        StartCoroutine(ToqueDoVideo());
        StartCoroutine(ToqueDosQuadros());

        // Fade de entrada (tempo real: funciona com o jogo congelado)
        float t = 0f;
        while (grupoDoPainel != null && t < duracaoDoFade)
        {
            t += Time.unscaledDeltaTime;
            grupoDoPainel.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, duracaoDoFade));
            yield return null;
        }
        if (grupoDoPainel != null) grupoDoPainel.alpha = 1f;

        yield return new WaitForSecondsRealtime(segundosAteOsBotoes);

        if (esperarFimDoVideo && videoClip != null && !repetirVideo)
        {
            float limite = (float)videoClip.length + 3f;
            while (!videoTerminou && limite > 0f)
            {
                limite -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (areaDosBotoes != null) areaDosBotoes.SetActive(true);
        botoesVisiveis = true;
        SelecionarPrimeiro();
    }

    private void SelecionarPrimeiro()
    {
        if (EventSystem.current == null || botaoTentarNovamente == null) return;
        EventSystem.current.SetSelectedGameObject(botaoTentarNovamente.gameObject);
    }

    // ---------- AÇÕES ----------

    /// <summary>Zera o dano e carrega a cena de recomeço (a sala). O progresso (puzzles e itens) continua salvo.</summary>
    public void TentarNovamente()
    {
        if (!aberta || saindo) return;

        // Sem cena de recomeço configurada (ou fora do Build), recarrega a cena atual
        string cena = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrWhiteSpace(cenaDoRecomeco))
        {
            if (Application.CanStreamedLevelBeLoaded(cenaDoRecomeco))
            {
                cena = cenaDoRecomeco;
            }
            else
            {
                Debug.LogError($"[TelaDeGameOver] A cena '{cenaDoRecomeco}' não está nas Build Settings. Recarregando a cena atual.", this);
            }
        }

        saindo = true;

        ZerarDano();

        DevolverEstado();
        SceneManager.LoadScene(cena);
    }

    /// <summary>Zera o dano e os efeitos na hora. A escuridão continua clareando sozinha (fade-in).</summary>
    private static void ZerarDano()
    {
        DamageSystem dano = DamageSystem.Instancia;
        if (dano != null) dano.ResetDamageImediato();
    }

    public void VoltarAoMenu()
    {
        if (!aberta || saindo) return;

        if (!Application.CanStreamedLevelBeLoaded(cenaMenu))
        {
            Debug.LogError($"[TelaDeGameOver] A cena '{cenaMenu}' não está nas Build Settings.", this);
            return;
        }

        saindo = true;

        ZerarDano();

        DevolverEstado();

        // O menu principal precisa do cursor livre
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(cenaMenu);
    }

    /// <summary>Para a animação e devolve tempo, pausa e cursor ao que eram antes do game over.</summary>
    private void DevolverEstado()
    {
        aberta = false;
        botoesVisiveis = false;
        StopAllCoroutines();

        if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();

        if (congelarJogo) Time.timeScale = escalaAntes;
        MenuDePausa.Bloqueado = false;

        Cursor.visible = cursorVisivelAntes;
        Cursor.lockState = travaAntes;
    }

    [ContextMenu("Testar: mostrar tela de game over (só no Play)")]
    private void Testar()
    {
        if (Application.isPlaying) Mostrar();
    }
}