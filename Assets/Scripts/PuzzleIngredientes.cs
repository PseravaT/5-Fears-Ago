using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PuzzleIngredientes : MonoBehaviour
{
    public enum TipoOpcao
    {
        Ingrediente1,
        Ingrediente2,
        Ingrediente3,
        Ingrediente4,
        Ingrediente5,
        Sair
    }

    [System.Serializable]
    public class SlotOpcaoUI
    {
        public string nomeExibicao;
        public TipoOpcao tipo;
        public Image fundoOuBorda;
        public TextMeshProUGUI texto;
    }

    [Header("Painel Principal do Puzzle")]
    [SerializeField] private GameObject painelPuzzle;
    [SerializeField] private TextMeshProUGUI textoInstrucao;

    [Header("Configuração da Grelha (6 Itens: 5 Ingredientes + 1 Sair)")]
    [Tooltip("Ordem dos slots no grid visual: 2 colunas x 3 linhas ou 3 colunas x 2 linhas")]
    [SerializeField] private int colunasGrelha = 2; // Exemplo: 2 colunas x 3 linhas
    [SerializeField] private List<SlotOpcaoUI> slots = new List<SlotOpcaoUI>();

    [Header("Cores de Destaque")]
    [SerializeField] private Color corTextoNormal = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color corTextoSelecionado = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color corFundoNormal = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    [SerializeField] private Color corFundoSelecionado = new Color(0.35f, 0.35f, 0.1f, 0.9f);

    [Header("Persistência")]
    [SerializeField] private string idUnico = "caldeirao"; // único para cada puzzle

    [Header("Sequência Correta dos 5 Ingredientes")]
    [SerializeField] private TipoOpcao[] sequenciaCorreta = new TipoOpcao[]
    {
        TipoOpcao.Ingrediente1,
        TipoOpcao.Ingrediente2,
        TipoOpcao.Ingrediente3,
        TipoOpcao.Ingrediente4,
        TipoOpcao.Ingrediente5
    };

    [Header("Mensagens de Texto no Topo")]
    [TextArea(2, 3)]
    [SerializeField] private string mensagemInicial = "Qual o primeiro ingrediente que devo colocar?";
    [TextArea(2, 3)]
    [SerializeField] private string[] mensagensAcerto = new string[]
    {
        "Certo, o primeiro já foi! Qual devo colocar depois?",
        "Muito bem, segundo ingrediente adicionado. O que vem a seguir?",
        "Mais um! Estamos quase lá... qual é o próximo?",
        "Quase pronto! Falta apenas o último ingrediente..."
    };
    [TextArea(2, 3)]
    [SerializeField] private string mensagemErro = "Droga... ingrediente errado! Vamos voltar ao início... qual deve ser a ordem?";
    [TextArea(2, 3)]
    [SerializeField] private string mensagemConclusao = "Perfeito! A mistura está pronta!";

    [Header("Efeitos Sonoros Opcionais")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip somNavegacao;
    [SerializeField] private AudioClip somErro;
    [SerializeField] private AudioClip somConclusao;

    [Header("Sons de Acerto (vários, sorteados a cada acerto)")]
    [Tooltip("Arraste aqui os clipes (ex.: splash_01 ... splash_10). Um é sorteado a cada acerto, sem repetir o anterior.")]
    [SerializeField] private AudioClip[] sonsAcerto;
    [Tooltip("AudioSource exclusivo dos acertos (opcional). Se vazio, um é criado automaticamente. É separado para o pitch não afetar os outros sons.")]
    [SerializeField] private AudioSource audioSourceAcerto;
    [Tooltip("Pitch sorteado entre mínimo e máximo. Abaixo de 1 = mais grave.")]
    [SerializeField, Range(0.1f, 1.5f)] private float pitchAcertoMin = 0.70f;
    [SerializeField, Range(0.1f, 1.5f)] private float pitchAcertoMax = 0.85f;
    [Tooltip("Corta o som depois de X segundos (0 = toca o clipe inteiro).")]
    [SerializeField, Min(0f)] private float duracaoMaximaAcerto = 0.6f;
    [Tooltip("Tempo do fade-out no final do corte, para não estalar.")]
    [SerializeField, Min(0f)] private float fadeOutAcerto = 0.08f;
    [SerializeField, Range(0f, 1f)] private float volumeAcerto = 1f;

    [Header("Eventos")]
    public UnityEvent aoIniciarPuzzle;
    public UnityEvent aoCancelarPuzzle;
    public UnityEvent aoConcluirPuzzle;

    // Estado interno
    private int indiceSelecionado = 0;
    private int passoAtualSequencia = 0;
    private bool puzzleAtivo = false;
    private int frameAtivacao = -1;
    private int ultimoFrameFechado = -1;
    private bool puzzleConcluido = false;
    private int ultimoSomAcertoIndice = -1;
    private Coroutine rotinaCorteAcerto;
    public bool JaConcluido => puzzleConcluido;

    public static PuzzleIngredientes Instancia;

    public bool EstaAtivo => puzzleAtivo;
    public bool FechouNesteFrame => Time.frameCount == ultimoFrameFechado;

    void Awake()
    {
        if (EstadoDoJogo.PuzzleConcluido(idUnico))
        {
            puzzleConcluido = true;
            if (TryGetComponent<Interagivel>(out var interagivel))
                interagivel.SetEnabled(false);
        }
        Instancia = this;

        PrepararAudioSourceAcerto();

        if (painelPuzzle != null)
            painelPuzzle.SetActive(false);
    }

    /// <summary>
    /// Garante um AudioSource próprio para os acertos (o pitch é do AudioSource inteiro,
    /// então não pode ser o mesmo usado pelos outros sons).
    /// </summary>
    private void PrepararAudioSourceAcerto()
    {
        if (audioSourceAcerto == null)
        {
            audioSourceAcerto = gameObject.AddComponent<AudioSource>();
            audioSourceAcerto.playOnAwake = false;
            audioSourceAcerto.loop = false;

            // Herda as configurações do AudioSource principal, se existir
            if (audioSource != null)
            {
                audioSourceAcerto.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                audioSourceAcerto.spatialBlend = audioSource.spatialBlend;
                audioSourceAcerto.minDistance = audioSource.minDistance;
                audioSourceAcerto.maxDistance = audioSource.maxDistance;
            }
        }
    }

    /// <summary>
    /// Chame este método no AoEntrarNaVisao (junto com o MostrarTexto do diálogo).
    /// Ele espera APENAS este diálogo terminar para abrir o puzzle, e depois se desconecta
    /// para que outros diálogos da cena não abram o puzzle!
    /// </summary>
    public void EsperarDialogoEIniciarPuzzle()
    {
        if (puzzleConcluido) return;
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AoTerminarDialogoDoCaldeirao);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(AoTerminarDialogoDoCaldeirao);
        }
        else
        {
            if (puzzleConcluido) return;
            IniciarPuzzle();
        }
    }

    private void AoTerminarDialogoDoCaldeirao()
    {
        CancelarEsperaDeDialogo();
        IniciarPuzzle();
    }

    public void CancelarEsperaDeDialogo()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AoTerminarDialogoDoCaldeirao);
        }
    }

    /// <summary>
    /// Inicia o puzzle imediatamente.
    /// </summary>
    public void IniciarPuzzle()
    {
        // Se ainda houver algum balão de diálogo aberto, fecha para não sobrepor
        if (GerenciadorDeDialogo.Instancia != null && GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
        {
            GerenciadorDeDialogo.Instancia.EsconderTexto();
        }

        if (painelPuzzle != null)
            painelPuzzle.SetActive(true);

        puzzleAtivo = true;
        frameAtivacao = Time.frameCount;
        passoAtualSequencia = 0;
        indiceSelecionado = 0;

        AtualizarNomesNosSlots();
        AtualizarMensagemTopo(mensagemInicial);
        AtualizarSelecaoVisual();

        aoIniciarPuzzle?.Invoke();
    }

    private void AtualizarNomesNosSlots()
    {
        if (slots == null) return;
        foreach (var s in slots)
        {
            if (s != null && s.texto != null && !string.IsNullOrEmpty(s.nomeExibicao))
            {
                s.texto.text = s.nomeExibicao;
            }
        }
    }

    void Update()
    {
        if (!puzzleAtivo) return;

        // Evita ler o input no mesmo frame exato em que o puzzle abriu com a tecla E
        if (Time.frameCount <= frameAtivacao) return;

        ProcessarNavegacao();
        ProcessarConfirmacaoECancelamento();
    }

    private void ProcessarNavegacao()
    {
        if (slots == null || slots.Count == 0) return;

        int total = slots.Count;
        int linhas = Mathf.CeilToInt((float)total / Mathf.Max(1, colunasGrelha));
        int colunaAtual = indiceSelecionado % colunasGrelha;
        int linhaAtual = indiceSelecionado / colunasGrelha;

        bool moveu = false;
        int novoIndice = indiceSelecionado;

        // Cima / Baixo / Esquerda / Direita via Teclado (WASD / Setas)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                linhaAtual = (linhaAtual - 1 + linhas) % linhas;
                moveu = true;
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                linhaAtual = (linhaAtual + 1) % linhas;
                moveu = true;
            }
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                colunaAtual = (colunaAtual - 1 + colunasGrelha) % colunasGrelha;
                moveu = true;
            }
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                colunaAtual = (colunaAtual + 1) % colunasGrelha;
                moveu = true;
            }
        }

        if (moveu)
        {
            novoIndice = (linhaAtual * colunasGrelha) + colunaAtual;
            if (novoIndice >= total)
                novoIndice = total - 1;

            if (novoIndice != indiceSelecionado)
            {
                indiceSelecionado = novoIndice;
                TocarSom(somNavegacao);
                AtualizarSelecaoVisual();
            }
        }
    }

    private void ProcessarConfirmacaoECancelamento()
    {
        if (Keyboard.current == null) return;

        // Tecla ESC: Cancelar e sair a qualquer momento
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            FecharPuzzle(cancelado: true);
            return;
        }

        // Tecla E: Confirmar opção destacada
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ConfirmarOpcaoSelecionada();
        }
    }

    private void ConfirmarOpcaoSelecionada()
    {
        if (indiceSelecionado < 0 || indiceSelecionado >= slots.Count) return;

        SlotOpcaoUI slot = slots[indiceSelecionado];

        // Opção "Sair"
        if (slot.tipo == TipoOpcao.Sair)
        {
            FecharPuzzle(cancelado: true);
            return;
        }

        // Verifica acerto ou erro com base na sequência esperada
        if (passoAtualSequencia < sequenciaCorreta.Length && slot.tipo == sequenciaCorreta[passoAtualSequencia])
        {
            // --- ACERTOU O PASSO ---
            passoAtualSequencia++;

            if (passoAtualSequencia >= sequenciaCorreta.Length)
            {
                // Concluiu todos os ingredientes!
                TocarSom(somConclusao);
                AtualizarMensagemTopo(mensagemConclusao);
                StartCoroutine(RotinaConclusao());
            }
            else
            {
                TocarSomAcerto();
                string textoFeedback = (passoAtualSequencia - 1 < mensagensAcerto.Length)
                    ? mensagensAcerto[passoAtualSequencia - 1]
                    : "Correto! Qual o próximo ingrediente?";
                AtualizarMensagemTopo(textoFeedback);
            }
        }
        else
        {
            // --- ERROU A SEQUÊNCIA ---
            passoAtualSequencia = 0; // Volta à estaca zero
            TocarSom(somErro);
            AtualizarMensagemTopo(mensagemErro);

            // Penalização: aciona dano / estágio no sistema de vida global
            if (SistemaDeVida.Instancia != null)
            {
                SistemaDeVida.Instancia.RegistrarErroPuzzle();
            }
            else
            {
                SistemaDeVida.RegistrarErroGlobal();
            }
        }
    }

    private IEnumerator RotinaConclusao()
    {
        yield return new WaitForSeconds(1.2f);

        puzzleConcluido = true;
        EstadoDoJogo.MarcarPuzzleConcluido(idUnico);

        if (TryGetComponent<Interagivel>(out var interagivel))
            interagivel.SetEnabled(false);

        FecharPuzzle(cancelado: false);
        aoConcluirPuzzle?.Invoke();
    }

    public void FecharPuzzle(bool cancelado)
    {
        puzzleAtivo = false;
        ultimoFrameFechado = Time.frameCount;

        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AoTerminarDialogoDoCaldeirao);
        }

        if (painelPuzzle != null)
            painelPuzzle.SetActive(false);

        // Se este objeto também tiver ControleCameraInteracao, sai da visão da câmera ao fechar/concluir o puzzle
        if (TryGetComponent<ControleCameraInteracao>(out var cameraInteracao))
        {
            cameraInteracao.SairDaVisao();
        }

        if (cancelado)
        {
            aoCancelarPuzzle?.Invoke();
        }
    }

    private void AtualizarMensagemTopo(string mensagem)
    {
        if (textoInstrucao != null)
            textoInstrucao.text = mensagem;
    }

    private void AtualizarSelecaoVisual()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            bool estaSelecionado = (i == indiceSelecionado);
            SlotOpcaoUI s = slots[i];

            if (s.texto != null)
            {
                s.texto.color = estaSelecionado ? corTextoSelecionado : corTextoNormal;
            }

            if (s.fundoOuBorda != null)
            {
                s.fundoOuBorda.color = estaSelecionado ? corFundoSelecionado : corFundoNormal;
            }
        }
    }

    private void TocarSom(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ------------------------------------------------------------------
    //  SOM DE ACERTO: sorteia um clipe, abaixa o pitch e corta o final
    // ------------------------------------------------------------------
    private void TocarSomAcerto()
    {
        if (sonsAcerto == null || sonsAcerto.Length == 0) return;

        PrepararAudioSourceAcerto();

        // Sorteia um clipe diferente do último (quando há mais de um)
        // Obs.: UnityEngine.Random explícito, pois "using System;" deixa "Random" ambíguo.
        int indice = UnityEngine.Random.Range(0, sonsAcerto.Length);
        if (sonsAcerto.Length > 1)
        {
            int tentativas = 0;
            while ((indice == ultimoSomAcertoIndice || sonsAcerto[indice] == null) && tentativas < 10)
            {
                indice = UnityEngine.Random.Range(0, sonsAcerto.Length);
                tentativas++;
            }
        }

        AudioClip clip = sonsAcerto[indice];
        if (clip == null) return;
        ultimoSomAcertoIndice = indice;

        // Interrompe o corte do acerto anterior, se ainda estiver rodando
        if (rotinaCorteAcerto != null)
        {
            StopCoroutine(rotinaCorteAcerto);
            rotinaCorteAcerto = null;
        }

        float pitchMin = Mathf.Min(pitchAcertoMin, pitchAcertoMax);
        float pitchMax = Mathf.Max(pitchAcertoMin, pitchAcertoMax);

        audioSourceAcerto.Stop();
        audioSourceAcerto.clip = clip;
        audioSourceAcerto.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        audioSourceAcerto.volume = volumeAcerto;
        audioSourceAcerto.Play();

        if (duracaoMaximaAcerto > 0f)
            rotinaCorteAcerto = StartCoroutine(RotinaCortarAcerto());
    }

    private IEnumerator RotinaCortarAcerto()
    {
        // Tempo de espera até começar o fade-out
        float esperar = Mathf.Max(0f, duracaoMaximaAcerto - fadeOutAcerto);
        yield return new WaitForSecondsRealtime(esperar);

        // Fade-out rápido para o corte não estalar
        if (fadeOutAcerto > 0f)
        {
            float t = 0f;
            float volumeInicial = audioSourceAcerto.volume;
            while (t < fadeOutAcerto)
            {
                t += Time.unscaledDeltaTime;
                audioSourceAcerto.volume = Mathf.Lerp(volumeInicial, 0f, t / fadeOutAcerto);
                yield return null;
            }
        }

        audioSourceAcerto.Stop();
        audioSourceAcerto.volume = volumeAcerto;
        rotinaCorteAcerto = null;
    }
}