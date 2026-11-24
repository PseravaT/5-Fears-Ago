using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// COLOQUE ESTE ARQUIVO NA PASTA "Editor" (ex.: Assets/Scripts/Editor).
/// ABRA A CENA DO MENU e use Tools > Menu > Criar Menu de Pausa (Esc).
///
/// O menu de pausa copia o estilo do menu principal: fonte, cores e comportamento do MenuItemHUD,
/// imagem da moldura, fundo/borda/seta dos botões e resolução do Canvas.
/// O resultado é salvo como prefab em Assets/Prefabs/MenuDePausa.prefab (a cópia temporária na cena do Menu é apagada).
/// Depois é só arrastar o prefab nas cenas de jogo.
/// </summary>
public static class CriadorDoMenuDePausa
{
    private const string PastaPrefab = "Assets/Prefabs";
    private const string CaminhoPrefab = "Assets/Prefabs/MenuDePausa.prefab";

    // Dentro das linhas de configuração (frações da largura da linha)
    private const float FimDoRotulo = 0.46f;
    private const float InicioControle = 0.50f;
    private const float FimControle = 0.96f;
    private const float FimSlider = 0.78f;
    private const float InicioPorcentagem = 0.82f;

    /// <summary>Tudo o que é copiado do menu principal.</summary>
    private class Estilo
    {
        public MenuItemHUD hud;          // cores e comportamento dos itens
        public Image fundo;              // fundo do botão do menu
        public Graphic borda;            // borda do botão do menu
        public Graphic seta;             // seta do botão do menu
        public TMP_Text rotulo;          // texto do botão do menu
        public TMP_Text titulo;          // título do menu
        public Image moldura;            // imagem do quadro do menu
        public float altura;             // altura de um botão
        public float larguraMoldura;     // largura do quadro
        public Vector2 referencia = new Vector2(1920f, 1080f);
        public CanvasScaler.ScreenMatchMode modoMatch = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        public float match = 0.5f;
    }

    private class Linha
    {
        public GameObject go;
        public Image borda;
        public Image fundo;
        public TMP_Text rotulo;
        public Graphic seta;
    }

    private static Estilo e;

    [MenuItem("Tools/Menu/Criar Menu de Pausa (Esc)")]
    private static void Criar()
    {
        // "global::" porque o Editor também tem uma classe UnityEditor.Menu
        var menu = Object.FindFirstObjectByType<global::Menu>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Aviso("Abra a cena do Menu e rode de novo: o menu de pausa copia o estilo (fonte, cores, moldura e botões) do menu principal.");
            return;
        }

