using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// COLOQUE ESTE ARQUIVO NA PASTA "Editor" (ex.: Assets/Scripts/Editor).
/// Abra uma cena de jogo e use Tools > Menu > Criar Menu de Pausa (Esc).
///
/// Monta o Canvas da pausa do zero (não depende do menu principal) e liga tudo no MenuDePausa e no OpcoesDoJogo.
/// Na primeira vez também salva um prefab em Assets/Prefabs/MenuDePausa.prefab para você arrastar nas outras cenas.
///
/// DICA (opcional): antes de rodar, selecione no Project o asset de fonte TMP do seu jogo
/// (ou um texto TMP da cena) para o menu usar a mesma fonte.
/// </summary>
public static class CriadorDoMenuDePausa
{
    private const string NomeDoDesfazer = "Criar Menu de Pausa";
    private const string PastaPrefab = "Assets/Prefabs";
    private const string CaminhoPrefab = "Assets/Prefabs/MenuDePausa.prefab";

    // ---- Tamanhos (em pixels da resolução de referência 1920x1080) ----
    private const float LarguraCaixa = 760f;
    private const float AlturaLinha = 56f;
    private const float Espaco = 12f;
    private const int TamanhoTexto = 26;
    private const int TamanhoTitulo = 48;

    // ---- Dentro das linhas de configuração (frações da largura da linha) ----
    private const float FimDoRotulo = 0.46f;
    private const float InicioControle = 0.50f;
    private const float FimControle = 0.96f;
    private const float FimSlider = 0.78f;
    private const float InicioPorcentagem = 0.82f;

    // ---- Cores (edite aqui para combinar com o seu menu) ----
    private static readonly Color CorFundoTela      = new Color(0f, 0f, 0f, 0.70f);
    private static readonly Color CorCaixa          = new Color(0.04f, 0.05f, 0.05f, 0.97f);
    private static readonly Color CorMoldura        = new Color(0.35f, 0.38f, 0.37f, 1f);
    private static readonly Color CorFundoLinha     = new Color(0.12f, 0.13f, 0.13f, 1f);
    private static readonly Color CorTitulo         = new Color(0.85f, 0.20f, 0.15f, 1f);
    private static readonly Color CorTextoNormal    = new Color(0.66f, 0.66f, 0.62f, 1f);
    private static readonly Color CorBordaNormal    = new Color(0.28f, 0.30f, 0.30f, 1f);
    private static readonly Color CorSetaNormal     = new Color(0.28f, 0.30f, 0.30f, 1f);
    private static readonly Color CorTextoSelecao   = new Color(0.90f, 0.30f, 0.20f, 1f);
    private static readonly Color CorBordaSelecao   = new Color(0.80f, 0.15f, 0.12f, 1f);
    private static readonly Color CorSetaSelecao    = new Color(0.35f, 0.95f, 1.00f, 1f);
    private static readonly Color CorValor          = new Color(0.88f, 0.88f, 0.82f, 1f);
    private static readonly Color CorTrilho         = new Color(0.30f, 0.12f, 0.10f, 1f);
    private static readonly Color CorPreenchimento  = new Color(0.80f, 0.15f, 0.12f, 1f);
    private static readonly Color CorPuxador        = new Color(0.95f, 0.45f, 0.10f, 1f);
    private static readonly Color CorStatus         = new Color(0.66f, 0.66f, 0.62f, 1f);

    private static TMP_FontAsset fonte;
    private static Material materialDaFonte;

    private class Linha
    {
        public GameObject go;
        public Image borda;
        public Image fundo;
        public TMP_Text rotulo;
        public TMP_Text seta;
    }

