using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// COLOQUE ESTE ARQUIVO EM UMA PASTA CHAMADA "Editor" (ex.: Assets/Editor/CriadorDeUIInventario.cs).
/// Depois use o menu Tools > Inventario > Criar UI do Inventário.
/// Ele cria o Canvas, o painel, a grade de slots, o painel de detalhes e o prefab do slot,
/// e já liga todos os campos do GerenciadorDeInventario.
/// </summary>
public static class CriadorDeUIInventario
{
    private const string PastaPrefabs = "Assets/Prefabs";
    private const string CaminhoPrefabSlot = PastaPrefabs + "/SlotInventario.prefab";
    private const string NomeDoDesfazer = "Criar UI do Inventário";

    [MenuItem("Tools/Inventario/Criar UI do Inventário")]
    private static void Criar()
    {
        TMP_FontAsset fonte = ProcurarFonteDoJogo();

        GerenciadorDeInventario gerenciador = ObterOuCriarGerenciador();
        if (gerenciador == null) return;

        GameObject raiz = gerenciador.gameObject;
        ConfigurarCanvas(raiz);

        // ---------- PAINEL PRINCIPAL (fundo escuro em tela cheia) ----------
        RectTransform painel = CriarRect("PainelInventario", raiz.transform);
        Esticar(painel);
        painel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        // ---------- JANELA ----------
        RectTransform janela = CriarRect("Janela", painel);
        Ancorar(janela, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 760f));
        janela.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.09f, 0.96f);

        TextMeshProUGUI titulo = CriarTexto("Titulo", janela, "INVENTÁRIO", 40f, FontStyles.Bold, TextAlignmentOptions.Left, fonte);
        Ancorar(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-80f, 60f));

        TextMeshProUGUI dicaFechar = CriarTexto("DicaFechar", janela, "I ou Esc: fechar        E: inspecionar item", 22f, FontStyles.Normal, TextAlignmentOptions.Center, fonte);
        dicaFechar.color = new Color(1f, 1f, 1f, 0.55f);
        Ancorar(dicaFechar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-80f, 36f));

        // ---------- ÁREA DOS SLOTS (com rolagem) ----------
        RectTransform areaSlots = CriarRect("AreaSlots", janela);
        areaSlots.anchorMin = new Vector2(0f, 0f);
        areaSlots.anchorMax = new Vector2(0.62f, 1f);
        areaSlots.offsetMin = new Vector2(40f, 70f);
        areaSlots.offsetMax = new Vector2(-20f, -100f);
        areaSlots.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);
        ScrollRect rolagem = areaSlots.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = CriarRect("Viewport", areaSlots);
        Esticar(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform conteudo = CriarRect("ConteudoDosSlots", viewport);
        Ancorar(conteudo, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

        GridLayoutGroup grade = conteudo.gameObject.AddComponent<GridLayoutGroup>();
        grade.cellSize = new Vector2(130f, 150f);
        grade.spacing = new Vector2(12f, 12f);
        grade.padding = new RectOffset(12, 12, 12, 12);
        grade.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grade.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter ajuste = conteudo.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rolagem.viewport = viewport;
        rolagem.content = conteudo;
        rolagem.horizontal = false;
        rolagem.vertical = true;
        rolagem.movementType = ScrollRect.MovementType.Clamped;
        rolagem.scrollSensitivity = 30f;

        TextMeshProUGUI textoVazio = CriarTexto("TextoInventarioVazio", areaSlots, "Inventário vazio", 28f, FontStyles.Normal, TextAlignmentOptions.Center, fonte);
        textoVazio.color = new Color(1f, 1f, 1f, 0.45f);
        Esticar(textoVazio.rectTransform);

        // ---------- PAINEL DE DETALHES ----------
        RectTransform detalhes = CriarRect("PainelDetalhes", janela);
        detalhes.anchorMin = new Vector2(0.62f, 0f);
        detalhes.anchorMax = new Vector2(1f, 1f);
        detalhes.offsetMin = new Vector2(20f, 70f);
        detalhes.offsetMax = new Vector2(-40f, -100f);
        detalhes.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

        TextMeshProUGUI nomeDetalhe = CriarTexto("NomeDetalhe", detalhes, "Nome do item", 34f, FontStyles.Bold, TextAlignmentOptions.TopLeft, fonte);
        Ancorar(nomeDetalhe.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-32f, 60f));

        // Preview 3D do item (o gerenciador desenha o modelo girando nesta imagem)
        RectTransform previewRect = CriarRect("PreviewModelo", detalhes);
        Ancorar(previewRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(260f, 260f));
        RawImage previewModelo = previewRect.gameObject.AddComponent<RawImage>();
        previewModelo.raycastTarget = false;
        previewModelo.gameObject.SetActive(false);

        TextMeshProUGUI descricaoDetalhe = CriarTexto("DescricaoDetalhe", detalhes, "Descrição do item", 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, fonte);
        descricaoDetalhe.rectTransform.anchorMin = Vector2.zero;
        descricaoDetalhe.rectTransform.anchorMax = Vector2.one;
        descricaoDetalhe.rectTransform.offsetMin = new Vector2(16f, 84f);
        descricaoDetalhe.rectTransform.offsetMax = new Vector2(-16f, -350f);

        Button botaoInspecionar = CriarBotaoInspecionar(detalhes, fonte);

        // ---------- PREFAB DO SLOT ----------
        SlotInventarioUI prefabDoSlot = CriarPrefabDoSlot(fonte);

        // ---------- LIGA TUDO NO GERENCIADOR ----------
        var so = new SerializedObject(gerenciador);
        so.FindProperty("painelInventario").objectReferenceValue = painel.gameObject;
        so.FindProperty("conteudoDosSlots").objectReferenceValue = conteudo;
        so.FindProperty("prefabDoSlot").objectReferenceValue = prefabDoSlot;
        so.FindProperty("textoInventarioVazio").objectReferenceValue = textoVazio.gameObject;
        so.FindProperty("painelDetalhes").objectReferenceValue = detalhes.gameObject;
        so.FindProperty("textoNomeDetalhe").objectReferenceValue = nomeDetalhe;
        so.FindProperty("textoDescricaoDetalhe").objectReferenceValue = descricaoDetalhe;
        so.FindProperty("botaoInspecionar").objectReferenceValue = botaoInspecionar;
        so.FindProperty("imagemDoPreview").objectReferenceValue = previewModelo;
        so.ApplyModifiedProperties();

        // O painel começa escondido (o gerenciador também faz isso ao iniciar)
        painel.gameObject.SetActive(false);

        GarantirEventSystem();

        Selection.activeGameObject = raiz;
        EditorGUIUtility.PingObject(raiz);
        EditorSceneManager.MarkSceneDirty(raiz.scene);

        Debug.Log("[Inventario] UI criada e ligada no GerenciadorDeInventario. Salve a cena (Ctrl+S) e dê Play.", raiz);
    }

    // ------------------------------------------------------------------
    // GERENCIADOR / CANVAS
    // ------------------------------------------------------------------

    private static GerenciadorDeInventario ObterOuCriarGerenciador()
    {
        var existente = Object.FindFirstObjectByType<GerenciadorDeInventario>(FindObjectsInactive.Include);

        if (existente != null)
        {
            var so = new SerializedObject(existente);
            var painelAtual = so.FindProperty("painelInventario").objectReferenceValue as GameObject;

            if (EhObjetoDedicado(existente.gameObject, painelAtual))
            {
                if (painelAtual != null)
                {
                    bool substituir = EditorUtility.DisplayDialog(
                        NomeDoDesfazer,
                        "Este gerenciador já tem um painel ligado. Substituir pela UI nova?",
                        "Substituir", "Cancelar");
                    if (!substituir) return null;

                    Undo.DestroyObjectImmediate(painelAtual);
                }
                return existente;
            }

            bool criarNovo = EditorUtility.DisplayDialog(
                NomeDoDesfazer,
                $"O GerenciadorDeInventario está em '{existente.name}', que tem outros objetos ou scripts " +
                "e por isso não pode persistir entre cenas sozinho.\n\n" +
                "Criar um Canvas novo chamado 'Inventario' e mover o gerenciador para lá?",
                "Criar", "Cancelar");
            if (!criarNovo) return null;

            Undo.DestroyObjectImmediate(existente); // remove só o componente
        }

        var raiz = new GameObject("Inventario", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(raiz, NomeDoDesfazer);
        return Undo.AddComponent<GerenciadorDeInventario>(raiz);
    }

    /// <summary>Um objeto "dedicado" é raiz da hierarquia e só tem o gerenciador, o Canvas e o painel.</summary>
    private static bool EhObjetoDedicado(GameObject go, GameObject painelAtual)
    {
        if (go.transform.parent != null) return false;

        foreach (Transform filho in go.transform)
        {
            if (filho.gameObject != painelAtual) return false;
        }

        foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            if (!(mb is GerenciadorDeInventario || mb is CanvasScaler || mb is GraphicRaycaster)) return false;
        }

        return true;
    }

    private static void ConfigurarCanvas(GameObject raiz)
    {
        // Só configura o que ainda não existe, para não mexer em um Canvas já ajustado
        if (!raiz.TryGetComponent(out Canvas canvas))
        {
            canvas = Undo.AddComponent<Canvas>(raiz);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        if (!raiz.TryGetComponent(out CanvasScaler scaler))
        {
            scaler = Undo.AddComponent<CanvasScaler>(raiz);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (!raiz.TryGetComponent(out GraphicRaycaster raycaster))
            Undo.AddComponent<GraphicRaycaster>(raiz);
    }

    private static void GarantirEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(es, NomeDoDesfazer);
    }

    // ------------------------------------------------------------------
    // BOTÃO E PREFAB DO SLOT
    // ------------------------------------------------------------------

    private static Button CriarBotaoInspecionar(Transform pai, TMP_FontAsset fonte)
    {
        RectTransform rect = CriarRect("BotaoInspecionar", pai);
        Ancorar(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(260f, 52f));

        Image fundo = rect.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.55f, 0.55f, 0.62f, 1f);

        Button botao = rect.gameObject.AddComponent<Button>();
        botao.targetGraphic = fundo;
        DefinirCores(botao,
            new Color(0.7f, 0.7f, 0.7f, 1f),
            Color.white,
            new Color(0.5f, 0.5f, 0.5f, 1f),
            new Color(0.7f, 0.7f, 0.7f, 1f));

        TextMeshProUGUI texto = CriarTexto("Texto", rect, "Inspecionar (E)", 26f, FontStyles.Normal, TextAlignmentOptions.Center, fonte);
        Esticar(texto.rectTransform);

        return botao;
    }

    private static SlotInventarioUI CriarPrefabDoSlot(TMP_FontAsset fonte)
    {
        if (!AssetDatabase.IsValidFolder(PastaPrefabs))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        RectTransform raiz = CriarRect("SlotInventario", null);
        raiz.sizeDelta = new Vector2(130f, 150f);

        Image fundo = raiz.gameObject.AddComponent<Image>();
        fundo.color = new Color(1f, 1f, 1f, 0.3f);

        Button botao = raiz.gameObject.AddComponent<Button>();
        botao.targetGraphic = fundo;
        DefinirCores(botao,
            new Color(1f, 1f, 1f, 0.5f),   // normal
            Color.white,                    // mouse por cima
            new Color(0.8f, 0.8f, 0.8f, 1f), // apertado
            Color.white);                   // selecionado (item escolhido)

        RectTransform iconeRect = CriarRect("Icone", raiz);
        iconeRect.anchorMin = Vector2.zero;
        iconeRect.anchorMax = Vector2.one;
        iconeRect.offsetMin = new Vector2(10f, 40f);
        iconeRect.offsetMax = new Vector2(-10f, -10f);
        Image icone = iconeRect.gameObject.AddComponent<Image>();
        icone.preserveAspect = true;
        icone.raycastTarget = false;

        TextMeshProUGUI nome = CriarTexto("Nome", raiz, "Item", 18f, FontStyles.Normal, TextAlignmentOptions.Center, fonte);
        Ancorar(nome.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-12f, 32f));

        SlotInventarioUI slot = raiz.gameObject.AddComponent<SlotInventarioUI>();
        var so = new SerializedObject(slot);
        so.FindProperty("botao").objectReferenceValue = botao;
        so.FindProperty("icone").objectReferenceValue = icone;
        so.FindProperty("textoNome").objectReferenceValue = nome;
        so.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz.gameObject, CaminhoPrefabSlot);
        Object.DestroyImmediate(raiz.gameObject);

        return prefab.GetComponent<SlotInventarioUI>();
    }

    private static void DefinirCores(Button botao, Color normal, Color mouse, Color apertado, Color selecionado)
    {
        ColorBlock cores = botao.colors;
        cores.normalColor = normal;
        cores.highlightedColor = mouse;
        cores.pressedColor = apertado;
        cores.selectedColor = selecionado;
        cores.colorMultiplier = 1f;
        cores.fadeDuration = 0.1f;
        botao.colors = cores;
    }

    // ------------------------------------------------------------------
    // AUXILIARES
    // ------------------------------------------------------------------

    /// <summary>Usa a mesma fonte do texto "pressione Q..." do sistema de inspeção, se ele existir na cena.</summary>
    private static TMP_FontAsset ProcurarFonteDoJogo()
    {
        var inspecao = Object.FindFirstObjectByType<GerenciadorDeInspecao>(FindObjectsInactive.Include);
        if (inspecao != null && inspecao.textoBotaoQ != null)
            return inspecao.textoBotaoQ.font;

        return null;
    }

    private static RectTransform CriarRect(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return go.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI CriarTexto(string nome, Transform pai, string conteudo, float tamanho,
        FontStyles estilo, TextAlignmentOptions alinhamento, TMP_FontAsset fonte)
    {
        RectTransform rect = CriarRect(nome, pai);
        var texto = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (fonte != null) texto.font = fonte;
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.alignment = alinhamento;
        texto.raycastTarget = false;
        return texto;
    }

    private static void Esticar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Ancorar(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivo, Vector2 posicao, Vector2 tamanho)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivo;
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
    }
}