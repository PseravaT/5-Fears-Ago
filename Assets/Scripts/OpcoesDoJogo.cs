using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Coloque no painel de Opções do menu. Controla o volume geral, a resolução e o modo de tela.
/// Os valores são salvos em PlayerPrefs e aplicados sozinhos toda vez que o jogo abre,
/// mesmo que o painel nunca seja aberto.
/// </summary>
public class OpcoesDoJogo : MonoBehaviour
{
    private const string ChaveVolume = "opcao_volume";
    private const string ChaveTelaCheia = "opcao_tela_cheia";
    private const string ChaveResLargura = "opcao_res_largura";
    private const string ChaveResAltura = "opcao_res_altura";

    // Se quiser que a resolução escolhida também valha em tela cheia, troque por FullScreenMode.ExclusiveFullScreen
    private const FullScreenMode ModoTelaCheia = FullScreenMode.FullScreenWindow;

    private static readonly string[] NomesDoModo = { "Janela", "Tela cheia" };

    [Header("Volume")]
    [Tooltip("Slider do volume geral (o script ajusta de 0 a 1)")]
    [SerializeField] private Slider sliderVolume;
    [Tooltip("Texto opcional que mostra o valor, ex.: 80%")]
    [SerializeField] private TMP_Text textoVolume;
    [Tooltip("Ligue só se a barra do slider não aparece (altura negativa). Recalcula o tamanho do fundo, da barra e da bolinha ao abrir.")]
    [SerializeField] private bool corrigirGeometriaDoSlider = false;

    [Header("Tela")]
    [SerializeField] private SeletorDeOpcao seletorResolucao;
    [SerializeField] private SeletorDeOpcao seletorModoDeTela;

    private readonly List<Vector2Int> resolucoes = new List<Vector2Int>();

    // Aplica as opções salvas assim que o jogo inicia
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AplicarOpcoesSalvas()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(ChaveVolume, 1f);

        // A cada cena nova, reaplica o volume salvo (caso outro script tenha mexido)
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;

        bool temModo = PlayerPrefs.HasKey(ChaveTelaCheia);
        bool temResolucao = PlayerPrefs.HasKey(ChaveResLargura) && PlayerPrefs.HasKey(ChaveResAltura);
        if (!temModo && !temResolucao) return;

        bool cheia = temModo ? PlayerPrefs.GetInt(ChaveTelaCheia, 1) == 1 : Screen.fullScreen;
        int largura = temResolucao ? PlayerPrefs.GetInt(ChaveResLargura) : Screen.width;
        int altura = temResolucao ? PlayerPrefs.GetInt(ChaveResAltura) : Screen.height;

