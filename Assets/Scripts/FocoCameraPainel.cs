using UnityEngine;
using UnityEngine.InputSystem;

public class FocoCameraPainel : MonoBehaviour
{
    [Header("Puzzle")]
    [Tooltip("PainelRunasLivros do cofre do CENÁRIO. Guarda a sequência e dispara o aoResolver")]
    [SerializeField] private PainelRunasLivros painelDoCenario;
    [Tooltip("ValvulaCofre do cofre do CENÁRIO (opcional). Abre a porta do cenário quando destrancar")]
    [SerializeField] private ValvulaCofre valvulaDoCenario;

    [Header("Pose do modelo ao focar (relativa à câmera)")]
    [SerializeField] private float distanciaDoPainel = 0.6f;
    [SerializeField] private Vector2 deslocamentoDoPainel = Vector2.zero;
    [SerializeField] private float duracaoTransicaoSegundos = 0.6f;

    [Header("UI na inspeção")]
    [Tooltip("Botão/texto 'F: focar no painel'. Deixe desativado na cena")]
    [SerializeField] private GameObject botaoFocar;
    [Tooltip("Texto 'D: destrancar'. Aparece só fora do foco. Deixe desativado na cena")]
    [SerializeField] private GameObject dicaDestrancar;
    [SerializeField] private Key teclaFocar = Key.F;
    [SerializeField] private Key teclaDestrancar = Key.D;

    private PainelRunasLivros painelDoModelo;
    private ValvulaCofre valvulaDoModelo;
    private bool armado;
    private bool inspecaoComecou;
    private bool focado;
    private bool tentando;

    private void Start()
    {
        // Os painéis do cenário não devem reagir a teclas fora da inspeção
        if (painelDoCenario != null) painelDoCenario.SetAtivo(false);
        if (botaoFocar != null) botaoFocar.SetActive(false);
        if (dicaDestrancar != null) dicaDestrancar.SetActive(false);
    }

    // Ligue no "On Interact ()" do Interagivel, junto do MandarParaAInspecao
    public void ArmarFoco()
    {
        armado = true;
        inspecaoComecou = false;
        focado = false;
        tentando = false;
    }

    // Ligue também no OnClick do botão
    public void AlternarFoco()
    {
        var ger = GerenciadorDeInspecao.Instancia;
        if (!armado || tentando || ger == null || !ger.EstaInspecionando || ger.EmTransicao) return;

        if (painelDoModelo == null)
        {
            Debug.LogWarning("O modelo inspecionado não tem PainelRunasLivros. Ele precisa conter os tambores e o painel dentro do prefab.", this);
            return;
        }

        // Com o cofre já aberto, não há mais o que ajustar
        if (painelDoCenario != null && painelDoCenario.Resolvido) return;

        if (!focado)
        {
            ger.IrParaPose(ger.RotacaoInicial, distanciaDoPainel, deslocamentoDoPainel, duracaoTransicaoSegundos, true);
            painelDoModelo.SetAtivo(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            painelDoModelo.SetAtivo(false);
            ger.VoltarParaPoseInicial(duracaoTransicaoSegundos);
        }

        focado = !focado;
    }

    private void TentarDestrancar(GerenciadorDeInspecao ger)
    {
        if (focado || tentando || ger.EmTransicao) return;
        if (painelDoModelo == null || painelDoCenario == null || painelDoCenario.Resolvido) return;

        if (valvulaDoModelo == null)
        {
            Debug.LogWarning("O modelo inspecionado não tem ValvulaCofre.", this);
            return;
        }

        bool correta = painelDoCenario.ConferirCombinacao();

        tentando = true;
        ger.TravarControles(true);

        if (correta)
        {
            // O estado lógico muda já: se a inspeção fechar no meio da animação, o cofre fica aberto
            painelDoCenario.MarcarResolvido();
            painelDoModelo.MarcarResolvido();
            if (valvulaDoCenario != null) valvulaDoCenario.DefinirAberto();
        }

        valvulaDoModelo.AoTerminarAnimacao += AoTerminarTentativa;
        valvulaDoModelo.Tentar(correta);
    }

    private void AoTerminarTentativa()
    {
        if (valvulaDoModelo != null) valvulaDoModelo.AoTerminarAnimacao -= AoTerminarTentativa;
        tentando = false;

        var ger = GerenciadorDeInspecao.Instancia;
        if (ger != null && ger.EstaInspecionando) ger.TravarControles(false);
    }

    private void PrepararModelo(GerenciadorDeInspecao ger)
    {
        if (ger.ObjetoInspecionado == null) return;

        valvulaDoModelo = ger.ObjetoInspecionado.GetComponentInChildren<ValvulaCofre>(true);
        painelDoModelo = ger.ObjetoInspecionado.GetComponentInChildren<PainelRunasLivros>(true);
        if (painelDoModelo == null) return;

        painelDoModelo.SetAtivo(false);

        // A cópia já nasce mostrando a sequência atual do cenário
        if (painelDoCenario != null)
        {
            painelDoModelo.AplicarEstado(painelDoCenario.ObterEstado());

            // Se o cofre já foi aberto, a cópia também nasce aberta e com os tambores travados
            if (painelDoCenario.Resolvido)
            {
                painelDoModelo.MarcarResolvido();
                if (valvulaDoModelo != null) valvulaDoModelo.DefinirAberto();
            }
        }

        painelDoModelo.AoMudarEstado += SincronizarComCenario;
    }

    // Cada giro na cópia é guardado no painel do cenário (só os números, sem girar os tambores dele)
    private void SincronizarComCenario()
    {
        if (painelDoCenario != null && painelDoModelo != null)
        {
            painelDoCenario.AplicarEstado(painelDoModelo.ObterEstado(), false);
        }
    }

    private void Desarmar()
    {
        if (painelDoModelo != null) painelDoModelo.AoMudarEstado -= SincronizarComCenario;
        if (valvulaDoModelo != null) valvulaDoModelo.AoTerminarAnimacao -= AoTerminarTentativa;

        painelDoModelo = null;
        valvulaDoModelo = null;
        armado = false;
        inspecaoComecou = false;
        focado = false;
        tentando = false;

        if (botaoFocar != null) botaoFocar.SetActive(false);
        if (dicaDestrancar != null) dicaDestrancar.SetActive(false);
    }

    private void Update()
    {
        if (!armado) return;

        var ger = GerenciadorDeInspecao.Instancia;
        bool inspecionando = ger != null && ger.EstaInspecionando;

        if (inspecionando)
        {
            if (!inspecaoComecou)
            {
                inspecaoComecou = true;
                PrepararModelo(ger);
            }

            bool resolvido = painelDoCenario != null && painelDoCenario.Resolvido;
            bool podeDestrancar = !focado && !tentando && !resolvido;

            if (botaoFocar != null) botaoFocar.SetActive(!resolvido && !tentando);
            if (dicaDestrancar != null) dicaDestrancar.SetActive(podeDestrancar);

            var teclado = Keyboard.current;
            if (teclado == null) return;

            if (teclado[teclaFocar].wasPressedThisFrame) AlternarFoco();
            else if (teclado[teclaDestrancar].wasPressedThisFrame) TentarDestrancar(ger);
        }
        else if (inspecaoComecou)
        {
            // A inspeção terminou (Esc ou E)
            Desarmar();
        }
    }
}