using UnityEngine;

/// <summary>
/// Coloque no objeto RAIZ "TelaDeGameOver". Quando o Painel da tela de game over aparece (o jogador morreu),
/// apaga o save do slot ativo (puzzles, inventário, cena e data). O "Continuar" do menu deixa de ter esse save.
/// </summary>
public class ApagarSaveAoMorrer : MonoBehaviour
{
    [Tooltip("Objeto que aparece quando o jogador morre. Se vazio, usa o filho chamado 'Painel'")]
    [SerializeField] private GameObject painelDoGameOver;

    [Tooltip("Ligado: apaga todos os slots, não só o ativo")]
    [SerializeField] private bool apagarTodosOsSlots = false;

    private bool jaApagou;

    void Awake()
    {
        if (painelDoGameOver == null)
        {
            Transform t = transform.Find("Painel");
            if (t != null) painelDoGameOver = t.gameObject;
        }

        if (painelDoGameOver == null)
            Debug.LogWarning("[ApagarSaveAoMorrer] Não achei o 'Painel'. Arraste-o no campo 'Painel Do Game Over'.", this);
    }

    void Update()
    {
        if (jaApagou || painelDoGameOver == null) return;

        if (painelDoGameOver.activeInHierarchy)
        {
            jaApagou = true;
            Apagar();
        }
    }

    public void Apagar()
    {
        if (apagarTodosOsSlots)
        {
            EstadoDoJogo.LimparTodosOsSlots();
            Debug.Log("[ApagarSaveAoMorrer] Todos os slots apagados.", this);
            return;
        }

        int slot = EstadoDoJogo.SlotAtual;
        EstadoDoJogo.ApagarSlot(slot);
        Debug.Log($"[ApagarSaveAoMorrer] Save do slot {slot} apagado.", this);
    }
}