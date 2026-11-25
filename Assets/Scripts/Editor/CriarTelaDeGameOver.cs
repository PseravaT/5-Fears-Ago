#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor.Events;

/// <summary>
/// COLOQUE ESTE ARQUIVO NA PASTA Assets/Scripts/Editor.
/// Tools > Menu > Criar Tela de Game Over: monta a tela inteira no Canvas da cena.
/// Tools > Menu > Refazer HUD da Tela de Game Over: troca só a HUD (caixa + botões) de uma tela que já existe,
/// mantendo vídeo, quadros, cenas e demais configurações. Selecione o PREFAB na janela Project antes de rodar
/// (ou deixe uma TelaDeGameOver solta na cena aberta).
/// A HUD segue o padrão do MenuDePausa.
/// </summary>
public static class CriarTelaDeGameOver
{
    // Mesma fonte (VT323) e mesma seta do menu de pausa
    private const string GuidDaFonte = "5310bee9c2211544b8086c1b80b560ed";
    private const string GuidDaSeta = "1f5292b5001737d4a83deb55f483cf16";

    // Cores copiadas do MenuDePausa
    private static readonly Color CorTextoNormal = new Color(0.7921569f, 0.5294118f, 0.2784314f, 1f);
    private static readonly Color CorTextoSelecionado = new Color(0.9716981f, 0.18006755f, 0f, 1f);
    private static readonly Color CorBordaNormal = new Color(0.21568628f, 0.14509805f, 0.13725491f, 0.56078434f);
    private static readonly Color CorBordaSelecionada = new Color(1f, 0.009791819f, 0f, 1f);
    private static readonly Color CorSetaNormal = new Color(0.44f, 0.66f, 0.54f, 0.55f);
    private static readonly Color CorSetaSelecionada = new Color(0.91f, 0.97f, 0.93f, 1f);
    private static readonly Color CorSombraDoBotao = new Color(0.20754719f, 0.13744338f, 0.1262905f, 0.4392157f);
    private static readonly Color CorFundoDoBotao = new Color(0.13333334f, 0.07579368f, 0.07450981f, 1f);
    private static readonly Color CorContorno = new Color(0f, 0f, 0f, 0.5f);

    private static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 CantoSuperiorEsquerdo = new Vector2(0f, 1f);

    private const float LarguraDoBotao = 500f;   // mais largo que os 350 do pausa só para caber "Tentar novamente" sem encolher a letra
    private const float AlturaDoBotao = 80f;
    private const float EspacoEntreBotoes = 20f;
    private const float MargemDaCaixa = 30f;

    // ---------- CRIAR TELA COMPLETA ----------

    [MenuItem("Tools/Menu/Criar Tela de Game Over")]
    private static void Criar()
    {
        Canvas canvas = AcharCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Tela de Game Over", "Não achei nenhum Canvas na cena aberta.", "OK");
            return;
        }

        if (Object.FindFirstObjectByType<TelaDeGameOver>(FindObjectsInactive.Include) != null)
        {
            bool continuar = EditorUtility.DisplayDialog(
                "Tela de Game Over",
                "Esta cena já tem uma TelaDeGameOver. Criar outra mesmo assim?",
                "Criar outra", "Cancelar");
            if (!continuar) return;
        }

        TMP_FontAsset fonte = CarregarFonte();

        // Raiz: nunca fica desativada (guarda o script e o VideoPlayer)
        GameObject raiz = Novo("TelaDeGameOver", canvas.transform);
        Undo.RegisterCreatedObjectUndo(raiz, "Criar Tela de Game Over");
        Esticar(raiz.GetComponent<RectTransform>());
        raiz.transform.SetAsLastSibling();

        VideoPlayer video = raiz.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        TelaDeGameOver tela = raiz.AddComponent<TelaDeGameOver>();

