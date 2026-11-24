using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vincula um objeto e seu Interagivel a um puzzle específico.
/// Enquanto o puzzle não for concluído, o Interagivel fica desabilitado (não aparece no Sistema Dot
/// e não permite interação) e, opcionalmente, o visual do objeto fica escondido.
/// Assim que o puzzle concluir (nesta cena ou vindo de outra), o objeto e o Dot aparecem automaticamente!
/// </summary>
public class LiberarAposPuzzle : MonoBehaviour
{
    [Header("Vínculo com o Puzzle")]
    [Tooltip("ID do puzzle que libera este objeto (ex: 'caldeirao' ou 'escritorio'). Deixe vazio se depender apenas de um item coletado.")]
    [SerializeField] private string idDoPuzzleNecessario = "caldeirao";

    [Tooltip("Opcional: se preencher um ItemData aqui (ex: GarrafaComMensagem), este objeto só libera e persiste habilitado DEPOIS que esse item for coletado!")]
    [SerializeField] private ItemData itemColetadoQueLibera;

    [Header("Comportamento Visual e Interação")]
    [Tooltip("Se marcado, esconde os Renderers e Colliders deste objeto enquanto o puzzle não terminar (mantendo este script ativo para escutar o fim do puzzle).")]
    [SerializeField] private bool ocultarObjetoAntesDoPuzzle = true;

    [Tooltip("Opcional: objeto filho específico para ligar só quando o puzzle terminar. Se vazio, controla os Renderers/Colliders deste próprio objeto.")]
    [SerializeField] private GameObject objetoVisualParaMostrar;

    [Tooltip("Opcional: arraste o Interagivel (se vazio, busca neste mesmo objeto).")]
    [SerializeField] private Interagivel interagivelAlvo;

    [Header("Evento ao Liberar (Opcional)")]
    public UnityEvent aoLiberar;

    private Renderer[] renderers;
    private Collider[] colisores;
    private bool jaLiberado = false;

    void Awake()
    {
        if (interagivelAlvo == null)
            interagivelAlvo = GetComponentInChildren<Interagivel>(true);

        renderers = GetComponentsInChildren<Renderer>(true);
        colisores = GetComponentsInChildren<Collider>(true);
    }

    void OnEnable()
    {
        EstadoDoJogo.AoConcluirPuzzle += AoConcluirAlgumPuzzle;
        AtualizarEstado();
    }

    void Start()
    {
        AtualizarEstado();
    }

    void OnDisable()
    {
        EstadoDoJogo.AoConcluirPuzzle -= AoConcluirAlgumPuzzle;
    }

    private void AoConcluirAlgumPuzzle(string idConcluido)
    {
        bool batePuzzle = !string.IsNullOrWhiteSpace(idDoPuzzleNecessario) &&
                          string.Equals(idConcluido, idDoPuzzleNecessario.Trim(), System.StringComparison.OrdinalIgnoreCase);

        bool bateItem = itemColetadoQueLibera != null &&
                        string.Equals(idConcluido, "coletado_" + itemColetadoQueLibera.Id, System.StringComparison.OrdinalIgnoreCase);

        if (batePuzzle || bateItem)
        {
            AtualizarEstado();
        }
    }

    public void AtualizarEstado()
    {
        bool puzzleOk = string.IsNullOrWhiteSpace(idDoPuzzleNecessario) ||
                        EstadoDoJogo.PuzzleConcluido(idDoPuzzleNecessario.Trim());

        bool itemOk = itemColetadoQueLibera == null ||
                      EstadoDoJogo.PuzzleConcluido("coletado_" + itemColetadoQueLibera.Id);

        bool liberado = puzzleOk && itemOk;

        if (interagivelAlvo != null)
        {
            // Se o objeto já foi coletado anteriormente (ItemParaInspecionar), não reabilita
            bool jaColetado = TryGetComponent<ItemParaInspecionar>(out var itemInsp) &&
                              itemInsp.itemParaInventario != null &&
                              EstadoDoJogo.PuzzleConcluido("coletado_" + itemInsp.itemParaInventario.Id);

            interagivelAlvo.SetEnabled(liberado && !jaColetado);
        }

        if (ocultarObjetoAntesDoPuzzle)
        {
            if (objetoVisualParaMostrar != null)
            {
                objetoVisualParaMostrar.SetActive(liberado);
            }
            else
            {
                if (renderers != null)
                {
                    foreach (var r in renderers)
                        if (r != null) r.enabled = liberado;
                }
                if (colisores != null)
                {
                    foreach (var c in colisores)
                        if (c != null) c.enabled = liberado;
                }
            }
        }

        if (liberado && !jaLiberado)
        {
            jaLiberado = true;
            aoLiberar?.Invoke();
        }
    }
}
