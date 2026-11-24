using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Coloque este script na RAIZ do Canvas do inventário (Screen Space - Overlay).
/// Ele sobrevive às trocas de cena; os itens ficam guardados no EstadoDoJogo.
/// Dentro do inventário: clique no item para ver o modelo 3D girando; E abre o modelo interativo (inspeção).
/// </summary>
public class GerenciadorDeInventario : MonoBehaviour
{
    public static GerenciadorDeInventario Instancia;

    /// <summary>true enquanto o inventário está aberto. Use nos seus scripts para ignorar input.</summary>
    public static bool InventarioAberto => Instancia != null && Instancia.aberto;

    /// <summary>Disparado ao abrir (true) e ao fechar (false) o inventário.</summary>
    public static event System.Action<bool> AoMudarEstado;

    [Header("UI")]
    [Tooltip("Painel que abre e fecha (filho deste objeto)")]
    [SerializeField] private GameObject painelInventario;
    [Tooltip("Objeto com GridLayoutGroup onde os slots são criados")]
    [SerializeField] private Transform conteudoDosSlots;
    [SerializeField] private SlotInventarioUI prefabDoSlot;
    [Tooltip("Texto tipo 'Inventário vazio' (opcional)")]
    [SerializeField] private GameObject textoInventarioVazio;

    [Header("Detalhes do item (opcional)")]
    [SerializeField] private GameObject painelDetalhes;
    [SerializeField] private TMPro.TextMeshProUGUI textoNomeDetalhe;
    [SerializeField] private TMPro.TextMeshProUGUI textoDescricaoDetalhe;
    [SerializeField] private Button botaoInspecionar;

    [Header("Preview 3D (opcional)")]
    [Tooltip("RawImage onde o modelo 3D girando aparece")]
    [SerializeField] private RawImage imagemDoPreview;
    [SerializeField] private float velocidadeDeGiroGrausPorSegundo = 40f;
    [SerializeField] private float distanciaDaCameraDoPreview = 3.2f;
    [Tooltip("Aumente se o modelo aparecer escuro")]
    [SerializeField] private float intensidadeDaLuzDoPreview = 2f;
    [Tooltip("Lugar longe da cena onde o modelo do preview é montado")]
    [SerializeField] private Vector3 posicaoDoPreviewNoMundo = new Vector3(0f, -2000f, 0f);

    [Header("Configurações")]
    [SerializeField] private Key teclaDeAbrir = Key.I;
    [Tooltip("Tecla que abre o modelo interativo (inspeção) do item selecionado")]
    [SerializeField] private Key teclaDeInspecionar = Key.E;
    [Tooltip("Se ligado, o tempo do jogo para enquanto o inventário está aberto")]
    [SerializeField] private bool pausarTempoAoAbrir = false;
    [Tooltip("Pasta dentro de Assets/Resources onde ficam os ItemData")]
    [SerializeField] private string pastaDosItensEmResources = "Itens";

    [Header("Eventos (use para travar o jogador, por exemplo)")]
    public UnityEvent aoAbrirInventario;
    public UnityEvent aoFecharInventario;

    private const float TamanhoDoModeloNoPreview = 1.1f;

    private readonly Dictionary<string, ItemData> bancoDeItens = new Dictionary<string, ItemData>();
    private ItemData itemSelecionado;
    private bool aberto;
    private int frameDeAbertura = -1;
    private bool voltarAoInventarioDepois;

    private CursorLockMode lockAnterior;
    private bool cursorVisivelAnterior;
    private float escalaDeTempoAnterior = 1f;

    // Preview 3D
    private Transform raizDoPreview;
    private Transform giradorDoPreview;
    private Transform paiInativoDoPreview;
    private Camera cameraDoPreview;
    private RenderTexture texturaDoPreview;
    private GameObject modeloDoPreview;

