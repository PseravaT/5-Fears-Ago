using UnityEngine;
using UnityEngine.Events;

public class DialogoEInspecao : MonoBehaviour
{
    [Header("1. Fala Inicial (Muda conforme o Dano)")]
    [Tooltip("Fala quando está sem dano (Verde / 3 Vidas)")]
    [TextArea(2, 4)]
    public string falaDoPersonagem = "Acho que a Mãe lia para mim";

    [Tooltip("Opcional: Fala quando está no 1º nível de dano (Laranja / 2 Vidas)")]
    [TextArea(2, 4)]
    public string falaDano1Laranja;

    [Tooltip("Opcional: Fala quando está no 2º nível de dano (Vermelho / 1 Vida)")]
    [TextArea(2, 4)]
    public string falaDano2Vermelho;

    [Header("2. Modelo 3D e História (Tecla Q)")]
    public GameObject modelo3DParaInspecao;
    [TextArea(5, 10)]
    public string textoDetalhado;

    [Tooltip("Opcional: Texto da tecla Q no 1º nível de dano (Laranja)")]
    [TextArea(3, 6)]
    public string textoDetalhadoDano1Laranja;

    [Tooltip("Opcional: Texto da tecla Q no 2º nível de dano (Vermelho)")]
    [TextArea(3, 6)]
    public string textoDetalhadoDano2Vermelho;

    // Chamado pelo On Interact() do objeto
    public void IniciarInteracao()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            // Limpa ouvintes anteriores para não acumular e escuta o encerramento deste diálogo
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AoFinalizarFala);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(AoFinalizarFala);

            string falaEscolhida = GerenciadorDeDialogo.EscolherTextoPorDano(
                falaDoPersonagem,
                falaDano1Laranja,
                falaDano2Vermelho
            );

            // Inicia o diálogo com a frase correspondente ao nível de dano
            GerenciadorDeDialogo.Instancia.MostrarTexto(falaEscolhida);
        }
        else
        {
            // Se não houver diálogo, abre a inspeção direto
            AbrirInspecao();
        }
    }

    private void AoFinalizarFala()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AoFinalizarFala);
        }

        // Abre a inspeção 3D imediatamente após o balão sumir
        AbrirInspecao();
    }

    public void AbrirInspecao()
    {
        if (GerenciadorDeInspecao.Instancia != null)
        {
            string historiaEscolhida = GerenciadorDeDialogo.EscolherTextoPorDano(
                textoDetalhado,
                textoDetalhadoDano1Laranja,
                textoDetalhadoDano2Vermelho
            );

            GerenciadorDeInspecao.Instancia.IniciarInspecao(modelo3DParaInspecao, historiaEscolhida);
        }
    }
}