    [MenuItem("Tools/Menu/Criar Menu de Pausa (Esc)")]
    private static void Criar()
    {
        var existente = Object.FindFirstObjectByType<MenuDePausa>(FindObjectsInactive.Include);
        if (existente != null)
        {
            bool substituir = EditorUtility.DisplayDialog(
                NomeDoDesfazer,
                "Esta cena já tem um Menu de Pausa. Substituir por um novo?",
                "Substituir", "Cancelar");
            if (!substituir) return;

            Undo.DestroyObjectImmediate(existente.gameObject);
        }

        EscolherFonte();

        // ---------- EVENT SYSTEM (precisa existir para a UI responder) ----------
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, NomeDoDesfazer);
        }

        // ---------- CANVAS ----------
        var raiz = new GameObject("MenuDePausa", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(raiz, NomeDoDesfazer);

        var canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = raiz.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raiz.AddComponent<GraphicRaycaster>();
        var pausa = raiz.AddComponent<MenuDePausa>();

        // ---------- PAINEL (tela inteira escurecida; é o que liga/desliga) ----------
        RectTransform painel = NovoUI("Painel", raiz.transform);
        Esticar(painel);
        painel.gameObject.AddComponent<Image>().color = CorFundoTela;
        var opcoes = painel.gameObject.AddComponent<OpcoesDoJogo>();

        // ---------- CAIXA CENTRAL ----------
        RectTransform caixa = NovoUI("Caixa", painel);
        caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
        caixa.pivot = new Vector2(0.5f, 0.5f);
        caixa.sizeDelta = new Vector2(LarguraCaixa, 0f);

        caixa.gameObject.AddComponent<Image>().color = CorCaixa;
        var moldura = caixa.gameObject.AddComponent<Outline>();
        moldura.effectColor = CorMoldura;
        moldura.effectDistance = new Vector2(3f, -3f);

        var layout = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 32, 32);
        layout.spacing = Espaco;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var ajuste = caixa.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Título e divisória
        TMP_Text titulo = CriarTexto(caixa, "Titulo", "PAUSA", TamanhoTitulo, CorTitulo, TextAlignmentOptions.Center);
        DefinirAltura(titulo.gameObject, 70f);

        RectTransform divisoria = NovoUI("Divisoria", caixa);
        divisoria.gameObject.AddComponent<Image>().color = CorBordaNormal;
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
        TMP_Text status = CriarTexto(caixa, "TextoStatus", "", TamanhoTexto - 4, CorStatus, TextAlignmentOptions.Center);
        DefinirAltura(status.gameObject, 30f);

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

        // ---------- PREFAB (só na primeira vez) ----------
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab) == null)
        {
            if (!AssetDatabase.IsValidFolder(PastaPrefab))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, CaminhoPrefab, InteractionMode.UserAction);
            Debug.Log($"[Pausa] Prefab salvo em {CaminhoPrefab}. Arraste ele nas outras cenas de jogo.");
        }
        else
        {
            Debug.Log($"[Pausa] O prefab {CaminhoPrefab} já existe e não foi alterado.");
        }

        Selection.activeGameObject = raiz;
        EditorGUIUtility.PingObject(raiz);
        EditorSceneManager.MarkSceneDirty(raiz.scene);

        Debug.Log("[Pausa] Menu de pausa criado. No Editor ele aparece aberto para você editar; no Play começa escondido e abre com Esc. Salve a cena (Ctrl+S).", raiz);
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

        TMP_Text setaEsquerda = CriarTexto(area, "SetaEsquerda", "<", TamanhoTexto, CorSetaSelecao, TextAlignmentOptions.Center);
        Ancorar(setaEsquerda.rectTransform, 0f, 0f, 0.14f, 1f);

        TMP_Text setaDireita = CriarTexto(area, "SetaDireita", ">", TamanhoTexto, CorSetaSelecao, TextAlignmentOptions.Center);
        Ancorar(setaDireita.rectTransform, 0.86f, 0f, 1f, 1f);

        TMP_Text valor = CriarTexto(area, "Valor", "-", TamanhoTexto, CorValor, TextAlignmentOptions.Center);
        Ancorar(valor.rectTransform, 0.14f, 0f, 0.86f, 1f);

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

        PintarImagem(goSlider.transform.Find("Background"), CorTrilho);
        PintarImagem(goSlider.transform.Find("Fill Area/Fill"), CorPreenchimento);
        Transform puxador = goSlider.transform.Find("Handle Slide Area/Handle");
        PintarImagem(puxador, CorPuxador);
        if (puxador != null)
            puxador.GetComponent<RectTransform>().sizeDelta = new Vector2(AlturaLinha * 0.25f, 0f);

        textoPorcentagem = CriarTexto(l.go.transform, "TextoPorcentagem", "100%", TamanhoTexto - 2, CorValor, TextAlignmentOptions.Left);
        Ancorar(textoPorcentagem.rectTransform, InicioPorcentagem, 0f, FimControle, 1f);

        LigarHud(goSlider, l);
        ConfigurarLinha(l.go, slider);
    }

    /// <summary>Linha base: borda (a própria linha), fundo escuro, rótulo e seta. Os controles entram por cima.</summary>
    private static Linha NovaLinha(Transform pai, string nome, string texto, bool rotuloAEsquerda)
    {
        var l = new Linha();

        l.go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        l.go.transform.SetParent(pai, false);
        DefinirAltura(l.go, AlturaLinha);

        l.borda = l.go.GetComponent<Image>();
        l.borda.color = CorBordaNormal;

        RectTransform rf = NovoUI("Fundo", l.go.transform);
        Esticar(rf, 2f);
        l.fundo = rf.gameObject.AddComponent<Image>();
        l.fundo.color = CorFundoLinha;
        l.fundo.raycastTarget = false;

        TextAlignmentOptions alinhamento = rotuloAEsquerda ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
        l.rotulo = CriarTexto(l.go.transform, "Texto", texto, TamanhoTexto, CorTextoNormal, alinhamento);
        if (rotuloAEsquerda)
        {
            Ancorar(l.rotulo.rectTransform, 0f, 0f, FimDoRotulo, 1f);
            l.rotulo.rectTransform.offsetMin = new Vector2(56f, 0f); // espaço para a seta
        }
        else
        {
            Esticar(l.rotulo.rectTransform);
        }

        l.seta = CriarTexto(l.go.transform, "Seta", ">", TamanhoTexto, CorSetaNormal, TextAlignmentOptions.Center);
        RectTransform rs = l.seta.rectTransform;
        rs.anchorMin = rs.anchorMax = new Vector2(0f, 0.5f);
        rs.pivot = new Vector2(0.5f, 0.5f);
        rs.anchoredPosition = new Vector2(30f, 0f);
        rs.sizeDelta = new Vector2(36f, 36f);

        return l;
    }

    /// <summary>Põe o MenuItemHUD no controle (já precisa ter um Selectable) e liga fundo, borda, seta, texto e cores.</summary>
    private static void LigarHud(GameObject controle, Linha l)
    {
        var hud = controle.AddComponent<MenuItemHUD>();
        var so = new SerializedObject(hud);

        Atribuir(so, "fundo", l.fundo);
        Atribuir(so, "borda", l.borda);
        Atribuir(so, "seta", l.seta);
        Atribuir(so, "texto", l.rotulo);

        DefinirCor(so, "corTextoNormal", CorTextoNormal);
        DefinirCor(so, "corBordaNormal", CorBordaNormal);
        DefinirCor(so, "corSetaNormal", CorSetaNormal);
        DefinirCor(so, "corTextoSelecionado", CorTextoSelecao);
        DefinirCor(so, "corBordaSelecionada", CorBordaSelecao);
        DefinirCor(so, "corSetaSelecionada", CorSetaSelecao);

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

    /// <summary>Usa a fonte selecionada no Project (ou de um texto TMP selecionado); senão tenta achar uma fonte do projeto.</summary>
    private static void EscolherFonte()
    {
        fonte = null;
        materialDaFonte = null;

        if (Selection.activeObject is TMP_FontAsset selecionada)
        {
            fonte = selecionada;
        }
        else if (Selection.activeGameObject != null)
        {
            TMP_Text t = Selection.activeGameObject.GetComponentInChildren<TMP_Text>(true);
            if (t != null)
            {
                fonte = t.font;
                materialDaFonte = t.fontSharedMaterial;
            }
        }

        if (fonte == null)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                if (caminho.Contains("TextMesh Pro") || caminho.Contains("LiberationSans")) continue;

                var candidata = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(caminho);
                if (candidata == null) continue;

                fonte = candidata;
                break;
            }
        }

        Debug.Log(fonte != null
            ? $"[Pausa] Fonte usada: {fonte.name}. Para usar outra, selecione o asset de fonte TMP no Project e rode de novo."
            : "[Pausa] Usando a fonte padrão do TextMeshPro. Para usar a do seu jogo, selecione o asset de fonte TMP no Project e rode de novo.");
    }

    private static RectTransform NovoUI(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return go.GetComponent<RectTransform>();
    }

    private static TMP_Text CriarTexto(Transform pai, string nome, string conteudo, int tamanho, Color cor, TextAlignmentOptions alinhamento)
    {
        RectTransform rect = NovoUI(nome, pai);
        var texto = rect.gameObject.AddComponent<TextMeshProUGUI>();

        if (fonte != null)
        {
            texto.font = fonte;
            if (materialDaFonte != null) texto.fontSharedMaterial = materialDaFonte;
        }

        texto.fontSize = tamanho;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.text = conteudo;
        texto.enableWordWrapping = false;
        texto.raycastTarget = false;
        return texto;
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

    private static void DefinirCor(SerializedObject so, string campo, Color cor)
    {
        SerializedProperty p = so.FindProperty(campo);
        if (p != null) p.colorValue = cor;
    }
}
