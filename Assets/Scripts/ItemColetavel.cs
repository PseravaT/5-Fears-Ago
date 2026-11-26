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

    [Header("Inspeção ao coletar")]
    [Tooltip("Se ligado, ao pegar o item entra direto no modo de inspeção")]
    [SerializeField] private bool inspecionarAoColetar = true;
    [Tooltip("Modelo que aparece na inspeção. Aceita prefab. Se vazio, usa o 'Modelo 3D' do ItemData (com a rotação, posição e escala dele); se ele também estiver vazio, usa este próprio objeto")]
    [SerializeField] private GameObject modeloParaInspecao;
    [Tooltip("Usados só quando 'Modelo Para Inspecao' está preenchido")]
    [SerializeField] private Vector3 rotacaoInicialInspecao = Vector3.zero;
    [Tooltip("X = direita, Y = cima, Z = mais longe (positivo) ou mais perto (negativo)")]
    [SerializeField] private Vector3 posicaoInicialInspecao = Vector3.zero;
    [SerializeField] private Vector3 escalaInicialInspecao = Vector3.one;

    [Tooltip("Opcional: guia de UI que aparece na inspeção do item já coletado (ex.: GuiaPassarPagina). Deixe desativado na cena")]
    [SerializeField] private GameObject textoExtraNaInspecao;

    [Header("Interação")]
    [Tooltip("Tecla para coletar fora da inspeção. Evite o E: ele fecha a inspeção")]
    [SerializeField] private Key teclaDeColetar = Key.P;
    [Tooltip("Se ligado, durante a inspeção apertar E pega este item (quando ele é o mais próximo e está liberado)")]
    [SerializeField] private bool coletarComE = true;
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
    private static GameObject inspecaoDoItemColetado;

    [Header("Dot / Interagível (opcional)")]
    [Tooltip("Opcional. Se houver um Interagivel neste item (ou filho), ele só fica habilitado quando o item estiver liberado para coleta.")]
    [SerializeField] private Interagivel interagivelDoItem;

    private Renderer[] renderersDoItem;
    private bool coletado;
    private bool registreiAcaoE;

    private string ChaveDeColeta => "coletado_" + item.Id;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        ativos.Clear();
        frameDoCalculo = -1;
        melhorDoFrame = null;
        frameDaUltimaColeta = -1;
        inspecaoDoItemColetado = null;
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

        ResolverInteragivel();

        // Se já foi pego antes (ex.: o jogador voltou para esta cena ou reabriu o cofre), o objeto não reaparece
        if (EstadoDoJogo.PuzzleConcluido(ChaveDeColeta))
        {
            coletado = true;
            AtualizarInteragivel();
            gameObject.SetActive(false);
            return;
        }

        renderersDoItem = GetComponentsInChildren<Renderer>(true);
        MostrarDica(false);
        AtualizarInteragivel();
    }

    void Update()
    {
        if (coletado) return;

        AtualizarInteragivel();

        if (Keyboard.current == null) return;

        ItemColetavel melhor = ObterMelhorCandidato();
        bool souOMelhor = melhor == this;
        MostrarDica(souOMelhor);
        AtualizarAcaoDaTeclaE(souOMelhor);

        if (!Keyboard.current[teclaDeColetar].wasPressedThisFrame) return;

        // Outro item já foi pego neste mesmo frame
        if (Time.frameCount == frameDaUltimaColeta) return;

        if (souOMelhor)
            Coletar();
        else if (melhor == null && mostrarDiagnostico)
            Debug.Log($"[ItemColetavel:{name}] Não coletou: {MotivoDeNaoColetar()}", this);
    }

    // Enquanto este item for o candidato, o E da inspeção passa a coletá-lo
    private void AtualizarAcaoDaTeclaE(bool souOMelhor)
    {
        var g = GerenciadorDeInspecao.Instancia;
        if (g == null) return;

        // Na inspeção de um item recém-coletado, o E não coleta nada (só o ESC sai)
        if (g.EstaInspecionando && g.ObjetoInspecionado == inspecaoDoItemColetado) return;

        bool deveRegistrar = coletarComE && souOMelhor && g.EstaInspecionando;

        if (deveRegistrar)
        {
            g.DefinirAcaoAoPressionarE(Coletar);
            registreiAcaoE = true;
        }
        else if (registreiAcaoE)
        {
            g.LimparAcaoAoPressionarESeFor(Coletar);
            registreiAcaoE = false;
        }
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
        AtualizarInteragivel();
        frameDaUltimaColeta = Time.frameCount;
        EstadoDoJogo.MarcarPuzzleConcluido(ChaveDeColeta);

        MostrarDica(false);
        aoColetar?.Invoke();
        bool abriuInspecao = false;

        if (inspecionarAoColetar && GerenciadorDeInspecao.Instancia != null)
        {
            // Se a inspeção do cofre ainda estiver aberta (ex.: coleta pela tecla P), fecha antes
            if (GerenciadorDeInspecao.Instancia.EstaInspecionando)
                GerenciadorDeInspecao.Instancia.TerminarInspecao();

            GameObject modelo;
            Quaternion rotacao;
            Vector3 posicao;
            Vector3 escala;

            if (modeloParaInspecao != null)
            {
                // Configuração própria deste coletável
                modelo = modeloParaInspecao;
                rotacao = Quaternion.Euler(rotacaoInicialInspecao);
                posicao = posicaoInicialInspecao;
                escala = escalaInicialInspecao;
            }
            else if (item.modelo3D != null)
            {
                // Mesma configuração usada pelo inventário
                modelo = item.modelo3D;
                rotacao = Quaternion.Euler(item.usarRotacaoPropriaNaInspecao
                    ? item.rotacaoNaInspecaoGraus
                    : item.rotacaoInicialGraus);
                posicao = item.posicaoInicialInspecao;
                escala = item.escalaInspecao;
            }
            else
            {
                // Último recurso: este próprio objeto (ainda ativo neste ponto, então o clone nasce ativo)
                modelo = gameObject;
                rotacao = Quaternion.Euler(rotacaoInicialInspecao);
                posicao = posicaoInicialInspecao;
                escala = escalaInicialInspecao;
            }

            string texto = string.IsNullOrEmpty(item.textoDetalhado) ? item.descricaoCurta : item.textoDetalhado;

            GerenciadorDeInspecao.Instancia.IniciarInspecao(modelo, rotacao, texto, posicao, escala);
            abriuInspecao = true;
            inspecaoDoItemColetado = GerenciadorDeInspecao.Instancia.ObjetoInspecionado;
            GerenciadorDeInspecao.Instancia.DefinirAcaoAoPressionarE(null);
        }

        gameObject.SetActive(false);
        if (abriuInspecao) MostrarTextoExtra();
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

    private void ResolverInteragivel()
    {
        if (interagivelDoItem == null)
        {
            interagivelDoItem = GetComponent<Interagivel>();
            if (interagivelDoItem == null)
                interagivelDoItem = GetComponentInChildren<Interagivel>(true);
        }
    }

    private void AtualizarInteragivel()
    {
        if (interagivelDoItem == null) return;
        bool pode = !coletado && LiberadoParaColeta(out _);
        interagivelDoItem.SetEnabled(pode);
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

    private void MostrarTextoExtra()
    {
        if (textoExtraNaInspecao == null || GerenciadorDeInspecao.Instancia == null) return;

        textoExtraNaInspecao.SetActive(true);
        GerenciadorDeInspecao.Instancia.aoTerminarInspecao.RemoveListener(EsconderTextoExtra);
        GerenciadorDeInspecao.Instancia.aoTerminarInspecao.AddListener(EsconderTextoExtra);
    }

    private void EsconderTextoExtra()
    {
        if (textoExtraNaInspecao != null) textoExtraNaInspecao.SetActive(false);

        if (GerenciadorDeInspecao.Instancia != null)
            GerenciadorDeInspecao.Instancia.aoTerminarInspecao.RemoveListener(EsconderTextoExtra);
    }
}