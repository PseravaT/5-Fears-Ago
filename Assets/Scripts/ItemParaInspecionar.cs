using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ItemParaInspecionar : MonoBehaviour
{
    [Header("Modelo 3D")]
    [Tooltip("Arraste o modelo 3D (ou Prefab) que vai aparecer flutuando na tela")]
    [FormerlySerializedAs("modelo3DParaInspecao")]
    public GameObject modeloDoObjeto;

    [Tooltip("Rotação inicial relativa à câmera (ex: 90, 0, 0 para levantar uma folha deitada)")]
    public Vector3 rotacaoInicial = Vector3.zero;

    [Tooltip("Deslocamento inicial em relação ao ponto padrão, relativo à câmera: X = direita, Y = cima, Z = mais longe (positivo) ou mais perto (negativo)")]
    public Vector3 posicaoInicial = Vector3.zero;

    [Tooltip("Escala do modelo na inspeção (1,1,1 = tamanho original do prefab)")]
    public Vector3 escalaInicial = Vector3.one;

    [Header("Tecla Q na inspeção")]
    [Tooltip("LerDetalhes: abre o texto da história. Focar: alterna entre focar e desfocar (use os eventos abaixo). Nenhum: a Q não faz nada")]
    public ModoTeclaQ funcaoDaTeclaQ = ModoTeclaQ.LerDetalhes;
    [Tooltip("Só no modo Focar: chamado quando o jogador aperta Q para focar")]
    public UnityEvent aoFocar;
    [Tooltip("Só no modo Focar: chamado quando o jogador aperta Q para sair do foco")]
    public UnityEvent aoDesfocar;

    [Header("Texto extra na inspeção (Opcional)")]
    [Tooltip("Objeto de UI que só aparece enquanto ESTE item é inspecionado. Deixe desativado na cena")]
    public GameObject textoExtraNaInspecao;

    [Header("História do Objeto (Tecla Q no modo LerDetalhes)")]
    [Tooltip("Texto customizado que aparece ao apertar Q durante a inspeção (Estágio 0 / Normal)")]
    [TextArea(5, 10)]
    public string textoDetalhado;

    [Tooltip("Opcional: Texto da tecla Q quando estiver no 1º nível de dano (Laranja)")]
    [TextArea(3, 6)]
    public string textoDetalhadoDano1Laranja;

    [Tooltip("Opcional: Texto da tecla Q quando estiver no 2º nível de dano (Vermelho)")]
    [TextArea(3, 6)]
    public string textoDetalhadoDano2Vermelho;

    [Header("Diálogo Prévio (Opcional)")]
    [Tooltip("Se preenchido, exibe este monólogo/diálogo antes de abrir o 3D (Estágio 0 / Normal)")]
    [TextArea(2, 4)]
    public string dialogoAntesDeInspecionar;

    [Tooltip("Opcional: Diálogo antes de inspecionar no 1º nível de dano (Laranja)")]
    [TextArea(2, 4)]
    public string dialogoDano1Laranja;

    [Tooltip("Opcional: Diálogo antes de inspecionar no 2º nível de dano (Vermelho)")]
    [TextArea(2, 4)]
    public string dialogoDano2Vermelho;

    [Header("Coletar ao Pressionar E na Inspeção (Opcional)")]
    [Tooltip("Se preencher um ItemData aqui, ao apertar E dentro da inspeção 3D ele pega o item e coloca no inventário. O ESC sai sem pegar.")]
    public ItemData itemParaInventario;
    [Tooltip("Se marcado, esconde/desativa o objeto da cena após pegar o item.")]
    public bool ocultarObjetoAoColetar = true;
    public UnityEvent aoColetarItem;

    private bool jaColetado = false;

    void Start()
    {
        if (itemParaInventario != null && EstadoDoJogo.PuzzleConcluido("coletado_" + itemParaInventario.Id))
        {
            jaColetado = true;
            if (TryGetComponent<Interagivel>(out var inter)) inter.SetEnabled(false);
            if (ocultarObjetoAoColetar) gameObject.SetActive(false);
        }
    }

    // Conecte esta função no seu "On Interact ()" do Interagivel
    public void MandarParaAInspecao()
    {
        if (jaColetado) return;

        string dialogoEscolhido = GerenciadorDeDialogo.EscolherTextoPorDano(
            dialogoAntesDeInspecionar,
            dialogoDano1Laranja,
            dialogoDano2Vermelho
        );

        if (!string.IsNullOrEmpty(dialogoEscolhido) && GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AbrirInspecaoAposDialogo);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(AbrirInspecaoAposDialogo);
            GerenciadorDeDialogo.Instancia.MostrarTexto(dialogoEscolhido);
            return;
        }

        ExecutarAberturaInspecao();
    }

    private void AbrirInspecaoAposDialogo()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AbrirInspecaoAposDialogo);
        }

        ExecutarAberturaInspecao();
    }

    private void ExecutarAberturaInspecao()
    {
        if (GerenciadorDeInspecao.Instancia != null && modeloDoObjeto != null)
        {
            string historiaEscolhida = GerenciadorDeDialogo.EscolherTextoPorDano(
                textoDetalhado,
                textoDetalhadoDano1Laranja,
                textoDetalhadoDano2Vermelho
            );

            GerenciadorDeInspecao.Instancia.IniciarInspecao(
                modeloDoObjeto,
                Quaternion.Euler(rotacaoInicial),
                historiaEscolhida,
                posicaoInicial,
                escalaInicial
            );

            // Texto extra que só existe para este item
            if (textoExtraNaInspecao != null)
            {
                textoExtraNaInspecao.SetActive(true);
                GerenciadorDeInspecao.Instancia.aoTerminarInspecao.RemoveListener(EsconderTextoExtra);
                GerenciadorDeInspecao.Instancia.aoTerminarInspecao.AddListener(EsconderTextoExtra);
            }

            // Função da tecla Q escolhida no Inspector
            if (funcaoDaTeclaQ != ModoTeclaQ.LerDetalhes)
            {
                GerenciadorDeInspecao.Instancia.DefinirTeclaQ(funcaoDaTeclaQ, AoAlternarFoco);
            }

            if (itemParaInventario != null)
            {
                GerenciadorDeInspecao.Instancia.DefinirAcaoAoPressionarE(ColetarParaInventario);
            }
        }
        else
        {
            Debug.LogError("Faltou colocar o modelo 3D no script ou falta o Gerenciador na cena!", this);
        }
    }

    private void EsconderTextoExtra()
    {
        if (textoExtraNaInspecao != null) textoExtraNaInspecao.SetActive(false);

        if (GerenciadorDeInspecao.Instancia != null)
        {
            GerenciadorDeInspecao.Instancia.aoTerminarInspecao.RemoveListener(EsconderTextoExtra);
        }
    }

    private void AoAlternarFoco(bool focando)
    {
        if (focando) aoFocar?.Invoke();
        else aoDesfocar?.Invoke();
    }

    private void ColetarParaInventario()
    {
        if (jaColetado || itemParaInventario == null) return;

        // Garante que o ItemData do inventário também tenha o texto detalhado se estiver vazio no asset
        if (string.IsNullOrWhiteSpace(itemParaInventario.textoDetalhado))
        {
            string historia = GerenciadorDeDialogo.EscolherTextoPorDano(
                textoDetalhado,
                textoDetalhadoDano1Laranja,
                textoDetalhadoDano2Vermelho
            );
            if (!string.IsNullOrWhiteSpace(historia))
            {
                itemParaInventario.textoDetalhado = historia;
            }
        }

        if (GerenciadorDeInventario.Instancia != null)
        {
            GerenciadorDeInventario.Instancia.Adicionar(itemParaInventario);
        }
        else
        {
            EstadoDoJogo.AdicionarItem(itemParaInventario.Id);
        }

        jaColetado = true;
        EstadoDoJogo.MarcarPuzzleConcluido("coletado_" + itemParaInventario.Id);

        if (TryGetComponent<Interagivel>(out var inter) ||
            (inter = GetComponentInChildren<Interagivel>(true)) != null ||
            (inter = GetComponentInParent<Interagivel>()) != null)
        {
            inter.SetEnabled(false);
        }

        aoColetarItem?.Invoke();

        if (ocultarObjetoAoColetar)
        {
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        // Se o objeto for desativado no meio da inspeção, o texto extra não pode ficar preso na tela
        if (textoExtraNaInspecao != null) textoExtraNaInspecao.SetActive(false);

        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AbrirInspecaoAposDialogo);
        }

        if (GerenciadorDeInspecao.Instancia != null)
        {
            GerenciadorDeInspecao.Instancia.aoTerminarInspecao.RemoveListener(EsconderTextoExtra);
        }
    }

    // Mantido por compatibilidade caso algum botão/evento chame AcionarInspecao
    public void AcionarInspecao() => MandarParaAInspecao();
}