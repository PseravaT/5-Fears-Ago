using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GerenciadorDeDialogo : MonoBehaviour
{
    public static GerenciadorDeDialogo Instancia;

    [Header("Elementos da Interface")]
    [SerializeField] private GameObject painelBalao;
    [SerializeField] private TextMeshProUGUI componenteDeTexto;
    [Tooltip("Objeto do ícone do triângulo que indica que o jogador pode avançar o diálogo")]
    [SerializeField] private GameObject iconeContinuar;

    [Header("Configurações de Tempo")]
    [SerializeField] private float tempoParaAparecer = 1.5f; // Tempo que o balão espera antes de surgir
    [SerializeField] private float delayEntreLetras = 0.05f; // Velocidade da máquina de escrever

    [Header("Animação do Ícone (Triângulo)")]
    [SerializeField] private float velocidadeFlutuacao = 6f;
    [SerializeField] private float amplitudeFlutuacao = 5f;

    [Header("Input (Opcional - Novo Input System)")]
    [Tooltip("Opcional: Ação de interação do Novo Input System. A tecla 'E' já funciona automaticamente.")]
    [SerializeField] private InputActionReference acaoInteragir;

    [Header("Eventos")]
    public UnityEvent aoFecharDialogo;

    private Coroutine corrotinaAtual;
    private readonly List<string> filaDeFalas = new List<string>();
    private int indiceFalaAtual = 0;
    private bool aguardandoInput = false;
    private bool dialogoAtivo = false;
    private int ultimoFrameFechado = -1;
    private Vector3 posicaoInicialIcone;
    private bool posicaoIconeSalva = false;

    public bool DialogoEstaAtivo => dialogoAtivo;
    public bool FechouNesteFrame => Time.frameCount == ultimoFrameFechado;

    void Awake()
    {
        if (Instancia == null) Instancia = this;

        SalvarPosicaoInicialIcone();
    }

    void Start()
    {
        // Garante que tudo começa escondido sem disparar eventos de fim de diálogo no início da cena
        if (painelBalao != null) painelBalao.SetActive(false);
        if (componenteDeTexto != null) componenteDeTexto.text = "";
        DefinirIconeVisivel(false);
    }

    private void SalvarPosicaoInicialIcone()
    {
        if (iconeContinuar != null && !posicaoIconeSalva)
        {
            posicaoInicialIcone = iconeContinuar.transform.localPosition;
            posicaoIconeSalva = true;
        }
    }

    private void DefinirIconeVisivel(bool visivel)
    {
        if (iconeContinuar == null) return;

        SalvarPosicaoInicialIcone();
        iconeContinuar.transform.localPosition = posicaoInicialIcone;
        iconeContinuar.SetActive(visivel);
    }

    void Update()
    {
        // 1. Animação matemática de flutuação do ícone do triângulo enquanto estiver visível
        if (iconeContinuar != null && iconeContinuar.activeSelf)
        {
            float deslocamentoY = Mathf.Sin(Time.time * velocidadeFlutuacao) * amplitudeFlutuacao;
            iconeContinuar.transform.localPosition = posicaoInicialIcone + new Vector3(0f, deslocamentoY, 0f);
        }

        // 2. Avanço por Input quando a frase atual terminou de ser digitada
        if (dialogoAtivo && aguardandoInput && JogadorPressionouAvancar())
        {
            AvancarParaProximaFala();
        }
    }

    private bool JogadorPressionouAvancar()
    {
        // Tecla 'E' via Novo Input System
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;

        // Clique esquerdo do mouse também permite avançar/fechar o diálogo
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        // Ação de interação opcional configurada do Novo Input System
        if (acaoInteragir != null && acaoInteragir.action != null && acaoInteragir.action.WasPressedThisFrame())
            return true;

        return false;
    }

    /// <summary>
    /// Recebe um array de strings para exibir múltiplas falas em sequência.
    /// </summary>
    public void MostrarTexto(string[] mensagens)
    {
        if (mensagens == null || mensagens.Length == 0) return;

        IniciarSequenciaDeFalas(mensagens);
    }

    /// <summary>
    /// Recebe uma lista de strings para exibir múltiplas falas em sequência.
    /// </summary>
    public void MostrarTexto(List<string> mensagens)
    {
        if (mensagens == null || mensagens.Count == 0) return;

        IniciarSequenciaDeFalas(mensagens);
    }

    /// <summary>
    /// Sobrecarga para uma única string (mantém compatibilidade com o Inspector / scripts existentes).
    /// Dica: se usar '|' no texto do Inspector, ele divide automaticamente em várias falas!
    /// </summary>
    public void MostrarTexto(string mensagem)
    {
        if (string.IsNullOrEmpty(mensagem)) return;

        string[] partes = mensagem.Contains("|")
            ? mensagem.Split('|')
            : new string[] { mensagem };

        IniciarSequenciaDeFalas(partes);
    }

    private void IniciarSequenciaDeFalas(IEnumerable<string> mensagens)
    {
        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
        }

        filaDeFalas.Clear();
        foreach (string msg in mensagens)
        {
            if (!string.IsNullOrEmpty(msg))
                filaDeFalas.Add(msg.Trim());
        }

        if (filaDeFalas.Count == 0) return;

        indiceFalaAtual = 0;
        aguardandoInput = false;
        dialogoAtivo = true;

        // Inicia a primeira fala respeitando o tempoParaAparecer (ex: viagem da câmera Cinemachine)
        corrotinaAtual = StartCoroutine(RotinaMostrarFala(filaDeFalas[indiceFalaAtual], usarDelayInicial: true));
    }

    private void AvancarParaProximaFala()
    {
        aguardandoInput = false;
        DefinirIconeVisivel(false);

        indiceFalaAtual++;

        // Se ainda houver mais frases na lista, limpa a caixa e começa a digitar a próxima
        if (indiceFalaAtual < filaDeFalas.Count)
        {
            if (corrotinaAtual != null)
            {
                StopCoroutine(corrotinaAtual);
            }

            corrotinaAtual = StartCoroutine(RotinaMostrarFala(filaDeFalas[indiceFalaAtual], usarDelayInicial: false));
        }
        // Se era a última frase da lista, fecha a caixa de vez e dispara o aoFecharDialogo
        else
        {
            EsconderTexto();
        }
    }

    private IEnumerator RotinaMostrarFala(string mensagem, bool usarDelayInicial)
    {
        aguardandoInput = false;
        DefinirIconeVisivel(false);

        if (usarDelayInicial)
        {
            // 1. Garante que a caixa preta está invisível enquanto a câmera viaja
            if (painelBalao != null) painelBalao.SetActive(false);
            if (componenteDeTexto != null) componenteDeTexto.text = "";

            // 2. Espera o tempo configurado (ex: o tempo da transição do Cinemachine)
            if (tempoParaAparecer > 0f)
                yield return new WaitForSeconds(tempoParaAparecer);
        }

        // 3. Ativa a caixa preta e limpa o texto anterior
        if (painelBalao != null) painelBalao.SetActive(true);
        if (componenteDeTexto != null) componenteDeTexto.text = "";

        // 4. Efeito de digitação letra a letra (máquina de escrever)
        if (componenteDeTexto != null)
        {
            foreach (char letra in mensagem.ToCharArray())
            {
                componenteDeTexto.text += letra;
                yield return new WaitForSeconds(delayEntreLetras);
            }
        }

        // Aguarda 1 frame antes de liberar o input para evitar que o próprio clique/tecla que abriu o diálogo já o feche
        yield return null;

        // 5. Frase concluída: exibe o triângulo e aguarda o jogador pressionar 'E' ou clicar
        DefinirIconeVisivel(true);
        aguardandoInput = true;
        corrotinaAtual = null;
    }

    public void EsconderTexto()
    {
        // Para qualquer texto que esteja a ser escrito e esconde o balão e o ícone instantaneamente
        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
            corrotinaAtual = null;
        }

        aguardandoInput = false;
        dialogoAtivo = false;
        ultimoFrameFechado = Time.frameCount;
        filaDeFalas.Clear();

        DefinirIconeVisivel(false);
        if (painelBalao != null) painelBalao.SetActive(false);
        if (componenteDeTexto != null) componenteDeTexto.text = "";

        aoFecharDialogo?.Invoke();
    }
}