        // Painel: fundo preto que cobre a tela toda (começa escondido pelo script)
        GameObject painel = Novo("Painel", raiz.transform);
        Esticar(painel.GetComponent<RectTransform>());
        Image fundo = painel.AddComponent<Image>();
        fundo.color = Color.black;
        fundo.raycastTarget = true;
        CanvasGroup grupo = painel.AddComponent<CanvasGroup>();

        // Vídeo (começa desativado: um RawImage sem textura apareceria como um quadrado branco)
        GameObject objVideo = Novo("Video", painel.transform);
        Esticar(objVideo.GetComponent<RectTransform>());
        RawImage imagemDoVideo = objVideo.AddComponent<RawImage>();
        imagemDoVideo.raycastTarget = false;
        AspectRatioFitter ajuste = objVideo.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = 16f / 9f;
        objVideo.SetActive(false);

        // Sequência de imagens (começa desativada pelo mesmo motivo)
        GameObject objQuadros = Novo("Quadros", painel.transform);
        Esticar(objQuadros.GetComponent<RectTransform>());
        Image imagemDosQuadros = objQuadros.AddComponent<Image>();
        imagemDosQuadros.preserveAspect = true;
        imagemDosQuadros.raycastTarget = false;
        objQuadros.SetActive(false);

        // HUD no padrão do menu de pausa
        GameObject area = MontarHud(painel.transform, fonte, out Button botaoTentar, out Button botaoMenu);

        // Liga os campos privados do script
        SerializedObject so = new SerializedObject(tela);
        Ligar(so, "painel", painel);
        Ligar(so, "grupoDoPainel", grupo);
        Ligar(so, "areaDosBotoes", area);
        Ligar(so, "botaoTentarNovamente", botaoTentar);
        Ligar(so, "botaoMenu", botaoMenu);
        Ligar(so, "videoPlayer", video);
        Ligar(so, "imagemDoVideo", imagemDoVideo);
        Ligar(so, "imagemDosQuadros", imagemDosQuadros);
        so.ApplyModifiedPropertiesWithoutUndo();

        // O painel começa escondido; o script também garante isso ao iniciar
        painel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        EditorGUIUtility.PingObject(raiz);

