using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// COLOQUE ESTE ARQUIVO NA PASTA "Editor" (ex.: Assets/Scripts/Editor).
/// Abra a cena do Menu e use Tools > Menu > Criar Painel de Opções.
///
/// Copia o painel do menu (mantendo moldura, título e visual) e monta linhas com o rótulo à esquerda
/// e o controle à direita:  Resolução ‹ ›  /  Tela ‹ ›  /  Volume [slider] 100%  /  [ Voltar ].
/// Cada linha usa o visual de um botão do menu, então o estilo (fundo, borda, seta) é o mesmo.
/// </summary>
public static class CriadorDoPainelDeOpcoes
{
    private const string NomeDoDesfazer = "Criar Painel de Opções";

    // ---- Layout do painel (frações do painel: 0 = topo/esquerda, 1 = base/direita) ----
    private const float MargemLateral = 0.07f;        // espaço entre a borda do painel e as linhas
    private const float TopoPrimeiraLinha = 0.38f;    // centro da 1ª linha, medido do topo
    private const float PassoEntreLinhas = 0.115f;    // distância entre os centros das linhas
    private const float PosicaoVoltar = 0.79f;        // centro do botão Voltar, medido do topo
    private const float LarguraVoltar = 0.40f;        // largura do botão Voltar

    // ---- Dentro de cada linha (frações da largura da linha) ----
    private const float FimDoRotulo = 0.46f;
    private const float InicioControle = 0.50f;
    private const float FimControle = 0.96f;
    private const float FimSlider = 0.78f;
    private const float InicioPorcentagem = 0.82f;

    // Largura do botão modelo antes de virar linha larga (usada para achar elementos de largura fixa)
    private static float larguraDoModelo;

    [MenuItem("Tools/Menu/Criar Painel de Opções")]
    private static void Criar()
    {
        // "global::" porque o Editor também tem uma classe UnityEditor.Menu
        var menu = Object.FindFirstObjectByType<global::Menu>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Aviso("Não achei o componente Menu na cena. Abra a cena do menu e tente de novo.");
            return;
        }

        if (menu.grupoMenu == null)
        {
            Aviso("O campo 'Grupo Menu' do componente Menu está vazio. Arraste para ele o painel do menu (o que tem o CanvasGroup).");
            return;
        }

        if (menu.painelOpcoes != null)
        {
            bool substituir = EditorUtility.DisplayDialog(
                NomeDoDesfazer,
                "O Menu já tem um painel de opções ligado. Substituir por um novo?",
                "Substituir", "Cancelar");
            if (!substituir) return;

            Undo.DestroyObjectImmediate(menu.painelOpcoes.gameObject);
        }

        // ---------- COPIA O PAINEL DO MENU ----------
        GameObject copia = Object.Instantiate(menu.grupoMenu.gameObject, menu.grupoMenu.transform.parent);
        copia.name = "PainelOpcoes";
        Undo.RegisterCreatedObjectUndo(copia, NomeDoDesfazer);
        copia.transform.SetSiblingIndex(menu.grupoMenu.transform.GetSiblingIndex() + 1);

        RemoverScriptsQueNaoSaoDeUI(copia);

        // Botões da cópia, de cima para baixo. O de cima serve de modelo de visual para todas as linhas.
        var botoes = new List<Button>(copia.GetComponentsInChildren<Button>(true));
        botoes.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        if (botoes.Count < 1)
        {
            Object.DestroyImmediate(copia);
            Aviso("O painel do menu precisa ter pelo menos 1 botão para eu usar como modelo de visual.");
            return;
        }

        Button modelo = botoes[0];
        float altura = AlturaDaLinha(modelo.GetComponent<RectTransform>());

