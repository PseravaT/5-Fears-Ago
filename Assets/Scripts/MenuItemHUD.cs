using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Item de menu estilo terminal. Fundo e borda ficam SEMPRE visíveis (a borda acende ao
/// selecionar). A seta só aparece no item selecionado (mouse por cima, teclado ou controle).
/// Coloque no mesmo objeto do Button (ou de qualquer Selectable).
/// </summary>
[RequireComponent(typeof(Selectable))]
public class MenuItemHUD : MonoBehaviour,
    ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [Header("Elementos")]
    [SerializeField] Image fundo;       // fundo do item (opcional)
    [SerializeField] Graphic borda;     // contorno fixo do item (Image ou TMP, opcional)
    [SerializeField] Graphic seta;      // seta fixa (Image ou TMP, opcional)
    [SerializeField] TMP_Text texto;

    [Header("Comportamento")]
    [SerializeField] bool fundoSempreVisivel = true;    // true: fundo sempre ligado
    [SerializeField] bool setaSoNoSelecionado = true;   // true: seta só aparece no item selecionado

    [Header("Cores - item normal")]
    [SerializeField] Color corTextoNormal  = new Color(0.66f, 0.90f, 0.77f, 1f);
    [SerializeField] Color corBordaNormal  = new Color(0.44f, 0.66f, 0.54f, 0.55f);
    [SerializeField] Color corSetaNormal   = new Color(0.44f, 0.66f, 0.54f, 0.55f);

    [Header("Cores - item selecionado")]
    [SerializeField] Color corTextoSelecionado = new Color(0.80f, 1f, 0.90f, 1f);
    [SerializeField] Color corBordaSelecionada = new Color(0.66f, 0.90f, 0.77f, 1f);
    [SerializeField] Color corSetaSelecionada  = new Color(0.91f, 0.97f, 0.93f, 1f);

    [Header("Som (opcional)")]
    [SerializeField] AudioSource fonteDeSom;
    [SerializeField] AudioClip somNavegar;

    // Guarda o último item selecionado, para não perder a seleção ao clicar fora
    static GameObject ultimoSelecionado;

    void OnEnable() => Aplicar(false);

    void Update()
    {
        var es = EventSystem.current;
        if (es != null && es.currentSelectedGameObject == null && ultimoSelecionado == gameObject)
            es.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        ultimoSelecionado = gameObject;
        Aplicar(true);

        // Evita tocar som na seleção inicial, logo ao abrir o menu
        if (fonteDeSom != null && somNavegar != null && Time.timeSinceLevelLoad > 0.3f)
            fonteDeSom.PlayOneShot(somNavegar);
    }

    public void OnDeselect(BaseEventData eventData) => Aplicar(false);

    // O mouse passando por cima também seleciona, igual ao teclado
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    void Aplicar(bool selecionado)
    {
        if (fundo != null) fundo.enabled = fundoSempreVisivel || selecionado;
        Pintar(texto, selecionado ? corTextoSelecionado : corTextoNormal);
        Pintar(borda, selecionado ? corBordaSelecionada : corBordaNormal);
        if (seta != null) seta.enabled = !setaSoNoSelecionado || selecionado;
        Pintar(seta,  selecionado ? corSetaSelecionada  : corSetaNormal);
    }

    static void Pintar(Graphic g, Color c)
    {
        if (g != null) g.color = c;
    }
}