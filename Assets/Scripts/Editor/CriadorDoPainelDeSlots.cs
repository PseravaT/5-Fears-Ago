using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// COLOQUE ESTE ARQUIVO NA PASTA "Editor", JUNTO COM o CriadorDoPainelDeOpcoes.cs (os dois formam a mesma classe).
/// Abra a cena do Menu e use Tools > Menu > Criar Painel de Slots.
///
/// Copia o painel do menu (moldura, título e visual dos botões) e monta: Slot 1, Slot 2, Slot 3 e Voltar.
/// O mesmo painel serve para NOVO JOGO (escolher onde começar) e CONTINUAR (escolher qual carregar).
/// </summary>
public static partial class CriadorDoPainelDeOpcoes
{
    private const string NomeDoDesfazerSlots = "Criar Painel de Slots";

    [MenuItem("Tools/Menu/Criar Painel de Slots")]
    private static void CriarSlots()
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

        if (menu.painelSlots != null)
        {
            bool substituir = EditorUtility.DisplayDialog(
                NomeDoDesfazerSlots,
                "O Menu já tem um painel de slots ligado. Substituir por um novo?",
                "Substituir", "Cancelar");
            if (!substituir) return;

            Undo.DestroyObjectImmediate(menu.painelSlots.gameObject);
        }

        // ---------- COPIA O PAINEL DO MENU ----------
        GameObject copia = Object.Instantiate(menu.grupoMenu.gameObject, menu.grupoMenu.transform.parent);
        copia.name = "PainelSlots";
        Undo.RegisterCreatedObjectUndo(copia, NomeDoDesfazerSlots);
        copia.transform.SetSiblingIndex(menu.grupoMenu.transform.GetSiblingIndex() + 1);

        RemoverScriptsQueNaoSaoDeUI(copia);

        // Botões da cópia, de cima para baixo. O de cima serve de modelo de visual.
        var botoes = new List<Button>(copia.GetComponentsInChildren<Button>(true));
        botoes.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        if (botoes.Count < 1)
        {
            Object.DestroyImmediate(copia);
            Aviso("O painel do menu precisa ter pelo menos 1 botão para eu usar como modelo de visual.");
            return;
        }

        Button modelo = botoes[0];
        float altura = AlturaDaLinha(Rect(modelo));

        // Título (antes de mexer nos botões, para ignorar o texto que está dentro deles)
        TMP_Text titulo = null;
        foreach (TMP_Text t in copia.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.GetComponentInParent<Button>(true) != null) continue;
            if (titulo == null || t.fontSize > titulo.fontSize) titulo = t;
        }
        if (titulo != null)
        {
            // O título pode ter camadas sobrepostas (relevo): troca o texto de todas
            foreach (TMP_Text camada in titulo.GetComponentsInChildren<TMP_Text>(true))
                camada.text = "NOVO JOGO";
        }

        RectTransform moldura = AcharMoldura(copia, modelo, titulo);
        larguraDoModelo = LarguraDaLinha(Rect(modelo));

        // ---------- LINHAS: 3 slots + voltar ----------
        var slots = new Button[3];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = ClonarLinha(modelo, moldura, "BotaoSlot" + (i + 1));

        Button botaoVoltar = modelo;
        botaoVoltar.name = "BotaoVoltarSlots";

        // Os outros botões da cópia não servem mais
        foreach (Button b in botoes)
        {
            if (b != modelo) Object.DestroyImmediate(b.gameObject);
        }

        botaoVoltar.transform.SetParent(moldura, false);

        for (int i = 0; i < slots.Length; i++)
        {
            PosicionarLinha(Rect(slots[i]), TopoPrimeiraLinha + PassoEntreLinhas * i, altura, MargemLateral, 1f - MargemLateral);
            PrepararLinhaDeSlot(slots[i], "Slot " + (i + 1) + "  -  Vazio", altura);
        }

        PosicionarLinha(Rect(botaoVoltar), PosicaoVoltar, altura, 0.5f - LarguraVoltar / 2f, 0.5f + LarguraVoltar / 2f);
        DefinirTexto(botaoVoltar, "Voltar");
        LimparCliquesDoInspector(botaoVoltar);

        // Navegação com teclado/controle: de cima para baixo
        slots[0].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = slots[1] };
        slots[1].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = slots[0], selectOnDown = slots[2] };
        slots[2].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = slots[1], selectOnDown = botaoVoltar };
        botaoVoltar.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = slots[2] };

        // ---------- LIGA NO MENU ----------
        var soMenu = new SerializedObject(menu);
        Atribuir(soMenu, "painelSlots", copia.GetComponent<CanvasGroup>());
        Atribuir(soMenu, "botaoVoltarSlots", botaoVoltar);
        Atribuir(soMenu, "tituloSlots", titulo);

        SerializedProperty lista = soMenu.FindProperty("botoesSlot");
        if (lista != null)
        {
            lista.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                lista.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }
        else
        {
            Debug.LogWarning("[Menu] O componente Menu não tem o campo 'botoesSlot'. Substitua o Menu.cs pela versão nova e rode de novo.");
        }

        soMenu.ApplyModifiedProperties();

        Selection.activeGameObject = copia;
        EditorGUIUtility.PingObject(copia);
        EditorSceneManager.MarkSceneDirty(copia.scene);

        Debug.Log("[Menu] Painel de Slots criado e ligado no componente Menu. No Editor ele aparece sobre o menu; no Play começa escondido. Salve a cena (Ctrl+S).", copia);
    }

    /// <summary>Deixa a linha larga: estica o fundo, centraliza o rótulo (com espaço para a seta) e limpa os cliques antigos.</summary>
    private static void PrepararLinhaDeSlot(Button linha, string texto, float altura)
    {
        RectTransform rect = Rect(linha);
        MenuItemHUD hud = linha.GetComponent<MenuItemHUD>();
        TMP_Text rotulo = EncontrarRotulo(linha, hud);
        Graphic seta = LerReferencia<Graphic>(hud, "seta");

        // Qualquer elemento do botão com largura fixa passa a acompanhar a linha larga
        EsticarDecoracaoDaLinha(rect, rotulo, seta);

        if (rotulo != null)
        {
            // O texto do slot é longo: encolhe até caber entre a seta e a borda direita
            float tamanho = rotulo.fontSize;
            rotulo.enableAutoSizing = true;
            rotulo.fontSizeMax = tamanho;
            rotulo.fontSizeMin = tamanho * 0.5f;

            rotulo.text = texto;
            rotulo.enableWordWrapping = false;
            rotulo.alignment = TextAlignmentOptions.Center;

            if (rotulo.rectTransform != rect)
            {
                Ancorar(rotulo.rectTransform, 0f, 0f, 1f, 1f);
                rotulo.rectTransform.offsetMin = new Vector2(altura * 1.1f, 0f);   // espaço para a seta, simétrico
                rotulo.rectTransform.offsetMax = new Vector2(-altura * 1.1f, 0f);
                rotulo.gameObject.name = "Texto"; // o Menu procura o filho "Texto" para atualizar o rótulo do slot
            }
        }

        if (seta != null && seta.rectTransform != rect)
        {
            // seta presa ao canto esquerdo da linha, não ao centro dela
            RectTransform rs = seta.rectTransform;
            rs.anchorMin = rs.anchorMax = new Vector2(0f, 0.5f);
            rs.anchoredPosition = new Vector2(altura * 0.5f, 0f);
        }

        LimparCliquesDoInspector(linha);
    }
}