        // Título (antes de mexer nos botões, para ignorar o texto que está dentro deles)
        TMP_Text titulo = null;
        foreach (TMP_Text t in copia.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.GetComponentInParent<Button>(true) != null) continue;
            if (titulo == null || t.fontSize > titulo.fontSize) titulo = t;
        }
        if (titulo != null) titulo.text = "OPÇÕES";

        // A moldura é o quadro com borda onde o menu aparece (menor Image que contém o título e os botões).
        // Todas as frações do layout valem em relação a ela, e não ao CanvasGroup inteiro.
        RectTransform moldura = AcharMoldura(copia, modelo, titulo);
        larguraDoModelo = LarguraDaLinha(Rect(modelo));
        Debug.Log($"[Menu] Moldura do painel: '{moldura.name}'. Se as linhas ficarem fora do lugar, me diga esse nome.", moldura);

        // ---------- CRIA AS LINHAS (clones do botão modelo) ----------
        Button linhaResolucao = ClonarLinha(modelo, moldura, "LinhaResolucao");
        Button linhaModo = ClonarLinha(modelo, moldura, "LinhaModoDeTela");
        Button linhaVolume = ClonarLinha(modelo, moldura, "LinhaVolume");
        Button botaoVoltar = modelo;
        botaoVoltar.name = "BotaoVoltar";

        // Os outros botões da cópia não servem mais
        foreach (Button b in botoes)
        {
            if (b != modelo) Object.DestroyImmediate(b.gameObject);
        }

        // O Voltar sai do container original e passa a ser filho direto da moldura
        botaoVoltar.transform.SetParent(moldura, false);

        // Posição de cada linha
        PosicionarLinha(Rect(linhaResolucao), TopoPrimeiraLinha, altura, MargemLateral, 1f - MargemLateral);
        PosicionarLinha(Rect(linhaModo), TopoPrimeiraLinha + PassoEntreLinhas, altura, MargemLateral, 1f - MargemLateral);
        PosicionarLinha(Rect(linhaVolume), TopoPrimeiraLinha + PassoEntreLinhas * 2f, altura, MargemLateral, 1f - MargemLateral);
        PosicionarLinha(Rect(botaoVoltar), PosicaoVoltar, altura, 0.5f - LarguraVoltar / 2f, 0.5f + LarguraVoltar / 2f);

        // ---------- CONTROLES ----------
        SeletorDeOpcao seletorResolucao = ConstruirLinhaDeSeletor(linhaResolucao, "Resolução", altura);
        SeletorDeOpcao seletorModo = ConstruirLinhaDeSeletor(linhaModo, "Tela", altura);

        Slider slider;
        TMP_Text textoPorcentagem;
        ConstruirLinhaDeVolume(linhaVolume, altura, out slider, out textoPorcentagem);

        // Voltar
        DefinirTexto(botaoVoltar, "Voltar");
        LimparCliquesDoInspector(botaoVoltar);

        // Navegação com teclado/controle: de cima para baixo
        seletorResolucao.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = seletorModo };
        seletorModo.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = seletorResolucao, selectOnDown = slider };
        slider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = seletorModo, selectOnDown = botaoVoltar };
        botaoVoltar.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = slider };

        // ---------- SCRIPT DAS OPÇÕES ----------
        var opcoes = copia.AddComponent<OpcoesDoJogo>();
        var soOpcoes = new SerializedObject(opcoes);
        Atribuir(soOpcoes, "sliderVolume", slider);
        Atribuir(soOpcoes, "textoVolume", textoPorcentagem);
        Atribuir(soOpcoes, "seletorResolucao", seletorResolucao);
        Atribuir(soOpcoes, "seletorModoDeTela", seletorModo);
        soOpcoes.ApplyModifiedProperties();

        // ---------- LIGA NO MENU ----------
        var soMenu = new SerializedObject(menu);
        Atribuir(soMenu, "painelOpcoes", copia.GetComponent<CanvasGroup>());
        Atribuir(soMenu, "botaoVoltarOpcoes", botaoVoltar);
        Atribuir(soMenu, "primeiroItemOpcoes", seletorResolucao);
        LigarBotoesDoMenuPeloTexto(menu, soMenu);
        soMenu.ApplyModifiedProperties();

        Selection.activeGameObject = copia;
        EditorGUIUtility.PingObject(copia);
        EditorSceneManager.MarkSceneDirty(copia.scene);

        Debug.Log("[Menu] Painel de Opções criado e ligado no componente Menu. No Editor ele aparece sobre o menu; no Play começa escondido. Salve a cena (Ctrl+S).", copia);
    }

    // ------------------------------------------------------------------
    // LINHAS
    // ------------------------------------------------------------------

    private static SeletorDeOpcao ConstruirLinhaDeSeletor(Button linha, string nomeDoRotulo, float altura)
    {
        RectTransform rect = Rect(linha);
        MenuItemHUD hudAntigo = linha.GetComponent<MenuItemHUD>();
        TMP_Text rotulo = PrepararRotulo(linha, hudAntigo, nomeDoRotulo, altura);

        Color corSeta = LerCor(hudAntigo, "corSetaSelecionada", new Color(0.91f, 0.97f, 0.93f, 1f));
        Color corValor = LerCor(hudAntigo, "corTextoSelecionado", new Color(0.80f, 1f, 0.90f, 1f));

        var go = new GameObject("Seletor", typeof(RectTransform));
        go.transform.SetParent(rect, false);
        IgnorarLayout(go);
        Ancorar(go.GetComponent<RectTransform>(), InicioControle, 0.1f, FimControle, 0.9f);

        // Imagem invisível só para o seletor receber cliques do mouse
        Image area = go.AddComponent<Image>();
        area.color = new Color(0f, 0f, 0f, 0f);

        var seletor = go.AddComponent<SeletorDeOpcao>();
        seletor.transition = Selectable.Transition.None;
        seletor.targetGraphic = area;

        TMP_Text setaEsquerda = CriarTextoComoOutro(rotulo, go.transform, "SetaEsquerda", "<");
        Ancorar(setaEsquerda.rectTransform, 0f, 0f, 0.14f, 1f);
        setaEsquerda.alignment = TextAlignmentOptions.Center;
        setaEsquerda.color = corSeta;
        IgnorarLayout(setaEsquerda.gameObject);

        TMP_Text setaDireita = CriarTextoComoOutro(rotulo, go.transform, "SetaDireita", ">");
        Ancorar(setaDireita.rectTransform, 0.86f, 0f, 1f, 1f);
        setaDireita.alignment = TextAlignmentOptions.Center;
        setaDireita.color = corSeta;
        IgnorarLayout(setaDireita.gameObject);

        TMP_Text valor = CriarTextoComoOutro(rotulo, go.transform, "Valor", "-");
        Ancorar(valor.rectTransform, 0.14f, 0f, 0.86f, 1f);
        valor.alignment = TextAlignmentOptions.Center;
        valor.color = corValor;
        valor.enableWordWrapping = false;
        IgnorarLayout(valor.gameObject);

        var so = new SerializedObject(seletor);
        Atribuir(so, "textoValor", valor);
        so.ApplyModifiedProperties();

        // O seletor passa a ser o item "selecionável" da linha: ele acende a borda e a seta
        TransferirHud(hudAntigo, go);
        RemoverBotao(linha, hudAntigo);
        ConfigurarLinha(rect.gameObject, seletor);

        return seletor;
    }

    private static void ConstruirLinhaDeVolume(Button linha, float altura, out Slider slider, out TMP_Text textoPorcentagem)
    {
        RectTransform rect = Rect(linha);
        MenuItemHUD hudAntigo = linha.GetComponent<MenuItemHUD>();
        TMP_Text rotulo = PrepararRotulo(linha, hudAntigo, "Volume", altura);

        Color corTrilho = LerCor(hudAntigo, "corBordaNormal", new Color(0.44f, 0.66f, 0.54f, 0.55f));
        Color corPreenchimento = LerCor(hudAntigo, "corBordaSelecionada", new Color(0.66f, 0.90f, 0.77f, 1f));
        Color corPuxador = LerCor(hudAntigo, "corTextoSelecionado", new Color(0.80f, 1f, 0.90f, 1f));

        // Slider padrão da Unity, só com cores lisas (sem sprites)
        GameObject goSlider = DefaultControls.CreateSlider(new DefaultControls.Resources());
        goSlider.name = "SliderVolume";
        goSlider.transform.SetParent(rect, false);
        IgnorarLayout(goSlider);
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
            puxador.GetComponent<RectTransform>().sizeDelta = new Vector2(altura * 0.22f, 0f);

        // Texto com a porcentagem, ao lado do slider
        textoPorcentagem = CriarTextoComoOutro(rotulo, rect, "TextoPorcentagem", "100%");
        Ancorar(textoPorcentagem.rectTransform, InicioPorcentagem, 0f, FimControle, 1f);
        textoPorcentagem.alignment = TextAlignmentOptions.Left;
        textoPorcentagem.enableWordWrapping = false;
        IgnorarLayout(textoPorcentagem.gameObject);

        TransferirHud(hudAntigo, goSlider);
        RemoverBotao(linha, hudAntigo);
        ConfigurarLinha(rect.gameObject, slider);
    }

    // ------------------------------------------------------------------
    // AUXILIARES DE LAYOUT
    // ------------------------------------------------------------------

    private static RectTransform Rect(Component c) => c.GetComponent<RectTransform>();

    /// <summary>Altura da linha em unidades do Canvas, forçando o layout a atualizar antes de medir.</summary>
    private static float AlturaDaLinha(RectTransform linha)
    {
        Canvas.ForceUpdateCanvases();
        if (linha.parent is RectTransform pai)
            LayoutRebuilder.ForceRebuildLayoutImmediate(pai);

        float altura = linha.rect.height;
        return altura > 1f ? altura : 40f;
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

    /// <summary>Coloca a linha numa altura do painel (yDoTopo: 0 = topo, 1 = base) esticada de x0 a x1.</summary>
    private static void PosicionarLinha(RectTransform r, float yDoTopo, float altura, float x0, float x1)
    {
        float y = 1f - yDoTopo;
        r.anchorMin = new Vector2(x0, y);
        r.anchorMax = new Vector2(x1, y);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(0f, altura);
    }

    private static Button ClonarLinha(Button modelo, Transform pai, string nome)
    {
        GameObject go = Object.Instantiate(modelo.gameObject, pai);
        go.name = nome;
        return go.GetComponent<Button>();
    }

    /// <summary>Texto da linha: alinhado à esquerda, na metade esquerda. Também acomoda fundo, borda e seta na linha mais larga.</summary>
    private static TMP_Text PrepararRotulo(Button linha, MenuItemHUD hud, string texto, float altura)
    {
        TMP_Text rotulo = EncontrarRotulo(linha, hud);
        Graphic seta = LerReferencia<Graphic>(hud, "seta");

        // Qualquer elemento do botão com largura fixa (ex.: um fundo centralizado) passa a acompanhar a linha larga
        EsticarDecoracaoDaLinha(Rect(linha), rotulo, seta);

        if (rotulo != null)
        {
            rotulo.text = texto;
            Ancorar(rotulo.rectTransform, 0f, 0f, FimDoRotulo, 1f);
            rotulo.rectTransform.offsetMin = new Vector2(altura * 0.9f, 0f); // deixa espaço para a seta
            rotulo.alignment = TextAlignmentOptions.MidlineLeft;
            rotulo.enableWordWrapping = false;
        }

        if (seta != null && seta.rectTransform != Rect(linha))
        {
            // seta presa ao canto esquerdo da linha, não ao centro dela
            RectTransform rs = seta.rectTransform;
            rs.anchorMin = rs.anchorMax = new Vector2(0f, 0.5f);
            rs.anchoredPosition = new Vector2(altura * 0.5f, 0f);
        }

        return rotulo;
    }

    private static float LarguraDaLinha(RectTransform linha)
    {
        Canvas.ForceUpdateCanvases();
        if (linha.parent is RectTransform pai)
            LayoutRebuilder.ForceRebuildLayoutImmediate(pai);

        float largura = linha.rect.width;
        return largura > 1f ? largura : 200f;
    }

    /// <summary>
    /// Acha o quadro com borda do menu: a MENOR imagem (fora dos botões) que contém, ao mesmo tempo,
    /// o centro do botão modelo e o centro do título. Se não achar, usa o painel inteiro.
    /// </summary>
    private static RectTransform AcharMoldura(GameObject copia, Button modelo, TMP_Text titulo)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform rModelo = Rect(modelo);
        Vector3 pontoBotao = rModelo.TransformPoint(rModelo.rect.center);
        Vector3 pontoTitulo = titulo != null
            ? titulo.rectTransform.TransformPoint(titulo.rectTransform.rect.center)
            : pontoBotao;

        RectTransform melhor = null;
        float menorArea = float.MaxValue;

        foreach (Graphic g in copia.GetComponentsInChildren<Graphic>(true))
        {
            if (g is TMP_Text) continue;                           // textos não são moldura
            if (g.GetComponentInParent<Button>(true) != null) continue; // nem o visual dos botões

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

        return melhor != null ? melhor : (RectTransform)copia.transform;
    }

    /// <summary>
    /// O botão original é estreito. Dentro da linha larga, tudo que tinha largura fixa e era quase do tamanho
    /// do botão (fundo, brilho, sombra...) é esticado para acompanhar a linha. O rótulo e a seta ficam de fora.
    /// </summary>
    private static void EsticarDecoracaoDaLinha(RectTransform linha, TMP_Text rotulo, Graphic seta)
    {
        foreach (RectTransform r in linha.GetComponentsInChildren<RectTransform>(true))
        {
            if (r == linha) continue;
            if (rotulo != null && r.IsChildOf(rotulo.rectTransform)) continue;
            if (seta != null && r.IsChildOf(seta.rectTransform)) continue;
            if (r.rect.width < larguraDoModelo * 0.6f) continue;

            EsticarNaHorizontal(r);
        }
    }

    /// <summary>Se o elemento tem largura fixa (âncoras juntas), faz ele acompanhar a largura da linha.</summary>
    private static void EsticarNaHorizontal(RectTransform r)
    {
        if (!Mathf.Approximately(r.anchorMin.x, r.anchorMax.x)) return;

        r.anchorMin = new Vector2(0f, r.anchorMin.y);
        r.anchorMax = new Vector2(1f, r.anchorMax.y);
        r.offsetMin = new Vector2(0f, r.offsetMin.y);
        r.offsetMax = new Vector2(0f, r.offsetMax.y);
    }

    /// <summary>Põe o LinhaDeOpcao na linha e garante que ela recebe o mouse (precisa de um Graphic com raycast).</summary>
    private static void ConfigurarLinha(GameObject linha, Selectable alvo)
    {
        if (linha.GetComponent<Graphic>() == null)
        {
            var img = linha.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
        }
        linha.GetComponent<Graphic>().raycastTarget = true;

        var script = linha.GetComponent<LinhaDeOpcao>();
        if (script == null) script = linha.AddComponent<LinhaDeOpcao>();

        var so = new SerializedObject(script);
        Atribuir(so, "alvo", alvo);
        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------
    // AUXILIARES DE HUD / COMPONENTES
    // ------------------------------------------------------------------

    /// <summary>Copia o MenuItemHUD da linha para o controle novo (mesmos campos e cores).</summary>
    private static void TransferirHud(MenuItemHUD origem, GameObject destinoGO)
    {
        if (origem == null) return;

        var destino = destinoGO.AddComponent<MenuItemHUD>();

        // Copia todos os campos serializados (referências, cores, sons) do HUD antigo para o novo
        EditorUtility.CopySerialized(origem, destino);
    }

    /// <summary>Remove o HUD e o Button da linha, deixando só o visual (fundo, borda, seta e texto).</summary>
    private static void RemoverBotao(Button botao, MenuItemHUD hud)
    {
        if (hud != null) Object.DestroyImmediate(hud);
        Object.DestroyImmediate(botao);
    }

    private static void RemoverScriptsQueNaoSaoDeUI(GameObject raiz)
    {
        MonoBehaviour[] scripts = raiz.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = scripts.Length - 1; i >= 0; i--)
        {
            MonoBehaviour mb = scripts[i];
            if (mb == null || EhComponenteDeUI(mb)) continue;

            Debug.Log($"[Menu] Tirei '{mb.GetType().Name}' da cópia do painel (não é um componente de UI).", raiz);
            Object.DestroyImmediate(mb);
        }
    }

    private static bool EhComponenteDeUI(MonoBehaviour mb)
    {
        return mb is Graphic || mb is Selectable || mb is MenuItemHUD
            || mb is LayoutGroup || mb is LayoutElement || mb is ContentSizeFitter || mb is AspectRatioFitter
            || mb is Mask || mb is RectMask2D || mb is BaseMeshEffect || mb is ScrollRect;
    }

    private static TMP_Text EncontrarRotulo(Button botao, MenuItemHUD hud)
    {
        TMP_Text t = LerReferencia<TMP_Text>(hud, "texto");
        return t != null ? t : botao.GetComponentInChildren<TMP_Text>(true);
    }

    private static void DefinirTexto(Button botao, string texto)
    {
        TMP_Text t = EncontrarRotulo(botao, botao.GetComponent<MenuItemHUD>());
        if (t != null) t.text = texto;
    }

    private static void LimparCliquesDoInspector(Button botao)
    {
        var so = new SerializedObject(botao);
        SerializedProperty chamadas = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (chamadas == null) return;

        chamadas.ClearArray();
        so.ApplyModifiedProperties();
    }

    private static void LigarBotoesDoMenuPeloTexto(global::Menu menu, SerializedObject soMenu)
    {
        foreach (Button b in menu.grupoMenu.GetComponentsInChildren<Button>(true))
        {
            TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
            if (t == null) continue;

            string texto = t.text.Trim().ToLowerInvariant();

            if (texto.Contains("continu")) AtribuirSeVazio(soMenu, "botaoContinuar", b);
            else if (texto.StartsWith("op")) AtribuirSeVazio(soMenu, "botaoOpcoes", b);
            else if (texto.Contains("sair")) AtribuirSeVazio(soMenu, "botaoSair", b);
        }
    }

    private static void Atribuir(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty p = so.FindProperty(campo);
        if (p == null)
        {
            Debug.LogWarning($"[Menu] '{so.targetObject.GetType().Name}' não tem o campo '{campo}'. Não consegui ligar.");
            return;
        }
        p.objectReferenceValue = valor;
    }

    private static void AtribuirSeVazio(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty p = so.FindProperty(campo);
        if (p != null && p.objectReferenceValue == null)
            p.objectReferenceValue = valor;
    }

    private static T LerReferencia<T>(MenuItemHUD hud, string campo) where T : Object
    {
        if (hud == null) return null;

        SerializedProperty p = new SerializedObject(hud).FindProperty(campo);
        return p != null ? p.objectReferenceValue as T : null;
    }

    private static Color LerCor(MenuItemHUD hud, string campo, Color padrao)
    {
        if (hud == null) return padrao;

        SerializedProperty p = new SerializedObject(hud).FindProperty(campo);
        return p != null ? p.colorValue : padrao;
    }

    private static TMP_Text CriarTextoComoOutro(TMP_Text modelo, Transform pai, string nome, string conteudo)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        var texto = go.AddComponent<TextMeshProUGUI>();
        if (modelo != null)
        {
            texto.font = modelo.font;
            texto.fontSharedMaterial = modelo.fontSharedMaterial;
            texto.fontSize = modelo.fontSize * 0.85f;
            texto.fontStyle = modelo.fontStyle;
            texto.color = modelo.color;
            texto.characterSpacing = modelo.characterSpacing;
        }
        texto.text = conteudo;
        texto.raycastTarget = false;
        return texto;
    }

    private static void PintarImagem(Transform t, Color cor)
    {
        if (t == null) return;

        Image img = t.GetComponent<Image>();
        if (img != null) img.color = cor;
    }

    /// <summary>Evita que um LayoutGroup da linha reposicione o controle novo.</summary>
    private static void IgnorarLayout(GameObject go)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    private static void Aviso(string mensagem)
    {
        EditorUtility.DisplayDialog("Painel de Opções", mensagem, "OK");
    }
}