using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Coloque no objeto que pode ser pego (ex.: o relógio dentro do cofre, o diário na mesa).
/// Não precisa de Collider nem de mirar. Ao apertar a tecla, só o item MAIS PERTO do jogador
/// (e dentro da distância máxima) é pego; os outros continuam no lugar.
/// </summary>
public class ItemColetavel : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData item;

    [Header("Condição (opcional)")]
    [Tooltip("Se preenchido, o item só pode ser pego depois que este cofre estiver aberto")]
    [SerializeField] private ValvulaCofre cofreNecessario;
    [Tooltip("Se preenchido, o item só pode ser pego depois que este puzzle de ingredientes for concluído")]
    [SerializeField] private PuzzleIngredientes puzzleNecessario;
    [Tooltip("Alternativa ao campo acima: o 'Id Unico' de um puzzle. O item só pode ser pego depois que ele estiver concluído (funciona até se o puzzle estiver em outra cena)")]
    [SerializeField] private string idDoPuzzleNecessario = "";
    [Tooltip("Se ligado, só dá para pegar enquanto houver uma inspeção em andamento")]
    [SerializeField] private bool soDuranteAInspecao = false;

    [Header("Interação")]
    [Tooltip("Evite o E: ele fecha a inspeção")]
    [SerializeField] private Key teclaDeColetar = Key.P;
    [Tooltip("Distância máxima do jogador até o item para poder pegá-lo. Diminua nos itens que ficam perto de outros")]
    [SerializeField] private float distanciaMaxima = 4f;
    [Tooltip("De onde a distância é medida. Se vazio, usa a câmera principal")]
    [SerializeField] private Transform jogador;
    [Tooltip("Texto tipo 'P para pegar' (opcional). Aparece só quando este item é o que seria pego")]
    [SerializeField] private GameObject dicaDeInteracao;

    [Header("Diagnóstico")]
    [Tooltip("Escreve no Console o motivo quando você aperta a tecla e nada é coletado")]
    [SerializeField] private bool mostrarDiagnostico = true;

    [Header("Eventos")]
    public UnityEvent aoColetar;

    // Todos os itens ativos na cena: só o mais próximo reage à tecla
    private static readonly List<ItemColetavel> ativos = new List<ItemColetavel>();
    private static int frameDoCalculo = -1;
    private static ItemColetavel melhorDoFrame;
    private static int frameDaUltimaColeta = -1;

    private Renderer[] renderersDoItem;
    private bool coletado;

    private string ChaveDeColeta => "coletado_" + item.Id;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        ativos.Clear();
        frameDoCalculo = -1;
        melhorDoFrame = null;
        frameDaUltimaColeta = -1;
    }

    void OnEnable()
    {
        if (!ativos.Contains(this)) ativos.Add(this);
    }

    void OnDisable()
    {
        ativos.Remove(this);
        MostrarDica(false);
    }

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

        renderersDoItem = GetComponentsInChildren<Renderer>(true);
        MostrarDica(false);
    }

    void Update()
    {
        if (coletado || Keyboard.current == null) return;

        ItemColetavel melhor = ObterMelhorCandidato();
        bool souOMelhor = melhor == this;
        MostrarDica(souOMelhor);

        if (!Keyboard.current[teclaDeColetar].wasPressedThisFrame) return;

        // Outro item já foi pego neste mesmo frame
        if (Time.frameCount == frameDaUltimaColeta) return;

        if (souOMelhor)
            Coletar();
        else if (melhor == null && mostrarDiagnostico)
            Debug.Log($"[ItemColetavel:{name}] Não coletou: {MotivoDeNaoColetar()}", this);
    }

    /// <summary>Pode ser chamado também por um UnityEvent ou outro script.</summary>
    public void Coletar()
    {
        if (coletado || item == null) return;
        if (!LiberadoParaColeta(out _)) return;

        if (GerenciadorDeInventario.Instancia == null)
        {
            Debug.LogWarning("[ItemColetavel] Não achei o GerenciadorDeInventario na cena.", this);
            return;
        }

        GerenciadorDeInventario.Instancia.Adicionar(item);

        coletado = true;
        frameDaUltimaColeta = Time.frameCount;
        EstadoDoJogo.MarcarPuzzleConcluido(ChaveDeColeta);

        MostrarDica(false);
        aoColetar?.Invoke();
        gameObject.SetActive(false);
    }

    // ---------- ESCOLHA DO ITEM MAIS PRÓXIMO ----------

    private static ItemColetavel ObterMelhorCandidato()
    {
        if (frameDoCalculo == Time.frameCount) return melhorDoFrame;

        frameDoCalculo = Time.frameCount;
        melhorDoFrame = null;
        float menorDistancia = float.MaxValue;

        foreach (ItemColetavel candidato in ativos)
        {
            if (candidato == null || candidato.coletado) continue;
            if (!candidato.CondicoesAtendidas(out _)) continue;

            float distancia = candidato.DistanciaDoJogador();
            if (distancia > candidato.distanciaMaxima) continue;

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                melhorDoFrame = candidato;
            }
        }

        return melhorDoFrame;
    }

    /// <summary>O cofre foi aberto e o puzzle foi concluído? (as condições "de história" do item)</summary>
    private bool LiberadoParaColeta(out string motivo)
    {
        motivo = "";

        if (cofreNecessario != null && !cofreNecessario.Aberto)
        {
            motivo = "o ValvulaCofre indicado em 'Cofre Necessario' ainda não está Aberto (confira se é o mesmo cofre que você abriu).";
            return false;
        }

        if (puzzleNecessario != null && !puzzleNecessario.JaConcluido)
        {
            motivo = "o puzzle indicado em 'Puzzle Necessario' ainda não foi concluído.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(idDoPuzzleNecessario)
            && !EstadoDoJogo.PuzzleConcluido(idDoPuzzleNecessario.Trim()))
        {
            motivo = $"o puzzle '{idDoPuzzleNecessario.Trim()}' ainda não foi concluído.";
            return false;
        }

        return true;
    }

    private bool CondicoesAtendidas(out string motivo)
    {
        motivo = "";

        if (!LiberadoParaColeta(out motivo)) return false;

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

    private float DistanciaDoJogador()
    {
        Vector3 origem;
        if (jogador != null) origem = jogador.position;
        else if (Camera.main != null) origem = Camera.main.transform.position;
        else return float.MaxValue;

        // Distância até o ponto mais próximo do item (o pivô do prefab pode estar longe do modelo)
        if (renderersDoItem != null && renderersDoItem.Length > 0)
        {
            bool temLimites = false;
            Bounds limites = new Bounds();

            foreach (Renderer r in renderersDoItem)
            {
                if (r == null) continue;

                if (!temLimites)
                {
                    limites = r.bounds;
                    temLimites = true;
                }
                else
                {
                    limites.Encapsulate(r.bounds);
                }
            }

            if (temLimites) return Vector3.Distance(origem, limites.ClosestPoint(origem));
        }

        return Vector3.Distance(origem, transform.position);
    }

    private string MotivoDeNaoColetar()
    {
        if (!CondicoesAtendidas(out string motivo)) return motivo;

        float distancia = DistanciaDoJogador();
        if (distancia > distanciaMaxima)
            return $"o jogador está longe demais ({distancia:F1} m; máximo {distanciaMaxima:F1} m). Aumente 'Distancia Maxima' deste item.";

        return "nenhum motivo encontrado.";
    }

    private void MostrarDica(bool mostrar)
    {
        if (dicaDeInteracao != null && dicaDeInteracao.activeSelf != mostrar)
            dicaDeInteracao.SetActive(mostrar);
    }
}