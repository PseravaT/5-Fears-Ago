using UnityEngine;
using UnityEngine.Events;

public class DialogoEInspecao : MonoBehaviour
{
    [Header("1. Fala Inicial")]
    [TextArea(2, 4)]
    public string falaDoPersonagem = "Acho que a Mãe lia para mim";

    [Header("2. Modelo 3D e História (Tecla Q)")]
    public GameObject modelo3DParaInspecao;
    [TextArea(5, 10)]
    public string textoDetalhado;

    // Chamado pelo On Interact() do objeto
    public void IniciarInteracao()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            // Limpa ouvintes anteriores para não acumular e escuta o encerramento deste diálogo
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AoFinalizarFala);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(AoFinalizarFala);

            // Inicia o diálogo com a frase configurada
            GerenciadorDeDialogo.Instancia.MostrarTexto(falaDoPersonagem);
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
            if (GerenciadorDeInspecao.Instancia.componenteTextoHistoria != null)
            {
                GerenciadorDeInspecao.Instancia.componenteTextoHistoria.text = textoDetalhado;
            }

            GerenciadorDeInspecao.Instancia.IniciarInspecao(modelo3DParaInspecao);
        }
    }
}