    public bool Aberto => aberto;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);

        CarregarBancoDeItens();

        if (painelInventario != null) painelInventario.SetActive(false);
        if (imagemDoPreview != null) imagemDoPreview.gameObject.SetActive(false);
        if (botaoInspecionar != null) botaoInspecionar.onClick.AddListener(InspecionarSelecionado);
        LimparDetalhes();

        EstadoDoJogo.AoMudarInventario += AoMudarInventario;
        SceneManager.sceneLoaded += AoCarregarCena;

        Debug.Log($"[Inventario] Gerenciador ativo. Aperte {teclaDeAbrir} para abrir.", this);
    }

    void OnDestroy()
    {
        if (Instancia != this) return;

        EstadoDoJogo.AoMudarInventario -= AoMudarInventario;
        SceneManager.sceneLoaded -= AoCarregarCena;

        if (texturaDoPreview != null)
        {
            texturaDoPreview.Release();
            Destroy(texturaDoPreview);
        }
        if (raizDoPreview != null) Destroy(raizDoPreview.gameObject);

        Instancia = null;
    }

    void Update()
    {
        // O modelo do preview gira com tempo real, mesmo se o jogo estiver pausado
        if (aberto && modeloDoPreview != null && giradorDoPreview != null)
            giradorDoPreview.Rotate(Vector3.up, velocidadeDeGiroGrausPorSegundo * Time.unscaledDeltaTime, Space.World);

        if (Keyboard.current == null) return;

        bool apertouTecla = Keyboard.current[teclaDeAbrir].wasPressedThisFrame;

        if (aberto)
        {
            // Ignora o input no frame em que abriu (ex.: voltando da inspeção com E ou Esc)
            if (Time.frameCount <= frameDeAbertura) return;

            if (apertouTecla)
            {
                Fechar();
                return;
            }

            if (Keyboard.current[teclaDeInspecionar].wasPressedThisFrame)
                InspecionarSelecionado();

            return;
        }

        if (!apertouTecla) return;

        if (PodeAbrir())
            Abrir();
        else
            Debug.Log("[Inventario] Não abriu: há uma inspeção ou um diálogo ativo.", this);
    }

    // ---------- API PÚBLICA ----------

    /// <summary>Adiciona o item ao inventário. Retorna false se ele já estava lá.</summary>
    public bool Adicionar(ItemData item)
    {
        if (item == null) return false;

        // garante que o item aparece na UI mesmo que o asset esteja fora da pasta de Resources
        bancoDeItens[item.Id] = item;
        return EstadoDoJogo.AdicionarItem(item.Id);
    }

    public bool Remover(ItemData item) => item != null && EstadoDoJogo.RemoverItem(item.Id);

    public bool Tem(ItemData item) => item != null && EstadoDoJogo.TemItem(item.Id);

    public void Abrir()
    {
        if (aberto) return;

        if (painelInventario == null)
        {
            Debug.LogWarning("[Inventario] O campo 'Painel Inventario' está vazio no Inspector do GerenciadorDeInventario.", this);
            return;
        }

        aberto = true;
        frameDeAbertura = Time.frameCount;

        lockAnterior = Cursor.lockState;
        cursorVisivelAnterior = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pausarTempoAoAbrir)
        {
            escalaDeTempoAnterior = Time.timeScale;
            Time.timeScale = 0f;
        }

        painelInventario.SetActive(true);
        LimparDetalhes();
        AtualizarSlots();

        aoAbrirInventario?.Invoke();
        AoMudarEstado?.Invoke(true);
    }

    public void Fechar()
    {
        if (!aberto) return;
        aberto = false;

        LimparPreview();
        if (painelInventario != null) painelInventario.SetActive(false);

        Cursor.lockState = lockAnterior;
        Cursor.visible = cursorVisivelAnterior;

        if (pausarTempoAoAbrir) Time.timeScale = escalaDeTempoAnterior;

        aoFecharInventario?.Invoke();
        AoMudarEstado?.Invoke(false);
    }

    /// <summary>Fecha o inventário e abre o item no modelo interativo (inspeção). Ao sair, volta para o inventário.</summary>
    public void InspecionarSelecionado()
    {
        if (itemSelecionado == null || itemSelecionado.modelo3D == null) return;

        GerenciadorDeInspecao inspecao = GerenciadorDeInspecao.Instancia;
        if (inspecao == null) return;

        ItemData item = itemSelecionado;
        Fechar();

        voltarAoInventarioDepois = true;
        inspecao.aoTerminarInspecao.RemoveListener(AoTerminarInspecaoDoInventario);
        inspecao.aoTerminarInspecao.AddListener(AoTerminarInspecaoDoInventario);

        string texto = string.IsNullOrEmpty(item.textoDetalhado) ? item.descricaoCurta : item.textoDetalhado;
        // Se o item tem rotação própria para a inspeção, usa ela; senão usa a mesma do preview
        Vector3 rotacaoDaInspecao = item.usarRotacaoPropriaNaInspecao
            ? item.rotacaoNaInspecaoGraus
            : item.rotacaoInicialGraus;

        inspecao.IniciarInspecao(
            item.modelo3D,
            Quaternion.Euler(rotacaoDaInspecao),
            texto,
            item.posicaoInicialInspecao,
            item.escalaInspecao
        );

        // Se a inspeção não começou, não deixa o "voltar" pendurado
        if (!inspecao.EstaInspecionando)
        {
            voltarAoInventarioDepois = false;
            inspecao.aoTerminarInspecao.RemoveListener(AoTerminarInspecaoDoInventario);
        }
    }

    // ---------- INTERNO ----------

    private void AoTerminarInspecaoDoInventario()
    {
        if (GerenciadorDeInspecao.Instancia != null)
            GerenciadorDeInspecao.Instancia.aoTerminarInspecao.RemoveListener(AoTerminarInspecaoDoInventario);

        if (!voltarAoInventarioDepois) return;
        voltarAoInventarioDepois = false;

        Abrir();
    }

    private bool PodeAbrir()
    {
        if (GerenciadorDeInspecao.Instancia != null && GerenciadorDeInspecao.Instancia.EstaInspecionando) return false;
        if (GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo) return false;
        return true;
    }

    private void CarregarBancoDeItens()
    {
        bancoDeItens.Clear();
        foreach (var item in Resources.LoadAll<ItemData>(pastaDosItensEmResources))
        {
            if (!bancoDeItens.ContainsKey(item.Id))
                bancoDeItens.Add(item.Id, item);
            else
                Debug.LogWarning($"[Inventario] ID repetido: '{item.Id}' em {item.name}.", item);
        }
    }

    private void AoMudarInventario()
    {
        if (!aberto) return;

        // se o item selecionado saiu do inventário, limpa os detalhes
        if (itemSelecionado != null && !EstadoDoJogo.TemItem(itemSelecionado.Id))
            LimparDetalhes();

        AtualizarSlots();
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        // evita ficar com o inventário aberto (ou o tempo parado) depois de trocar de cena
        if (aberto) Fechar();
    }

    private void AtualizarSlots()
    {
        if (conteudoDosSlots == null || prefabDoSlot == null) return;

        foreach (Transform filho in conteudoDosSlots)
            Destroy(filho.gameObject);

        int quantidade = 0;
        ItemData primeiro = null;

        foreach (string id in EstadoDoJogo.ItensColetados)
        {
            if (!bancoDeItens.TryGetValue(id, out ItemData item))
            {
                Debug.LogWarning($"[Inventario] Não achei o ItemData '{id}'. Ele está em Resources/{pastaDosItensEmResources}?");
                continue;
            }

            SlotInventarioUI slot = Instantiate(prefabDoSlot, conteudoDosSlots);
            slot.Configurar(item, Selecionar);
            if (primeiro == null) primeiro = item;
            quantidade++;
        }

        if (textoInventarioVazio != null) textoInventarioVazio.SetActive(quantidade == 0);

        // Ao abrir, já mostra o primeiro item
        if (itemSelecionado == null && primeiro != null)
            Selecionar(primeiro);
    }

    private void Selecionar(ItemData item)
    {
        itemSelecionado = item;

        if (painelDetalhes != null) painelDetalhes.SetActive(true);
        if (textoNomeDetalhe != null) textoNomeDetalhe.text = item.Nome;
        if (textoDescricaoDetalhe != null) textoDescricaoDetalhe.text = item.descricaoCurta;
        if (botaoInspecionar != null) botaoInspecionar.gameObject.SetActive(item.modelo3D != null);

        MostrarPreview(item);
    }

    private void LimparDetalhes()
    {
        itemSelecionado = null;
        if (painelDetalhes != null) painelDetalhes.SetActive(false);
        LimparPreview();
    }

    // ---------- PREVIEW 3D ----------

    private bool GarantirRigDePreview()
    {
        if (imagemDoPreview == null) return false;
        if (raizDoPreview != null) return true;

        var raiz = new GameObject("PreviewDoInventario");
        raiz.transform.position = posicaoDoPreviewNoMundo;
        DontDestroyOnLoad(raiz);
        raizDoPreview = raiz.transform;

        texturaDoPreview = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
        {
            name = "PreviewDoInventarioRT",
            antiAliasing = 4
        };
        imagemDoPreview.texture = texturaDoPreview;

        // Câmera que só filma o modelo, lá longe da cena
        var goCamera = new GameObject("CameraDoPreview");
        goCamera.transform.SetParent(raizDoPreview, false);
        goCamera.transform.localPosition = new Vector3(0f, 0f, -distanciaDaCameraDoPreview);
        cameraDoPreview = goCamera.AddComponent<Camera>();
        cameraDoPreview.clearFlags = CameraClearFlags.SolidColor;
        cameraDoPreview.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cameraDoPreview.fieldOfView = 30f;
        cameraDoPreview.nearClipPlane = 0.1f;
        cameraDoPreview.farClipPlane = 20f;
        cameraDoPreview.targetTexture = texturaDoPreview;
        cameraDoPreview.enabled = false;

        // Luzes de ponto: só iluminam o que está perto delas, então não mexem na cena
        CriarLuzDoPreview("LuzPrincipal", new Vector3(2f, 2f, -3f), intensidadeDaLuzDoPreview);
        CriarLuzDoPreview("LuzDeFundo", new Vector3(-3f, 0.5f, -2f), intensidadeDaLuzDoPreview * 0.5f);

        giradorDoPreview = new GameObject("Girador").transform;
        giradorDoPreview.SetParent(raizDoPreview, false);

        // Pai desativado: o modelo é limpo antes de "acordar"
        var inativo = new GameObject("ModeloInativo");
        inativo.transform.SetParent(raizDoPreview, false);
        inativo.SetActive(false);
        paiInativoDoPreview = inativo.transform;

        return true;
    }

    private void CriarLuzDoPreview(string nome, Vector3 posicaoLocal, float intensidade)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(raizDoPreview, false);
        go.transform.localPosition = posicaoLocal;

        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 15f;
        luz.intensity = intensidade;
        luz.shadows = LightShadows.None;
    }

    private void MostrarPreview(ItemData item)
    {
        LimparPreview();

        if (imagemDoPreview == null) return;

        if (item == null || item.modelo3D == null)
        {
            imagemDoPreview.gameObject.SetActive(false);
            return;
        }

        if (!GarantirRigDePreview()) return;

        // Instancia dentro de um pai desativado, remove scripts/colliders e só então ativa
        GameObject modelo = Instantiate(item.modelo3D, paiInativoDoPreview);
        PrepararModeloDePreview(modelo);

        giradorDoPreview.localRotation = Quaternion.identity;
        giradorDoPreview.localScale = Vector3.one;

        modelo.transform.SetParent(giradorDoPreview, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.Euler(item.rotacaoInicialGraus);

        Renderer[] renderers = modelo.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Destroy(modelo);
            imagemDoPreview.gameObject.SetActive(false);
            return;
        }

        // Centraliza o modelo e ajusta o tamanho para caber no quadro
        Bounds limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            limites.Encapsulate(renderers[i].bounds);

        Vector3 centroLocal = giradorDoPreview.InverseTransformPoint(limites.center);
        modelo.transform.localPosition -= centroLocal;

        float maior = Mathf.Max(limites.size.x, limites.size.y, limites.size.z);
        if (maior > 0.0001f)
            giradorDoPreview.localScale = Vector3.one * (TamanhoDoModeloNoPreview / maior);

        modeloDoPreview = modelo;
        imagemDoPreview.gameObject.SetActive(true);
        cameraDoPreview.enabled = true;
    }

    private void LimparPreview()
    {
        if (modeloDoPreview != null)
        {
            Destroy(modeloDoPreview);
            modeloDoPreview = null;
        }

        if (cameraDoPreview != null) cameraDoPreview.enabled = false;
    }

    /// <summary>Deixa só a parte visual: sem scripts, colliders, física nem áudio.</summary>
    private static void PrepararModeloDePreview(GameObject modelo)
    {
        MonoBehaviour[] scripts = modelo.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = scripts.Length - 1; i >= 0; i--)
            if (scripts[i] != null) DestroyImmediate(scripts[i]);

        foreach (Joint j in modelo.GetComponentsInChildren<Joint>(true)) DestroyImmediate(j);
        foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        foreach (Rigidbody rb in modelo.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
        foreach (AudioSource a in modelo.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(a);
    }
}