        Screen.SetResolution(largura, altura, cheia ? ModoTelaCheia : FullScreenMode.Windowed);
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        AudioListener.volume = PlayerPrefs.GetFloat(ChaveVolume, 1f);
        ObedecerVolumeGeral();
    }

    /// <summary>Faz todos os AudioSources da cena respeitarem o volume geral.</summary>
    private static void ObedecerVolumeGeral()
    {
        foreach (AudioSource fonte in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fonte.ignoreListenerVolume)
                fonte.ignoreListenerVolume = false;
        }
    }

    void Awake()
    {
        if (sliderVolume != null)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.wholeNumbers = false;
            sliderVolume.onValueChanged.AddListener(DefinirVolume);
        }

        if (seletorResolucao != null) seletorResolucao.AoMudar += AoMudarResolucao;
        if (seletorModoDeTela != null) seletorModoDeTela.AoMudar += AoMudarModo;
    }

    void OnEnable()
    {
        if (corrigirGeometriaDoSlider || GeometriaDoSliderQuebrada()) ArrumarGeometriaDoSlider();

        // Mostra os valores atuais sem disparar os eventos (que salvariam de novo à toa)
        float volume = PlayerPrefs.GetFloat(ChaveVolume, 1f);
        if (sliderVolume != null) sliderVolume.SetValueWithoutNotify(volume);
        AtualizarTexto(volume);

        // Garante que os sons da cena obedecem o volume geral
        ObedecerVolumeGeral();

        AtualizarSeletores();
    }

    void OnDisable()
    {
        // Grava em disco ao fechar o painel (e não a cada movimento do slider)
        PlayerPrefs.Save();
    }

    // ---------- VOLUME ----------

    private void DefinirVolume(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(ChaveVolume, valor);
        AtualizarTexto(valor);
    }

    private void AtualizarTexto(float valor)
    {
        if (textoVolume != null) textoVolume.text = Mathf.RoundToInt(valor * 100f) + "%";
    }

    // ---------- GEOMETRIA DO SLIDER ----------

    private static void Definir(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    /// <summary>True se alguma parte do slider tem altura ou largura zero/negativa (fica invisível).</summary>
    private bool GeometriaDoSliderQuebrada()
    {
        if (sliderVolume == null) return false;

        RectTransform[] partes =
        {
            sliderVolume.transform.Find("Background") as RectTransform,
            sliderVolume.fillRect,
            sliderVolume.handleRect
        };

        foreach (RectTransform parte in partes)
        {
            if (parte == null) continue;
            if (parte.rect.height <= 0f || parte.rect.width < 0f) return true;
        }
        return false;
    }

    /// <summary>
    /// Refaz o tamanho de cada parte do slider com valores fixos e seguros, para a barra não ficar
    /// com altura negativa (invisível). Ocupa a altura toda da linha; a barra fica no meio.
    /// </summary>
    private void ArrumarGeometriaDoSlider()
    {
        if (sliderVolume == null) return;

        // O slider ocupa a faixa entre o nome "Volume" e a porcentagem, na altura toda da linha
        Definir((RectTransform)sliderVolume.transform, 0.5124f, 0f, 0.8333f, 1f, Vector2.zero, Vector2.zero);

        // Fundo da barra
        Definir(sliderVolume.transform.Find("Background") as RectTransform, 0f, 0.35f, 1f, 0.65f, Vector2.zero, Vector2.zero);

        // Parte vermelha (que enche conforme o volume)
        RectTransform fill = sliderVolume.fillRect;
        if (fill != null)
        {
            Definir(fill.parent as RectTransform, 0f, 0.35f, 1f, 0.65f, new Vector2(5f, 0f), new Vector2(-15f, 0f));
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);   // o próprio Slider ajusta o X conforme o valor
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(10f, 0f);
        }

        // Bolinha
        RectTransform handle = sliderVolume.handleRect;
        if (handle != null)
        {
            Definir(handle.parent as RectTransform, 0f, 0f, 1f, 1f, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            handle.anchorMin = new Vector2(0f, 0.15f);
            handle.anchorMax = new Vector2(0f, 0.85f);
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new Vector2(14f, 0f);
        }
    }

    // ---------- TELA ----------

    private void AtualizarSeletores()
    {
        resolucoes.Clear();
        resolucoes.AddRange(ListarResolucoes());

        if (seletorResolucao != null)
        {
            var nomes = new List<string>();
            foreach (Vector2Int r in resolucoes) nomes.Add(r.x + " x " + r.y);
            seletorResolucao.DefinirOpcoes(nomes, IndiceDaResolucaoAtual());
        }

        if (seletorModoDeTela != null)
            seletorModoDeTela.DefinirOpcoes(NomesDoModo, Screen.fullScreen ? 1 : 0);
    }

    // Resoluções mais conhecidas (pode editar/acrescentar à vontade)
    private static readonly Vector2Int[] ResolucoesComuns =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),
    };

    private static List<Vector2Int> ListarResolucoes()
    {
        // Maior resolução que o monitor aceita: não oferece nada acima dela
        int maxLargura = 0, maxAltura = 0;
        foreach (Resolution r in Screen.resolutions)
        {
            if (r.width > maxLargura) maxLargura = r.width;
            if (r.height > maxAltura) maxAltura = r.height;
        }

        var lista = new List<Vector2Int>();
        foreach (Vector2Int v in ResolucoesComuns)
        {
            bool cabe = maxLargura == 0 || (v.x <= maxLargura && v.y <= maxAltura);
            if (cabe) lista.Add(v);
        }

        // Garantia: se nada coube (monitor pequeno), usa a resolução atual para o seletor não ficar vazio
        if (lista.Count == 0)
            lista.Add(new Vector2Int(Screen.width, Screen.height));

        return lista;
    }

    private int IndiceDaResolucaoAtual()
    {
        int melhor = 0;
        long menorDiferenca = long.MaxValue;
        long areaAtual = (long)Screen.width * Screen.height;

        for (int i = 0; i < resolucoes.Count; i++)
        {
            long diferenca = System.Math.Abs((long)resolucoes[i].x * resolucoes[i].y - areaAtual);
            if (diferenca < menorDiferenca)
            {
                menorDiferenca = diferenca;
                melhor = i;
            }
        }
        return melhor;
    }

    private void AoMudarResolucao(int indice)
    {
        if (indice < 0 || indice >= resolucoes.Count) return;

        Vector2Int r = resolucoes[indice];
        PlayerPrefs.SetInt(ChaveResLargura, r.x);
        PlayerPrefs.SetInt(ChaveResAltura, r.y);

        Screen.SetResolution(r.x, r.y, Screen.fullScreen ? ModoTelaCheia : FullScreenMode.Windowed);
    }

    private void AoMudarModo(int indice)
    {
        bool cheia = indice == 1;
        PlayerPrefs.SetInt(ChaveTelaCheia, cheia ? 1 : 0);

        Vector2Int r = new Vector2Int(Screen.width, Screen.height);
        if (seletorResolucao != null && seletorResolucao.Indice < resolucoes.Count)
            r = resolucoes[seletorResolucao.Indice];

        Screen.SetResolution(r.x, r.y, cheia ? ModoTelaCheia : FullScreenMode.Windowed);
    }
}