        Debug.Log("[TelaDeGameOver] Criada. Arraste o vídeo para o campo 'Video Clip' do componente TelaDeGameOver.", raiz);
    }

    // ---------- REFAZER SÓ A HUD ----------

    [MenuItem("Tools/Menu/Refazer HUD da Tela de Game Over")]
    private static void Refazer()
    {
        Object selecionado = Selection.activeObject;
        string caminho = selecionado != null ? AssetDatabase.GetAssetPath(selecionado) : "";

        // Caso 1: o prefab está selecionado na janela Project
        if (!string.IsNullOrEmpty(caminho) && caminho.EndsWith(".prefab"))
        {
            GameObject conteudo = PrefabUtility.LoadPrefabContents(caminho);
            try
            {
                TelaDeGameOver tela = conteudo.GetComponentInChildren<TelaDeGameOver>(true);
                if (tela == null)
                {
                    EditorUtility.DisplayDialog("Tela de Game Over", "Este prefab não tem um componente TelaDeGameOver.", "OK");
                    return;
                }

                if (!RefazerHud(tela, false)) return;

                PrefabUtility.SaveAsPrefabAsset(conteudo, caminho);
                Debug.Log("[TelaDeGameOver] HUD refeita e prefab salvo: " + caminho);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(conteudo);
            }
            return;
        }

        // Caso 2: uma TelaDeGameOver solta na cena aberta
        TelaDeGameOver daCena = null;
        if (Selection.activeGameObject != null)
            daCena = Selection.activeGameObject.GetComponentInParent<TelaDeGameOver>(true);
        if (daCena == null)
            daCena = Object.FindFirstObjectByType<TelaDeGameOver>(FindObjectsInactive.Include);

        if (daCena == null)
        {
            EditorUtility.DisplayDialog("Tela de Game Over",
                "Selecione o prefab TelaDeGameOver na janela Project (ou abra uma cena que tenha uma).", "OK");
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(daCena.gameObject))
        {
            EditorUtility.DisplayDialog("Tela de Game Over",
                "Esta TelaDeGameOver é uma instância de prefab. Selecione o ARQUIVO do prefab na janela Project e rode o comando de novo: a instância da cena é atualizada junto.",
                "OK");
            return;
        }

        if (RefazerHud(daCena, true))
        {
            EditorSceneManager.MarkSceneDirty(daCena.gameObject.scene);
            Debug.Log("[TelaDeGameOver] HUD refeita.", daCena);
        }
    }

    private static bool RefazerHud(TelaDeGameOver tela, bool naCena)
    {
        SerializedObject so = new SerializedObject(tela);
        GameObject painel = so.FindProperty("painel")?.objectReferenceValue as GameObject;
        if (painel == null)
        {
            EditorUtility.DisplayDialog("Tela de Game Over", "O campo 'Painel' da TelaDeGameOver está vazio.", "OK");
            return false;
        }

        GameObject areaAntiga = so.FindProperty("areaDosBotoes")?.objectReferenceValue as GameObject;
        if (areaAntiga != null)
        {
            if (naCena) Undo.DestroyObjectImmediate(areaAntiga);
            else Object.DestroyImmediate(areaAntiga);
        }

        GameObject area = MontarHud(painel.transform, CarregarFonte(), out Button botaoTentar, out Button botaoMenu);
        if (naCena) Undo.RegisterCreatedObjectUndo(area, "Refazer HUD da Tela de Game Over");

        so.Update();
        Ligar(so, "areaDosBotoes", area);
        Ligar(so, "botaoTentarNovamente", botaoTentar);
        Ligar(so, "botaoMenu", botaoMenu);
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // ---------- HUD NO PADRÃO DO MENU DE PAUSA ----------

    /// <summary>
    /// Monta "AreaDosBotoes" com a caixa escura (Background) e os dois botões empilhados, no padrão do MenuDePausa.
    /// Sem título e sem linha: só a caixa com "Tentar novamente" em cima de "Voltar ao menu".
    /// </summary>
    private static GameObject MontarHud(Transform pai, TMP_FontAsset fonte, out Button botaoTentar, out Button botaoMenu)
    {
        Sprite seta = CarregarSeta();

        // Área que o script mostra/esconde (sem imagem: não bloqueia nada)
        GameObject area = Novo("AreaDosBotoes", pai);
        Esticar(area.GetComponent<RectTransform>());

        // Caixa: mesma escala do menu de pausa
        GameObject caixa = Novo("Caixa", area.transform);
        RectTransform caixaRT = caixa.GetComponent<RectTransform>();
        Ancorar(caixaRT, Centro, Centro, Centro, Vector2.zero, Vector2.zero);
        caixaRT.localScale = Vector3.one * 0.7860371f;

        // Background escuro com contorno, justo em volta dos botões
        float alturaDaLista = AlturaDoBotao * 2f + EspacoEntreBotoes;
        float larguraDaCaixa = LarguraDoBotao + MargemDaCaixa * 2f;
        float alturaDaCaixa = alturaDaLista + MargemDaCaixa * 2f;

        GameObject background = Novo("Background", caixa.transform);
        Ancorar(background.GetComponent<RectTransform>(), Centro, Centro, Centro,
            Vector2.zero, new Vector2(larguraDaCaixa, alturaDaCaixa));
        Image imgBackground = background.AddComponent<Image>();
        imgBackground.color = new Color(0f, 0f, 0f, 0.7607843f);
        imgBackground.raycastTarget = false;
        Contorno(background, new Color(0.24528301f, 0.20748591f, 0.1492524f, 0.33333334f));

        // Lista vertical, como a do pausa, só que com o espaço entre itens que aparece no jogo
        GameObject lista = Novo("Lista", background.transform);
        Ancorar(lista.GetComponent<RectTransform>(), CantoSuperiorEsquerdo, CantoSuperiorEsquerdo, CantoSuperiorEsquerdo,
            new Vector2(MargemDaCaixa, -MargemDaCaixa), new Vector2(LarguraDoBotao, alturaDaLista));
        VerticalLayoutGroup layout = lista.AddComponent<VerticalLayoutGroup>();
        layout.spacing = EspacoEntreBotoes;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        botaoTentar = CriarItem("Item_TentarNovamente", "Tentar novamente", lista.transform, fonte, seta);
        botaoMenu = CriarItem("Item_Menu", "Voltar ao menu", lista.transform, fonte, seta);

        // Navegação com setas/teclado: Tentar novamente <-> Voltar ao menu
        Navigation navTentar = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = botaoMenu };
        botaoTentar.navigation = navTentar;
        Navigation navMenu = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = botaoTentar };
        botaoMenu.navigation = navMenu;

        return area;

        // Quando a tela de game over aparece, o save do slot ativo é apagado
        area.AddComponent<ApagarSaveAoMorrer>();
    }

    /// <summary>Um item igual ao "Item_Continuar" do menu de pausa: borda, sombra, texto com espaçamento entre letras, seta e o MenuItemHUD.</summary>
    private static Button CriarItem(string nome, string rotulo, Transform pai, TMP_FontAsset fonte, Sprite seta)
    {
        GameObject item = Novo(nome, pai);

        // Imagem transparente que recebe o clique
        Image area = item.AddComponent<Image>();
        area.color = new Color(1f, 1f, 1f, 0f);
        area.raycastTarget = true;

        Button botao = item.AddComponent<Button>();
        botao.targetGraphic = area;
        botao.transition = Selectable.Transition.None; // as cores quem troca é o MenuItemHUD

        LayoutElement le = item.AddComponent<LayoutElement>();
        le.preferredHeight = AlturaDoBotao;

        MenuItemHUD hud = item.AddComponent<MenuItemHUD>();

        // Borda (ocupa o item inteiro, começando na esquerda dele)
        GameObject borda = Novo("Borda_Item", item.transform);
        Ancorar(borda.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Centro,
            new Vector2(LarguraDoBotao * 0.5f, 0f), new Vector2(LarguraDoBotao, AlturaDoBotao));
        Image imgBorda = borda.AddComponent<Image>();
        imgBorda.color = CorBordaNormal;
        imgBorda.raycastTarget = false;
        Contorno(borda, CorContorno);

        // Sombra deslocada (o "Fundo" externo) e o fundo escuro dentro dela
        GameObject sombra = Novo("Fundo", borda.transform);
        Ancorar(sombra.GetComponent<RectTransform>(), Centro, Centro, Centro,
            new Vector2(1f, -10f), new Vector2(LarguraDoBotao + 5f, AlturaDoBotao + 10f));
        Image imgSombra = sombra.AddComponent<Image>();
        imgSombra.color = CorSombraDoBotao;
        imgSombra.raycastTarget = false;
        Contorno(sombra, CorContorno);

        GameObject miolo = Novo("Fundo", sombra.transform);
        Ancorar(miolo.GetComponent<RectTransform>(), Centro, Centro, Centro,
            new Vector2(-1f, 10f), new Vector2(LarguraDoBotao, AlturaDoBotao));
        Image imgMiolo = miolo.AddComponent<Image>();
        imgMiolo.color = CorFundoDoBotao;
        imgMiolo.raycastTarget = false;

        // Texto: mesmas margens do pausa (51 da esquerda para dar lugar à seta), com espaçamento entre letras
        float larguraDoTexto = LarguraDoBotao - 56f;
        TextMeshProUGUI texto = CriarTexto("Texto", item.transform, rotulo, 50f, CorTextoNormal, fonte);
        Ancorar(texto.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Centro,
            new Vector2(51f + larguraDoTexto * 0.5f, 0f), new Vector2(larguraDoTexto, 0f));
        texto.characterSpacing = 15f;
        texto.enableAutoSizing = true; // segurança: só encolhe se um rótulo não couber
        texto.fontSizeMin = 18f;
        texto.fontSizeMax = 50f;

        // Seta
        GameObject objSeta = Novo("Seta", item.transform);
        Ancorar(objSeta.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(13.5f, 0f), new Vector2(35f, 35f));
        Image imgSeta = objSeta.AddComponent<Image>();
        imgSeta.sprite = seta;
        imgSeta.color = CorSetaNormal;
        imgSeta.raycastTarget = false;

        // Liga o MenuItemHUD (mesmos campos e cores do prefab de pausa)
        SerializedObject so = new SerializedObject(hud);
        Ligar(so, "fundo", imgSombra);
        Ligar(so, "borda", imgBorda);
        Ligar(so, "seta", imgSeta);
        Ligar(so, "texto", texto);
        LigarBool(so, "fundoSempreVisivel", true);
        LigarBool(so, "setaSoNoSelecionado", true);
        LigarCor(so, "corTextoNormal", CorTextoNormal);
        LigarCor(so, "corBordaNormal", CorBordaNormal);
        LigarCor(so, "corSetaNormal", CorSetaNormal);
        LigarCor(so, "corTextoSelecionado", CorTextoSelecionado);
        LigarCor(so, "corBordaSelecionada", CorBordaSelecionada);
        LigarCor(so, "corSetaSelecionada", CorSetaSelecionada);
        so.ApplyModifiedPropertiesWithoutUndo();

        return botao;
    }

    // ---------- AUXILIARES ----------

    private static Canvas AcharCanvas()
    {
        Canvas[] todos = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas primeiro = null;

        foreach (Canvas c in todos)
        {
            if (!c.isRootCanvas) continue;
            if (c.name == "Canvas") return c;
            if (primeiro == null) primeiro = c;
        }

        return primeiro;
    }

    private static TMP_FontAsset CarregarFonte()
    {
        string caminho = AssetDatabase.GUIDToAssetPath(GuidDaFonte);
        TMP_FontAsset fonte = string.IsNullOrEmpty(caminho)
            ? null
            : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(caminho);

        return fonte != null ? fonte : TMP_Settings.defaultFontAsset;
    }

    private static Sprite CarregarSeta()
    {
        string caminho = AssetDatabase.GUIDToAssetPath(GuidDaSeta);
        if (string.IsNullOrEmpty(caminho)) return null;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        if (sprite != null) return sprite;

        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminho))
            if (o is Sprite s) return s;

        return null;
    }

    private static GameObject Novo(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return go;
    }

    private static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Ancorar(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivo, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivo;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    private static TextMeshProUGUI CriarTexto(string nome, Transform pai, string conteudo, float tamanho, Color cor, TMP_FontAsset fonte)
    {
        GameObject go = Novo(nome, pai);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = conteudo;
        tmp.fontSize = tamanho;
        tmp.color = cor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        if (fonte != null) tmp.font = fonte;
        return tmp;
    }

    private static void Contorno(GameObject go, Color cor)
    {
        Outline contorno = go.AddComponent<Outline>();
        contorno.effectColor = cor;
        contorno.effectDistance = new Vector2(3f, 3f);
    }

    private static void Ligar(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty p = so.FindProperty(campo);
        if (p == null)
        {
            Debug.LogWarning($"[TelaDeGameOver] Não achei o campo '{campo}' em {so.targetObject.GetType().Name}.");
            return;
        }
        p.objectReferenceValue = valor;
    }

    private static void LigarBool(SerializedObject so, string campo, bool valor)
    {
        SerializedProperty p = so.FindProperty(campo);
        if (p != null) p.boolValue = valor;
    }

    private static void LigarCor(SerializedObject so, string campo, Color valor)
    {
        SerializedProperty p = so.FindProperty(campo);
        if (p != null) p.colorValue = valor;
    }
}
#endif