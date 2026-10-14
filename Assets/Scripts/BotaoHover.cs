using UnityEngine;
using UnityEngine.EventSystems;

public class BotaoHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Alvo")]
    public RectTransform alvo;               // o visual que se move (vazio = o próprio botão)

    [Header("Movimento (pixels do Canvas)")]
    public float descerHover = 4f;
    public float descerClique = 2f;          // pixels extras ao clicar

    [Header("Tempo (segundos)")]
    public float duracao = 0.08f;            // tempo para percorrer o deslocamento do hover

    [Header("Som (opcional)")]
    public AudioSource fonte;
    public AudioClip somHover;

    Vector2 posicaoBase;
    float deslocamentoAtual;
    float deslocamentoAlvo;
    bool hover, pressionado, selecionado;

    void Awake()
    {
        if (alvo == null) alvo = (RectTransform)transform;
        posicaoBase = alvo.anchoredPosition;
    }

    void OnDisable()
    {
        deslocamentoAtual = deslocamentoAlvo = 0;
        alvo.anchoredPosition = posicaoBase;
        hover = pressionado = selecionado = false;
    }

    void Update()
    {
        bool ativo = hover || selecionado;
        deslocamentoAlvo = ativo ? descerHover + (pressionado ? descerClique : 0f) : 0f;

        float velocidade = descerHover / Mathf.Max(0.001f, duracao);   // pixels por segundo
        deslocamentoAtual = Mathf.MoveTowards(deslocamentoAtual, deslocamentoAlvo,
                                              velocidade * Time.unscaledDeltaTime);

        alvo.anchoredPosition = posicaoBase + new Vector2(0, -Mathf.Round(deslocamentoAtual));
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        if (somHover && fonte) fonte.PlayOneShot(somHover);
    }
    public void OnPointerExit(PointerEventData e) { hover = false; pressionado = false; }
    public void OnPointerDown(PointerEventData e) { pressionado = true; }
    public void OnPointerUp(PointerEventData e) { pressionado = false; }
    public void OnSelect(BaseEventData e) { selecionado = true; }
    public void OnDeselect(BaseEventData e) { selecionado = false; }
}