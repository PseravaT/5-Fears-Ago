using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0618 // enableWordWrapping aparece como obsoleto em algumas versões do TextMeshPro, mas funciona

/// <summary>
/// COLOQUE ESTE ARQUIVO NA PASTA "Editor" (ex.: Assets/Scripts/Editor).
/// Abra a cena de jogo e use Tools > Itens > Criar dica de interação nos itens coletáveis.
///
/// Para cada ItemColetavel da cena (ou só dos objetos que você selecionou), cria um texto
/// "P para pegar" na parte de baixo da tela, com a mesma fonte do texto "pressione Q..." da inspeção,
/// deixa ele desligado e arrasta para o campo "Dica De Interacao" do item.
/// Itens que já têm uma dica são pulados.
/// </summary>
public static class CriadorDaDicaDeInteracao
{
    private const string NomeDoDesfazer = "Criar dica de interação";

    [MenuItem("Tools/Itens/Criar dica de interação nos itens coletáveis")]
    private static void Criar()
    {
        // ---------- QUAIS ITENS ----------
        var itens = new List<ItemColetavel>();
        var vistos = new HashSet<ItemColetavel>();

        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (ItemColetavel i in go.GetComponentsInChildren<ItemColetavel>(true))
                if (vistos.Add(i)) itens.Add(i);
        }

        if (itens.Count == 0)
        {
            foreach (ItemColetavel i in Object.FindObjectsByType<ItemColetavel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (vistos.Add(i)) itens.Add(i);
        }

        if (itens.Count == 0)
        {
            Aviso("Não achei nenhum ItemColetavel. Abra a cena de jogo (ou selecione o objeto do item) e tente de novo.");
            return;
        }

        // ---------- ESTILO E LUGAR DO TEXTO ----------
        // Usa o texto "pressione Q..." da inspeção como modelo de fonte e cor
        TMP_Text modelo = null;
        var inspecao = Object.FindFirstObjectByType<GerenciadorDeInspecao>(FindObjectsInactive.Include);
        if (inspecao != null) modelo = inspecao.textoBotaoQ;

        Canvas canvas = AcharCanvas(modelo);
        bool canvasNovo = false;
        if (canvas == null)
        {
            canvas = CriarCanvas();
            canvasNovo = true;
        }

        // ---------- CRIA UMA DICA PARA CADA ITEM ----------
        int criadas = 0;
        int puladas = 0;

        foreach (ItemColetavel item in itens)
        {
            if (EditorUtility.IsPersistent(item))
            {
                Debug.LogWarning($"[Dica] '{item.name}' é um prefab do Project. Abra a cena e use o item que está nela.", item);
                puladas++;
                continue;
            }

            var so = new SerializedObject(item);
            SerializedProperty campo = so.FindProperty("dicaDeInteracao");
            if (campo == null)
            {
                Debug.LogWarning("[Dica] O ItemColetavel não tem o campo 'dicaDeInteracao'. Use a versão mais nova do script.", item);
                puladas++;
                continue;
            }

            if (campo.objectReferenceValue != null)
            {
                puladas++;
                continue;
            }

            string tecla = NomeDaTecla(so.FindProperty("teclaDeColetar"));
            GameObject dica = CriarTexto(canvas, modelo, "DicaInteracao_" + item.name, tecla + " para pegar");

            campo.objectReferenceValue = dica;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(item.gameObject.scene);
            criadas++;
        }

        if (canvasNovo && criadas == 0 && canvas != null)
            Object.DestroyImmediate(canvas.gameObject);

        Debug.Log($"[Dica] {criadas} dica(s) criada(s), {puladas} item(ns) pulado(s) (já tinham dica ou não eram da cena). Salve a cena (Ctrl+S).");
        Aviso(criadas > 0
            ? $"Criei {criadas} dica(s) de interação. Elas começam desligadas (o script liga na hora certa). Para mudar a posição, selecione 'DicaInteracao_...' na Hierarquia. Salve a cena."
            : "Nenhuma dica nova: todos os itens já tinham uma.");
    }

    // ------------------------------------------------------------------

    /// <summary>Canvas do texto da inspeção; senão o primeiro Canvas Overlay da cena.</summary>
    private static Canvas AcharCanvas(TMP_Text modelo)
    {
        if (modelo != null)
        {
            Canvas c = modelo.GetComponentInParent<Canvas>(true);
            if (c != null) return c.rootCanvas;
        }

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
        }

        return null;
    }

    private static Canvas CriarCanvas()
    {
        var go = new GameObject("CanvasDeDicas", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, NomeDoDesfazer);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Debug.Log("[Dica] A cena não tinha um Canvas para a dica; criei o 'CanvasDeDicas'.", go);
        return canvas;
    }

    private static GameObject CriarTexto(Canvas canvas, TMP_Text modelo, string nome, string conteudo)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, NomeDoDesfazer);
        go.transform.SetParent(canvas.transform, false);

        var texto = go.AddComponent<TextMeshProUGUI>();
        if (modelo != null)
        {
            texto.font = modelo.font;
            texto.fontSharedMaterial = modelo.fontSharedMaterial;
            texto.fontSize = modelo.fontSize;
            texto.fontStyle = modelo.fontStyle;
            texto.color = modelo.color;
            texto.characterSpacing = modelo.characterSpacing;
        }
        else
        {
            texto.fontSize = 28f;
            texto.color = Color.white;
        }

        texto.text = conteudo;
        texto.alignment = TextAlignmentOptions.Center;
        texto.enableWordWrapping = false;
        texto.raycastTarget = false;

        // Parte de baixo da tela, no centro
        RectTransform r = texto.rectTransform;
        r.anchorMin = new Vector2(0.5f, 0f);
        r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(700f, 60f);
        r.anchoredPosition = new Vector2(0f, 90f);

        go.SetActive(false); // o ItemColetavel liga e desliga sozinho
        return go;
    }

    private static string NomeDaTecla(SerializedProperty propriedade)
    {
        if (propriedade == null) return "P";

        int i = propriedade.enumValueIndex;
        string[] nomes = propriedade.enumDisplayNames;
        return i >= 0 && i < nomes.Length ? nomes[i] : "P";
    }

    private static void Aviso(string mensagem)
    {
        EditorUtility.DisplayDialog("Dica de interação", mensagem, "OK");
    }
}