        if (menu.grupoMenu == null)
        {
            Aviso("O campo 'Grupo Menu' do componente Menu está vazio. Arraste para ele o painel do menu (o que tem o CanvasGroup).");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab) != null)
        {
            bool substituir = EditorUtility.DisplayDialog(
                "Criar Menu de Pausa",
                "Já existe " + CaminhoPrefab + ". Substituir pelo novo (com o estilo do menu)? As cenas que usam o prefab são atualizadas junto.",
                "Substituir", "Cancelar");
            if (!substituir) return;
        }

        string erro;
        e = LerEstilo(menu, out erro);
        if (e == null)
        {
            Aviso(erro);
            return;
        }

        // ---------- MEDIDAS ----------
        float altura = e.altura;
        float espaco = altura * 0.22f;
        float margemV = altura * 0.8f;
        float margemH = altura * 0.9f;
        float alturaTitulo = e.titulo != null ? e.titulo.fontSize * 1.5f : altura * 1.4f;
        float alturaStatus = altura * 0.55f;
        const int linhasDeControle = 8;

        float estimativa = margemV * 2f + alturaTitulo + 2f + linhasDeControle * altura + alturaStatus + (linhasDeControle + 2) * espaco;

        // Se não couber na tela, diminui tudo por igual aumentando a resolução de referência
        Vector2 referencia = e.referencia;
        if (estimativa > referencia.y * 0.9f)
            referencia /= (referencia.y * 0.9f) / estimativa;

        float larguraCaixa = Mathf.Max(e.larguraMoldura * 1.5f, altura * 13f);
        larguraCaixa = Mathf.Min(larguraCaixa, referencia.x * 0.75f);

        // ---------- CANVAS (cópia temporária na cena; vira prefab no final) ----------
        var raiz = new GameObject("MenuDePausa", typeof(RectTransform));

        var canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = raiz.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referencia;
        scaler.screenMatchMode = e.modoMatch;
        scaler.matchWidthOrHeight = e.match;

        raiz.AddComponent<GraphicRaycaster>();
        var pausa = raiz.AddComponent<MenuDePausa>();

        // ---------- PAINEL (tela escurecida; é o que liga/desliga) ----------
        RectTransform painel = NovoUI("Painel", raiz.transform);
        Esticar(painel);
        painel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var opcoes = painel.gameObject.AddComponent<OpcoesDoJogo>();

        // ---------- CAIXA CENTRAL (com a imagem da moldura do menu) ----------
        RectTransform caixa = NovoUI("Caixa", painel);
        caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
        caixa.pivot = new Vector2(0.5f, 0.5f);
        caixa.sizeDelta = new Vector2(larguraCaixa, 0f);

        Image imagemCaixa = caixa.gameObject.AddComponent<Image>();
        if (e.moldura != null)
        {
            EditorUtility.CopySerialized(e.moldura, imagemCaixa);
            imagemCaixa.raycastTarget = true;
        }
        else
        {
            imagemCaixa.color = new Color(0.05f, 0.07f, 0.06f, 0.95f);
        }

        var layout = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(Mathf.RoundToInt(margemH), Mathf.RoundToInt(margemH), Mathf.RoundToInt(margemV), Mathf.RoundToInt(margemV));
        layout.spacing = espaco;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var ajuste = caixa.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Título e divisória
        TMP_Text titulo = NovoTexto(caixa, "Titulo", "PAUSA", e.titulo, 1f);
        titulo.alignment = TextAlignmentOptions.Center;
        DefinirAltura(titulo.gameObject, alturaTitulo);
        ReplicarCamadasDoTitulo(titulo, "PAUSA");

        RectTransform divisoria = NovoUI("Divisoria", caixa);
        divisoria.gameObject.AddComponent<Image>().color = Cor("corBordaNormal", new Color(0.44f, 0.66f, 0.54f, 0.55f));
        DefinirAltura(divisoria.gameObject, 2f);

        // ---------- LINHAS ----------
        Button botaoContinuar = CriarLinhaDeBotao(caixa, "BotaoContinuar", "Continuar");
        SeletorDeOpcao seletorResolucao = CriarLinhaDeSeletor(caixa, "Resolução");
        SeletorDeOpcao seletorTela = CriarLinhaDeSeletor(caixa, "Tela");

        Slider slider;
        TMP_Text textoPorcentagem;
        CriarLinhaDeVolume(caixa, out slider, out textoPorcentagem);

        Button botaoSalvar = CriarLinhaDeBotao(caixa, "BotaoSalvar", "Salvar jogo");
        Button botaoReiniciar = CriarLinhaDeBotao(caixa, "BotaoReiniciar", "Reiniciar");
        Button botaoMenu = CriarLinhaDeBotao(caixa, "BotaoMenu", "Menu principal");
        Button botaoSair = CriarLinhaDeBotao(caixa, "BotaoSair", "Sair do jogo");

        // Texto de aviso (ex.: "Jogo salvo.")
        TMP_Text status = NovoTexto(caixa, "TextoStatus", "", e.rotulo, 0.75f);
        status.alignment = TextAlignmentOptions.Center;
        status.color = Cor("corTextoNormal", new Color(0.66f, 0.90f, 0.77f, 1f));
        DefinirAltura(status.gameObject, alturaStatus);

        // Navegação com teclado/controle: de cima para baixo
        LigarNavegacao(botaoContinuar, seletorResolucao, seletorTela, slider,
                       botaoSalvar, botaoReiniciar, botaoMenu, botaoSair);

        // ---------- LIGA OS SCRIPTS ----------
        var soOpcoes = new SerializedObject(opcoes);
        Atribuir(soOpcoes, "sliderVolume", slider);
        Atribuir(soOpcoes, "textoVolume", textoPorcentagem);
        Atribuir(soOpcoes, "seletorResolucao", seletorResolucao);
        Atribuir(soOpcoes, "seletorModoDeTela", seletorTela);
        soOpcoes.ApplyModifiedProperties();

        var soPausa = new SerializedObject(pausa);
        Atribuir(soPausa, "painel", painel.gameObject);
        Atribuir(soPausa, "botaoContinuar", botaoContinuar);
        Atribuir(soPausa, "botaoSalvar", botaoSalvar);
        Atribuir(soPausa, "botaoReiniciar", botaoReiniciar);
        Atribuir(soPausa, "botaoMenu", botaoMenu);
        Atribuir(soPausa, "botaoSair", botaoSair);
        Atribuir(soPausa, "textoStatus", status);
        Atribuir(soPausa, "primeiroItem", botaoContinuar);
        soPausa.ApplyModifiedProperties();

        // ---------- SALVA O PREFAB E LIMPA A CENA DO MENU ----------
        if (!AssetDatabase.IsValidFolder(PastaPrefab))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
        Object.DestroyImmediate(raiz);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[Pausa] Prefab salvo em {CaminhoPrefab} com o estilo do menu principal. Arraste ele nas cenas de jogo (a cena do Menu não foi alterada).", prefab);
    }

    // ------------------------------------------------------------------
    // LEITURA DO ESTILO DO MENU PRINCIPAL
    // ------------------------------------------------------------------

    private static Estilo LerEstilo(global::Menu menu, out string erro)
    {
        erro = null;
        var estilo = new Estilo();

        Button modelo = EscolherBotaoModelo(menu);
        if (modelo == null)
        {
            erro = "Não achei nenhum botão no painel do menu para copiar o estilo.";
            return null;
        }

        estilo.hud = modelo.GetComponent<MenuItemHUD>();
        estilo.fundo = LerReferencia<Image>(estilo.hud, "fundo");
        estilo.borda = LerReferencia<Graphic>(estilo.hud, "borda");
        estilo.seta = LerReferencia<Graphic>(estilo.hud, "seta");
        estilo.rotulo = LerReferencia<TMP_Text>(estilo.hud, "texto");
        if (estilo.rotulo == null) estilo.rotulo = modelo.GetComponentInChildren<TMP_Text>(true);
        if (estilo.hud == null && estilo.fundo == null) estilo.fundo = modelo.GetComponent<Image>();

        estilo.altura = AlturaDaLinha(modelo.GetComponent<RectTransform>());

        // Título: o maior texto do painel que não está dentro de um botão
        foreach (TMP_Text t in menu.grupoMenu.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.GetComponentInParent<Button>(true) != null) continue;
            if (estilo.titulo == null || t.fontSize > estilo.titulo.fontSize) estilo.titulo = t;
        }

        // Moldura: o quadro com borda do menu
        RectTransform moldura = AcharMoldura(menu.grupoMenu.gameObject, modelo, estilo.titulo);
        estilo.moldura = moldura.GetComponent<Image>();
        estilo.larguraMoldura = moldura.rect.width;

        // Resolução de referência do Canvas do menu
        Canvas canvas = menu.grupoMenu.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                estilo.referencia = scaler.referenceResolution;
                estilo.modoMatch = scaler.screenMatchMode;
                estilo.match = scaler.matchWidthOrHeight;
            }
            else
            {
                Vector2 tamanho = ((RectTransform)canvas.transform).rect.size;
                if (tamanho.x > 100f && tamanho.y > 100f) estilo.referencia = tamanho;
            }
        }

        Debug.Log($"[Pausa] Estilo copiado do botão '{modelo.name}' (altura {estilo.altura:0}); moldura: '{moldura.name}'.", modelo);
        return estilo;
    }

    /// <summary>Prefere um botão que tenha MenuItemHUD (Opções, Continuar, Jogar, Sair, ou qualquer outro do painel).</summary>
    private static Button EscolherBotaoModelo(global::Menu menu)
    {
        Button[] preferidos = { menu.botaoOpcoes, menu.botaoContinuar, menu.botaoJogar, menu.botaoSair };

        foreach (Button b in preferidos)
            if (b != null && b.GetComponent<MenuItemHUD>() != null) return b;

        foreach (Button b in menu.grupoMenu.GetComponentsInChildren<Button>(true))
            if (b.GetComponent<MenuItemHUD>() != null) return b;

        foreach (Button b in preferidos)
            if (b != null) return b;

        Button[] todos = menu.grupoMenu.GetComponentsInChildren<Button>(true);
        return todos.Length > 0 ? todos[0] : null;
    }

    /// <summary>
    /// Acha o quadro com borda do menu: a MENOR imagem (fora dos botões) que contém, ao mesmo tempo,
    /// o centro do botão modelo e o centro do título. Se não achar, usa o painel inteiro.
    /// </summary>
    private static RectTransform AcharMoldura(GameObject painel, Button modelo, TMP_Text titulo)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform rModelo = modelo.GetComponent<RectTransform>();
        Vector3 pontoBotao = rModelo.TransformPoint(rModelo.rect.center);
        Vector3 pontoTitulo = titulo != null
            ? titulo.rectTransform.TransformPoint(titulo.rectTransform.rect.center)
            : pontoBotao;

        RectTransform melhor = null;
        float menorArea = float.MaxValue;

        foreach (Graphic g in painel.GetComponentsInChildren<Graphic>(true))
        {
            if (g is TMP_Text) continue;
            if (g.GetComponentInParent<Button>(true) != null) continue;

            RectTransform r = g.rectTransform;
            if (!r.rect.Contains(r.InverseTransformPoint(pontoBotao))) continue;
            if (!r.rect.Contains(r.InverseTransformPoint(pontoTitulo))) continue;

            float area = r.rect.width * r.rect.height;
            if (area > 0f && area < menorArea)
            {
                menorArea = area;
                melhor = r;
            }
        }

        return melhor != null ? melhor : (RectTransform)painel.transform;
    }

    private static float AlturaDaLinha(RectTransform linha)
    {
        Canvas.ForceUpdateCanvases();
        if (linha.parent is RectTransform pai)
            LayoutRebuilder.ForceRebuildLayoutImmediate(pai);

        float altura = linha.rect.height;
        return altura > 1f ? altura : 50f;
    }

    // ------------------------------------------------------------------
    // LINHAS
    // ------------------------------------------------------------------

    private static Button CriarLinhaDeBotao(Transform pai, string nome, string texto)
    {
        Linha l = NovaLinha(pai, nome, texto, false);

        var botao = l.go.AddComponent<Button>();
        botao.transition = Selectable.Transition.None;
        botao.targetGraphic = l.borda;

        LigarHud(l.go, l);
        return botao;
    }

    private static SeletorDeOpcao CriarLinhaDeSeletor(Transform pai, string rotulo)
    {
        Linha l = NovaLinha(pai, "Linha" + rotulo, rotulo, true);

        RectTransform area = NovoUI("Seletor", l.go.transform);
        Ancorar(area, InicioControle, 0.1f, FimControle, 0.9f);

        // Imagem invisível só para o seletor receber cliques do mouse
        Image imagem = area.gameObject.AddComponent<Image>();
        imagem.color = new Color(0f, 0f, 0f, 0f);

        var seletor = area.gameObject.AddComponent<SeletorDeOpcao>();
        seletor.transition = Selectable.Transition.None;
        seletor.targetGraphic = imagem;

        Color corSeta = Cor("corSetaSelecionada", new Color(0.91f, 0.97f, 0.93f, 1f));
        Color corValor = Cor("corTextoSelecionado", new Color(0.80f, 1f, 0.90f, 1f));

        TMP_Text setaEsquerda = NovoTexto(area, "SetaEsquerda", "<", e.rotulo, 0.9f);
        Ancorar(setaEsquerda.rectTransform, 0f, 0f, 0.14f, 1f);
        setaEsquerda.alignment = TextAlignmentOptions.Center;
        setaEsquerda.color = corSeta;

        TMP_Text setaDireita = NovoTexto(area, "SetaDireita", ">", e.rotulo, 0.9f);
        Ancorar(setaDireita.rectTransform, 0.86f, 0f, 1f, 1f);
        setaDireita.alignment = TextAlignmentOptions.Center;
        setaDireita.color = corSeta;

        TMP_Text valor = NovoTexto(area, "Valor", "-", e.rotulo, 0.9f);
        Ancorar(valor.rectTransform, 0.14f, 0f, 0.86f, 1f);
        valor.alignment = TextAlignmentOptions.Center;
        valor.color = corValor;

        var so = new SerializedObject(seletor);
        Atribuir(so, "textoValor", valor);
        so.ApplyModifiedProperties();

        LigarHud(area.gameObject, l);
        ConfigurarLinha(l.go, seletor);
        return seletor;
    }

    private static void CriarLinhaDeVolume(Transform pai, out Slider slider, out TMP_Text textoPorcentagem)
    {
        Linha l = NovaLinha(pai, "LinhaVolume", "Volume", true);

        Color corTrilho = Cor("corBordaNormal", new Color(0.44f, 0.66f, 0.54f, 0.55f));
        Color corPreenchimento = Cor("corBordaSelecionada", new Color(0.66f, 0.90f, 0.77f, 1f));
        Color corPuxador = Cor("corTextoSelecionado", new Color(0.80f, 1f, 0.90f, 1f));

        // Slider padrão da Unity, só com cores lisas (sem sprites)
        GameObject goSlider = DefaultControls.CreateSlider(new DefaultControls.Resources());
        goSlider.name = "SliderVolume";
        goSlider.transform.SetParent(l.go.transform, false);
        Ancorar(goSlider.GetComponent<RectTransform>(), InicioControle, 0.25f, FimSlider, 0.75f);

        slider = goSlider.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        PintarImagem(goSlider.transform.Find("Background"), new Color(corTrilho.r, corTrilho.g, corTrilho.b, 0.8f));
        PintarImagem(goSlider.transform.Find("Fill Area/Fill"), corPreenchimento);
        Transform puxador = goSlider.transform.Find("Handle Slide Area/Handle");
        PintarImagem(puxador, corPuxador);
        if (puxador != null)
            puxador.GetComponent<RectTransform>().sizeDelta = new Vector2(e.altura * 0.25f, 0f);

        textoPorcentagem = NovoTexto(l.go.transform, "TextoPorcentagem", "100%", e.rotulo, 0.9f);
        Ancorar(textoPorcentagem.rectTransform, InicioPorcentagem, 0f, FimControle, 1f);
        textoPorcentagem.alignment = TextAlignmentOptions.Left;
        textoPorcentagem.color = corPuxador;

        LigarHud(goSlider, l);
        ConfigurarLinha(l.go, slider);
    }

    /// <summary>Linha base com o visual do botão do menu: borda (a própria linha), fundo, rótulo e seta.</summary>
    private static Linha NovaLinha(Transform pai, string nome, string texto, bool rotuloAEsquerda)
    {
        var l = new Linha();

        l.go = new GameObject(nome, typeof(RectTransform));
        l.go.transform.SetParent(pai, false);
        DefinirAltura(l.go, e.altura);

        // Borda: mesma imagem e cor do menu (ou uma cor lisa se o menu não tiver)
        l.borda = l.go.AddComponent<Image>();
        if (e.borda is Image imagemBorda)
            EditorUtility.CopySerialized(imagemBorda, l.borda);
        else
            l.borda.color = Cor("corBordaNormal", new Color(0.44f, 0.66f, 0.54f, 0.55f));
        l.borda.enabled = true;
        l.borda.raycastTarget = true;

        // Fundo
        RectTransform rf = NovoUI("Fundo", l.go.transform);
        l.fundo = rf.gameObject.AddComponent<Image>();
        if (e.fundo != null)
        {
            CopiarMargens(e.fundo.rectTransform, rf);
            EditorUtility.CopySerialized(e.fundo, l.fundo);
        }
        else
        {
            Esticar(rf, 2f);
            l.fundo.color = new Color(0.07f, 0.09f, 0.08f, 0.95f);
        }
        l.fundo.enabled = true;
        l.fundo.raycastTarget = false;

        // Rótulo
        l.rotulo = NovoTexto(l.go.transform, "Texto", texto, e.rotulo, 1f);
        if (rotuloAEsquerda)
        {
            Ancorar(l.rotulo.rectTransform, 0f, 0f, FimDoRotulo, 1f);
            l.rotulo.rectTransform.offsetMin = new Vector2(e.altura * 1.0f, 0f); // espaço para a seta
            l.rotulo.alignment = TextAlignmentOptions.MidlineLeft;
        }
        else
        {
            Esticar(l.rotulo.rectTransform);
            l.rotulo.alignment = TextAlignmentOptions.Center;
        }

        // Seta (mesmo tipo e aparência da seta do menu)
        l.seta = CriarSeta(l.go.transform);
        RectTransform rs = l.seta.rectTransform;
        Vector2 tamanhoSeta = e.seta != null ? e.seta.rectTransform.rect.size : Vector2.zero;
        if (tamanhoSeta.x < 1f || tamanhoSeta.y < 1f) tamanhoSeta = new Vector2(e.altura * 0.6f, e.altura * 0.6f);
        rs.anchorMin = rs.anchorMax = new Vector2(0f, 0.5f);
        rs.pivot = new Vector2(0.5f, 0.5f);
        rs.sizeDelta = tamanhoSeta;
        rs.anchoredPosition = new Vector2(e.altura * 0.25f + tamanhoSeta.x * 0.5f, 0f);

        return l;
    }

    private static Graphic CriarSeta(Transform pai)
    {
        if (e.seta is TMP_Text textoSeta)
        {
            TMP_Text t = NovoTexto(pai, "Seta", textoSeta.text, textoSeta, 1f);
            t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        RectTransform r = NovoUI("Seta", pai);
        Image img = r.gameObject.AddComponent<Image>();

        if (e.seta is Image imagemSeta)
        {
            EditorUtility.CopySerialized(imagemSeta, img);
            img.raycastTarget = false;
            return img;
        }

        // O menu não tem seta: cria um ">" simples
        Object.DestroyImmediate(img);
        TMP_Text padrao = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (e.rotulo != null) EditorUtility.CopySerialized(e.rotulo, padrao);
        padrao.text = ">";
        padrao.alignment = TextAlignmentOptions.Center;
        padrao.enableWordWrapping = false;
        padrao.raycastTarget = false;
        return padrao;
    }

    /// <summary>Põe o MenuItemHUD no controle (que já precisa ter um Selectable) copiando as cores e o comportamento do menu.</summary>
    private static void LigarHud(GameObject controle, Linha l)
    {
        var hud = controle.AddComponent<MenuItemHUD>();
        if (e.hud != null) EditorUtility.CopySerialized(e.hud, hud);

        var so = new SerializedObject(hud);
        Atribuir(so, "fundo", l.fundo);
        Atribuir(so, "borda", l.borda);
        Atribuir(so, "seta", l.seta);
        Atribuir(so, "texto", l.rotulo);
        Atribuir(so, "fonteDeSom", null); // referência da cena do Menu não vale no prefab
        so.ApplyModifiedProperties();
    }

    /// <summary>Faz a linha inteira reagir ao mouse (seleciona o controle dela ao passar por cima).</summary>
    private static void ConfigurarLinha(GameObject linha, Selectable alvo)
    {
        var script = linha.AddComponent<LinhaDeOpcao>();
        var so = new SerializedObject(script);
        Atribuir(so, "alvo", alvo);
        so.ApplyModifiedProperties();
    }

    private static void LigarNavegacao(params Selectable[] itens)
    {
        for (int i = 0; i < itens.Length; i++)
        {
            itens[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? itens[i - 1] : null,
                selectOnDown = i < itens.Length - 1 ? itens[i + 1] : null
            };
        }
    }

    // ------------------------------------------------------------------
    // AUXILIARES
    // ------------------------------------------------------------------

    private static RectTransform NovoUI(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return go.GetComponent<RectTransform>();
    }

    /// <summary>Cria um texto com a mesma fonte, material, tamanho e cor de um texto do menu (escala = fração do tamanho).</summary>
    private static TMP_Text NovoTexto(Transform pai, string nome, string conteudo, TMP_Text modelo, float escala)
    {
        RectTransform rect = NovoUI(nome, pai);
        var texto = rect.gameObject.AddComponent<TextMeshProUGUI>();

        float tamanho;
        if (modelo != null)
        {
            EditorUtility.CopySerialized(modelo, texto);
            tamanho = modelo.fontSize * escala;
        }
        else
        {
            tamanho = e.altura * 0.5f * escala;
        }

        texto.enableAutoSizing = false;
        texto.fontSize = tamanho;
        texto.text = conteudo;
        texto.enableWordWrapping = false;
        texto.raycastTarget = false;
        return texto;
    }

    /// <summary>
    /// O título do menu pode ter camadas filhas (o mesmo texto repetido para dar relevo, como o objeto "Fundo"
    /// dentro de "Titulo"). Copia essas camadas para o título da pausa, com o mesmo texto.
    /// </summary>
    private static void ReplicarCamadasDoTitulo(TMP_Text destino, string conteudo)
    {
        if (e.titulo == null) return;

        foreach (TMP_Text camada in e.titulo.GetComponentsInChildren<TMP_Text>(true))
        {
            if (camada == e.titulo) continue;

            RectTransform r = NovoUI(camada.name, destino.transform);
            var novo = r.gameObject.AddComponent<TextMeshProUGUI>();
            EditorUtility.CopySerialized(camada, novo);

            novo.enableAutoSizing = false;
            novo.fontSize = camada.fontSize;
            novo.text = conteudo;
            novo.alignment = TextAlignmentOptions.Center;
            novo.enableWordWrapping = false;
            novo.raycastTarget = false;

            // Mesma posição e tamanho da camada original, em relação ao título
            RectTransform original = camada.rectTransform;
            r.anchorMin = original.anchorMin;
            r.anchorMax = original.anchorMax;
            r.pivot = original.pivot;
            r.anchoredPosition = original.anchoredPosition;
            r.sizeDelta = original.sizeDelta;
            r.SetSiblingIndex(original.GetSiblingIndex());
        }
    }

    /// <summary>Se o fundo original ocupa o botão todo (com margens), repete as margens; senão, usa 2 de margem.</summary>
    private static void CopiarMargens(RectTransform origem, RectTransform destino)
    {
        bool preencheTudo = origem.anchorMin == Vector2.zero && origem.anchorMax == Vector2.one;
        if (preencheTudo)
        {
            Esticar(destino);
            destino.offsetMin = origem.offsetMin;
            destino.offsetMax = origem.offsetMax;
        }
        else
        {
            Esticar(destino, 2f);
        }
    }

    private static void DefinirAltura(GameObject go, float altura)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = altura;
        le.preferredHeight = altura;
    }

    /// <summary>Âncoras em fração do pai, sem margens.</summary>
    private static void Ancorar(RectTransform r, float x0, float y0, float x1, float y1)
    {
        r.anchorMin = new Vector2(x0, y0);
        r.anchorMax = new Vector2(x1, y1);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void Esticar(RectTransform r, float margem = 0f)
    {
        Ancorar(r, 0f, 0f, 1f, 1f);
        r.offsetMin = new Vector2(margem, margem);
        r.offsetMax = new Vector2(-margem, -margem);
    }

    private static void PintarImagem(Transform t, Color cor)
    {
        if (t == null) return;

        Image img = t.GetComponent<Image>();
        if (img != null) img.color = cor;
    }

    private static void Atribuir(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty p = so.FindProperty(campo);
        if (p == null)
        {
            Debug.LogWarning($"[Pausa] '{so.targetObject.GetType().Name}' não tem o campo '{campo}'. Não consegui ligar.");
            return;
        }
        p.objectReferenceValue = valor;
    }

    private static T LerReferencia<T>(MenuItemHUD hud, string campo) where T : Object
    {
        if (hud == null) return null;

        SerializedProperty p = new SerializedObject(hud).FindProperty(campo);
        return p != null ? p.objectReferenceValue as T : null;
    }

    /// <summary>Cor guardada no MenuItemHUD do menu principal (ou um padrão, se não houver HUD).</summary>
    private static Color Cor(string campo, Color padrao)
    {
        if (e == null || e.hud == null) return padrao;

        SerializedProperty p = new SerializedObject(e.hud).FindProperty(campo);
        return p != null ? p.colorValue : padrao;
    }

    private static void Aviso(string mensagem)
    {
        EditorUtility.DisplayDialog("Menu de Pausa", mensagem, "OK");
    }
}