using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// DIAGNÓSTICO TEMPORÁRIO (pode apagar depois). NÃO vai na pasta Editor.
/// Adicione no objeto "Painel" do menu de pausa (ou em qualquer objeto acima do slider).
/// Cada vez que a pausa abre, escreve no Console o estado do slider de volume: se está ativo,
/// onde está na tela, se alguma parte está invisível e QUEM está desenhado por cima dele.
/// </summary>
public class DiagnosticoDoSlider : MonoBehaviour
{
    [Tooltip("Opcional. Se ficar vazio, usa o primeiro Slider encontrado dentro deste objeto.")]
    [SerializeField] private Slider slider;

    void OnEnable()
    {
        StartCoroutine(Rodar());
    }

    private IEnumerator Rodar()
    {
        // espera o layout assentar (pausa usa timeScale 0, então espera por frames, não por tempo)
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (slider == null) slider = GetComponentInChildren<Slider>(true);
        if (slider == null)
        {
            Debug.LogWarning("[DiagSlider] Não achei nenhum Slider dentro de " + name, this);
            yield break;
        }

        var sb = new StringBuilder("[DiagSlider] Estado do slider quando a pausa abriu\n");

        sb.AppendLine($"  slider: ativoNaHierarquia={slider.gameObject.activeInHierarchy}  ativoSelf={slider.gameObject.activeSelf}  " +
                      $"enabled={slider.enabled}  valor={slider.value:0.00}  min={slider.minValue}  max={slider.maxValue}");
        sb.AppendLine($"  fillRect={(slider.fillRect != null ? slider.fillRect.name : "NULL")}  handleRect={(slider.handleRect != null ? slider.handleRect.name : "NULL")}");

        // Pais desligados ou com CanvasGroup transparente
        for (Transform t = slider.transform; t != null; t = t.parent)
        {
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (!t.gameObject.activeSelf || (cg != null && cg.alpha < 1f))
                sb.AppendLine($"  >> pai '{t.name}': ativoSelf={t.gameObject.activeSelf}  canvasGroup.alpha={(cg != null ? cg.alpha.ToString("0.00") : "-")}");
        }

        // Cada parte visual do slider
        foreach (Graphic g in slider.GetComponentsInChildren<Graphic>(true))
        {
            RectTransform r = g.rectTransform;
            var cantos = new Vector3[4];
            r.GetWorldCorners(cantos);

            sb.AppendLine($"  {Caminho(g.transform, slider.transform)}: ativo={g.gameObject.activeInHierarchy} enabled={g.enabled} " +
                          $"corA={g.color.a:0.00} alphaHerdado={g.canvasRenderer.GetInheritedAlpha():0.00} cull={g.canvasRenderer.cull} " +
                          $"tamanho=({r.rect.width:0.#} x {r.rect.height:0.#}) cantoInfEsq=({cantos[0].x:0},{cantos[0].y:0}) cantoSupDir=({cantos[2].x:0},{cantos[2].y:0})");
        }

        // Quem está desenhado por cima do slider? (só enxerga elementos com Raycast Target ligado)
        EventSystem es = EventSystem.current;
        RectTransform rs = (RectTransform)slider.transform;
        if (es != null)
        {
            var c = new Vector3[4];
            rs.GetWorldCorners(c);
            Vector3 centroMundo = (c[0] + c[2]) * 0.5f;

            Canvas canvas = slider.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            Vector2 tela = RectTransformUtility.WorldToScreenPoint(cam, centroMundo);

            var dados = new PointerEventData(es) { position = tela };
            var resultados = new List<RaycastResult>();
            es.RaycastAll(dados, resultados);

            var nomes = new List<string>();
            foreach (RaycastResult rr in resultados) nomes.Add(Caminho(rr.gameObject.transform, transform));

            sb.AppendLine($"  Centro do slider na tela = ({tela.x:0},{tela.y:0}). Elementos nesse ponto, de CIMA para BAIXO:");
            sb.AppendLine("    " + (nomes.Count > 0 ? string.Join("\n    ", nomes) : "(nenhum)"));
        }
        else
        {
            sb.AppendLine("  EventSystem.current é nulo: não deu para testar o que está por cima.");
        }

        Debug.Log(sb.ToString(), slider);
    }

    private static string Caminho(Transform alvo, Transform raiz)
    {
        string caminho = alvo.name;
        for (Transform t = alvo.parent; t != null && t != raiz; t = t.parent)
            caminho = t.name + "/" + caminho;
        return caminho;
    }
}
