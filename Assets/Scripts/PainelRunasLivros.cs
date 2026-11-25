using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PainelRunasLivros : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask camadaTambores = ~0;

    [Header("Tambores (esquerda -> direita)")]
    [SerializeField] private TamborPuzzle[] simbolos = new TamborPuzzle[5]; // fileira de cima
    [SerializeField] private TamborPuzzle[] cores = new TamborPuzzle[5];    // fileira de baixo

    [Header("Resposta (0 = branco)")]
    // Símbolos: 1=X, 2=Y, 3=Z, 4=A, 5=B
    [SerializeField] private int[] respostaSimbolos = { 1, 2, 3, 4, 5 };
    // Cores: 1=Verde, 2=Vermelho, 3=Azul, 4=Roxo, 5=Amarelo
    [SerializeField] private int[] respostaCores = { 1, 2, 3, 4, 5 };

    [Header("Controle")]
    [Tooltip("Permite também girar os tambores clicando (esquerdo avança, direito volta)")]
    [SerializeField] private bool usarMouse = false;
    [Tooltip("Inverte o sentido do giro de W/S")]
    [SerializeField] private bool inverterSentido = false;

    [Header("Marcador de seleção")]
    [SerializeField] private Transform marcador;
    [SerializeField] private Renderer marcadorRenderer;
    [SerializeField] private Material materialSelecionado;
    [SerializeField] private Material materialTravado;
    [Tooltip("Deslocamento do marcador em relação ao centro do tambor, no espaço do pai do marcador")]
    [SerializeField] private Vector3 deslocamentoDoMarcador = Vector3.zero;

    [SerializeField] private bool ativo = true;

    [Header("Persistência")]
    [Tooltip("ID salvo no EstadoDoJogo ao resolver este puzzle (use o mesmo ID no campo 'Id Puzzle Que Destranca' da porta).")]
    [SerializeField] private string idParaSalvar = "escritorio";

    [Tooltip("Disparado quando o cofre é destrancado com a combinação certa")]
    public UnityEvent aoResolver;

    /// <summary>Disparado quando o jogador começa a girar um tambor (usado para sincronizar o cenário).</summary>
    public event Action AoMudarEstado;

    public bool Resolvido => resolvido;

    private bool resolvido;
    private int linha;   // 0 = símbolos, 1 = cores
    private int coluna;
    private bool travado;

    private void Start()
    {
        if (cam == null) cam = Camera.main;

        // Se o puzzle já foi resolvido antes (ex.: o jogador saiu da cena e voltou), já nasce resolvido.
        // Não dispara o aoResolver: o que depende dele guarda o próprio estado.
        if (!string.IsNullOrWhiteSpace(idParaSalvar) && EstadoDoJogo.PuzzleConcluido(idParaSalvar.Trim()))
        {
            resolvido = true;
            travado = false;
        }

        foreach (var t in simbolos) t.AoIniciarGiro += AoIniciarGiroTambor;
        foreach (var t in cores) t.AoIniciarGiro += AoIniciarGiroTambor;
        AtualizarMarcador();
    }

    public void SetAtivo(bool valor)
    {
        ativo = valor;
        if (!ativo) travado = false;   // a seleção continua guardada, só solta o tambor
        AtualizarMarcador();
    }

    /// <summary>10 valores: 5 símbolos seguidos de 5 cores.</summary>
    public int[] ObterEstado()
    {
        int n = simbolos.Length;
        var estado = new int[n + cores.Length];
        for (int i = 0; i < n; i++) estado[i] = simbolos[i].IndiceAtual;
        for (int i = 0; i < cores.Length; i++) estado[n + i] = cores[i].IndiceAtual;
        return estado;
    }

    /// <summary>Aplica o estado sem animação e sem som. Não dispara AoMudarEstado e não confere a resposta.</summary>
    public void AplicarEstado(int[] estado, bool atualizarVisual = true)
    {
        if (estado == null || estado.Length != simbolos.Length + cores.Length) return;

        int n = simbolos.Length;
        for (int i = 0; i < n; i++) simbolos[i].DefinirIndiceImediato(estado[i], atualizarVisual);
        for (int i = 0; i < cores.Length; i++) cores[i].DefinirIndiceImediato(estado[n + i], atualizarVisual);
    }

    /// <summary>Só compara com a resposta. Nada acontece sozinho: quem chama decide o que fazer.</summary>
    public bool ConferirCombinacao()
    {
        for (int i = 0; i < simbolos.Length; i++)
        {
            if (simbolos[i].IndiceAtual != respostaSimbolos[i]) return false;
            if (cores[i].IndiceAtual != respostaCores[i]) return false;
        }
        return true;
    }

    /// <summary>Trava os tambores e dispara aoResolver.</summary>
    public void MarcarResolvido()
    {
        if (resolvido) return;

        resolvido = true;
        travado = false;

        if (!string.IsNullOrWhiteSpace(idParaSalvar))
        {
            EstadoDoJogo.MarcarPuzzleConcluido(idParaSalvar.Trim());
        }

        AtualizarMarcador();
        aoResolver.Invoke();
    }

    private TamborPuzzle TamborSelecionado()
    {
        return linha == 0 ? simbolos[coluna] : cores[coluna];
    }

    private void Update()
    {
        if (!ativo || resolvido) return;

        var teclado = Keyboard.current;
        if (teclado != null) ControlarPorTeclado(teclado);

        if (usarMouse) ControlarPorMouse();

        AtualizarMarcador();
    }

    private void ControlarPorTeclado(Keyboard teclado)
    {
        if (teclado.eKey.wasPressedThisFrame)
        {
            travado = !travado;
        }

        if (!travado)
        {
            if (teclado.aKey.wasPressedThisFrame) coluna = Mathf.Max(0, coluna - 1);
            if (teclado.dKey.wasPressedThisFrame) coluna = Mathf.Min(simbolos.Length - 1, coluna + 1);
            if (teclado.wKey.wasPressedThisFrame) linha = 0;
            if (teclado.sKey.wasPressedThisFrame) linha = 1;
        }
        else
        {
            // isPressed: segurar a tecla continua girando (Girar ignora a chamada enquanto o tambor gira)
            TamborPuzzle tambor = TamborSelecionado();
            if (teclado.wKey.isPressed) tambor.Girar(inverterSentido ? -1 : 1);
            else if (teclado.sKey.isPressed) tambor.Girar(inverterSentido ? 1 : -1);
        }
    }

    private void ControlarPorMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        int passo = 0;
        if (mouse.leftButton.wasPressedThisFrame) passo = 1;
        else if (mouse.rightButton.wasPressedThisFrame) passo = -1;
        if (passo == 0) return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, camadaTambores))
        {
            var tambor = hit.collider.GetComponentInParent<TamborPuzzle>();
            if (tambor != null) tambor.Girar(passo);
        }
    }

    private void AtualizarMarcador()
    {
        if (marcador == null) return;

        bool mostrar = ativo && !resolvido;
        if (marcador.gameObject.activeSelf != mostrar) marcador.gameObject.SetActive(mostrar);
        if (!mostrar) return;

        Transform alvo = TamborSelecionado().transform;
        Vector3 pos = marcador.parent != null ? marcador.parent.InverseTransformPoint(alvo.position) : alvo.position;
        marcador.localPosition = pos + deslocamentoDoMarcador;

        if (marcadorRenderer != null)
        {
            Material m = travado ? materialTravado : materialSelecionado;
            if (m != null && marcadorRenderer.sharedMaterial != m) marcadorRenderer.sharedMaterial = m;
        }
    }

    private void AoIniciarGiroTambor(TamborPuzzle _)
    {
        AoMudarEstado?.Invoke();   // sincroniza com o cenário no começo do giro
    }
}