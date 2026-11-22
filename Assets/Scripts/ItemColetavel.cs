using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Coloque no objeto que pode ser pego (ex.: o prefab do relógio dentro do cofre).
/// Não precisa de Collider nem de mirar: com as condições certas (ex.: cofre aberto),
/// apertar a tecla coloca o item no inventário e esconde o objeto.
/// </summary>
public class ItemColetavel : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData item;

    [Header("Condição (opcional)")]
    [Tooltip("Se preenchido, o item só pode ser pego depois que este cofre estiver aberto")]
    [SerializeField] private ValvulaCofre cofreNecessario;
    [Tooltip("Se ligado, só dá para pegar enquanto o objeto estiver sendo inspecionado")]
    [SerializeField] private bool soDuranteAInspecao = false;

    [Header("Interação")]
    [Tooltip("Evite o E: ele fecha a inspeção")]
    [SerializeField] private Key teclaDeColetar = Key.P;
    [Tooltip("Texto tipo 'P para pegar' (opcional). Aparece enquanto o item pode ser pego")]
    [SerializeField] private GameObject dicaDeInteracao;

    [Header("Diagnóstico")]
    [Tooltip("Escreve no Console o motivo quando você aperta a tecla e o item não é coletado")]
    [SerializeField] private bool mostrarDiagnostico = true;

    [Header("Eventos")]
    public UnityEvent aoColetar;

    private bool coletado;

    private string ChaveDeColeta => "coletado_" + item.Id;

    void Start()
    {
        if (item == null)
        {
            Debug.LogWarning("[ItemColetavel] Falta o ItemData.", this);
            enabled = false;
            return;
        }

        // Se já foi pego antes (ex.: o jogador voltou para esta cena ou reabriu o cofre), o objeto não reaparece
        if (EstadoDoJogo.PuzzleConcluido(ChaveDeColeta))
        {
            coletado = true;
            gameObject.SetActive(false);
            return;
        }

        MostrarDica(false);
    }

    void Update()
    {
        if (coletado || Keyboard.current == null) return;

        bool podeColetar = PodeColetar(out string motivo);
        MostrarDica(podeColetar);

        if (!Keyboard.current[teclaDeColetar].wasPressedThisFrame) return;

        if (podeColetar)
            Coletar();
        else if (mostrarDiagnostico)
            Debug.Log($"[ItemColetavel:{name}] Não coletou: {motivo}", this);
    }

    /// <summary>Pode ser chamado também por um UnityEvent ou outro script.</summary>
    public void Coletar()
    {
        if (coletado || item == null) return;
        if (cofreNecessario != null && !cofreNecessario.Aberto) return;

        if (GerenciadorDeInventario.Instancia == null)
        {
            Debug.LogWarning("[ItemColetavel] Não achei o GerenciadorDeInventario na cena.", this);
            return;
        }

        GerenciadorDeInventario.Instancia.Adicionar(item);

        coletado = true;
        EstadoDoJogo.MarcarPuzzleConcluido(ChaveDeColeta);

        MostrarDica(false);
        aoColetar?.Invoke();
        gameObject.SetActive(false);
    }

    private bool PodeColetar(out string motivo)
    {
        motivo = "";

        if (cofreNecessario != null && !cofreNecessario.Aberto)
        {
            motivo = "o ValvulaCofre indicado em 'Cofre Necessario' ainda não está Aberto (confira se é o mesmo cofre que você abriu).";
            return false;
        }
        if (soDuranteAInspecao && !(GerenciadorDeInspecao.Instancia != null && GerenciadorDeInspecao.Instancia.EstaInspecionando))
        {
            motivo = "'So Durante A Inspecao' está ligado e não há inspeção em andamento.";
            return false;
        }
        if (GerenciadorDeInventario.Instancia != null && GerenciadorDeInventario.Instancia.Aberto)
        {
            motivo = "o inventário está aberto.";
            return false;
        }
        if (GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
        {
            motivo = "há um diálogo ativo.";
            return false;
        }
        return true;
    }

    private void MostrarDica(bool mostrar)
    {
        if (dicaDeInteracao != null && dicaDeInteracao.activeSelf != mostrar)
            dicaDeInteracao.SetActive(mostrar);
    }

    void OnDisable()
    {
        MostrarDica(false);
    }
}