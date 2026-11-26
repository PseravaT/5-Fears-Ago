using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Exibe a tela final animada (vídeo .mp4/.webm ou sequência de Sprites) após a transição
/// do corredor-porão e, ao terminar, leva o jogador de volta para o Menu principal.
/// </summary>
public class TelaFinalDeJogo : MonoBehaviour
{
    [Header("Interface")]
    [Tooltip("Painel com o visual da tela final (começa desativado automaticamente).")]
    [SerializeField] private GameObject painel;
    [Tooltip("CanvasGroup opcional do painel para fazer o fade de entrada.")]
    [SerializeField] private CanvasGroup grupoDoPainel;
    [SerializeField] private float duracaoDoFade = 1f;

    [Header("Animação em Vídeo (MP4 / WebM)")]
    [Tooltip("Obs.: a Unity não toca .gif diretamente; converta o GIF para .mp4/.webm ou use a lista de Sprites abaixo.")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private RawImage imagemDoVideo;
    [SerializeField] private bool repetirVideo = false;

    [Header("Animação em Quadros (alternativa ao vídeo)")]
    [SerializeField] private Image imagemDosQuadros;
    [SerializeField] private Sprite[] quadros;
    [SerializeField] private float quadrosPorSegundo = 12f;
    [SerializeField] private bool repetirQuadros = false;

    [Header("Saída para o Menu")]
    [SerializeField] private string cenaMenu = "Menu";
    [Tooltip("Tempo mínimo (em segundos) que a tela final fica visível antes de ir para o Menu.")]
    [SerializeField] private float duracaoMinimaSegundos = 4f;
    [Tooltip("Se marcado e houver vídeo (sem repetir), aguarda o vídeo terminar antes de ir para o Menu.")]
    [SerializeField] private bool esperarFimDoVideo = true;
    [Tooltip("Permite pular para o Menu apertando E, Espaço ou Esc depois do fade inicial.")]
    [SerializeField] private bool permitirPularComTecla = true;

    private bool exibindo;
    private bool saindo;
    private bool podePular;
    private bool videoTerminou;
    private RenderTexture texturaDoVideo;

    private void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (painel != null) painel.SetActive(false);
    }

    private void Start()
    {
        PrepararVideo();
    }

    private void Update()
    {
        if (!exibindo || saindo || !podePular || !permitirPularComTecla || Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            IrParaOMenu();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= AoTerminarVideo;

        if (texturaDoVideo != null)
        {
            texturaDoVideo.Release();
            Destroy(texturaDoVideo);
            texturaDoVideo = null;
        }
    }

    /// <summary>Chame no evento aoTerminarEspera do CarregadorDeTransicao.</summary>
    public void Mostrar()
    {
        if (exibindo) return;
        exibindo = true;
        saindo = false;
        podePular = false;
        videoTerminou = false;

        Time.timeScale = 1f;

        if (painel != null) painel.SetActive(true);

        if (grupoDoPainel != null)
            grupoDoPainel.alpha = duracaoDoFade > 0f ? 0f : 1f;

        bool usarQuadros = quadros != null && quadros.Length > 0 && imagemDosQuadros != null;
        bool usarVideo = !usarQuadros && videoPlayer != null && videoClip != null;

        if (imagemDosQuadros != null) imagemDosQuadros.gameObject.SetActive(usarQuadros);
        if (imagemDoVideo != null) imagemDoVideo.gameObject.SetActive(usarVideo);

        if (usarQuadros)
            StartCoroutine(ReproduzirQuadros());
        else if (usarVideo)
            videoPlayer.Play();

        StartCoroutine(FluxoTelaFinal(usarVideo));
    }

    private void PrepararVideo()
    {
        if (videoPlayer == null || videoClip == null || imagemDoVideo == null) return;

        int largura = videoClip.width > 0 ? (int)videoClip.width : 1920;
        int altura = videoClip.height > 0 ? (int)videoClip.height : 1080;

        texturaDoVideo = new RenderTexture(largura, altura, 0);
        texturaDoVideo.Create();

        videoPlayer.playOnAwake = false;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = repetirVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = texturaDoVideo;
        imagemDoVideo.texture = texturaDoVideo;

        videoPlayer.loopPointReached += AoTerminarVideo;
        videoPlayer.Prepare();
    }

    private void AoTerminarVideo(VideoPlayer vp)
    {
        videoTerminou = true;
    }

    private IEnumerator ReproduzirQuadros()
    {
        float intervalo = 1f / Mathf.Max(1f, quadrosPorSegundo);
        int indice = 0;

        while (exibindo && !saindo)
        {
            imagemDosQuadros.sprite = quadros[indice];
            yield return new WaitForSecondsRealtime(intervalo);

            indice++;
            if (indice >= quadros.Length)
            {
                if (!repetirQuadros) break;
                indice = 0;
            }
        }
    }

    private IEnumerator FluxoTelaFinal(bool usarVideo)
    {
        if (grupoDoPainel != null && duracaoDoFade > 0f)
        {
            float tempo = 0f;
            while (tempo < duracaoDoFade)
            {
                tempo += Time.unscaledDeltaTime;
                grupoDoPainel.alpha = Mathf.Clamp01(tempo / duracaoDoFade);
                yield return null;
            }
            grupoDoPainel.alpha = 1f;
        }

        podePular = true;

        float restante = Mathf.Max(0f, duracaoMinimaSegundos);
        while (restante > 0f && !saindo)
        {
            restante -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (usarVideo && esperarFimDoVideo && !repetirVideo)
        {
            while (!videoTerminou && !saindo)
                yield return null;
        }

        IrParaOMenu();
    }

    public void IrParaOMenu()
    {
        if (saindo) return;
        saindo = true;
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(cenaMenu))
        {
            Debug.LogError("[TelaFinalDeJogo] Nome da cena de Menu está vazio!", this);
            return;
        }

        if (EfeitoFade.Instancia != null)
            EfeitoFade.Instancia.MudarCenaComFade(cenaMenu);
        else
            SceneManager.LoadScene(cenaMenu);
    }
}
