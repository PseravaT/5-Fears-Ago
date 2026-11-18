using UnityEngine;

/// <summary>
/// Permite configurar 3 versões de um diálogo no Inspector de qualquer objeto interagível,
/// exibindo automaticamente a fala correspondente ao nível de dano atual do jogador:
/// - Verde (Sem Dano / 3 Vidas)
/// - Laranja (1º Dano / 2 Vidas)
/// - Vermelho (2º Dano / 1 Vida)
/// </summary>
public class DialogoReativoAoDano : MonoBehaviour
{
    [Header("Falas por Nível de Vida / Dano")]
    [Tooltip("Fala exibida quando o jogador está com a vida cheia (Estágio 0 - Verde). Use '|' para múltiplas caixas de fala.")]
    [TextArea(2, 4)]
    public string falaVerdeNormal;

    [Tooltip("Fala exibida após 1 erro/dano (Estágio 1 - Laranja). Se deixar vazio, usa a fala Verde.")]
    [TextArea(2, 4)]
    public string falaLaranjaDano1;

    [Tooltip("Fala exibida após 2 erros/dano (Estágio 2 - Vermelho). Se deixar vazio, usa a anterior.")]
    [TextArea(2, 4)]
    public string falaVermelhaDano2;

    /// <summary>
    /// Conecte esta função no evento On Interact () ou Ao Entrar Na Visao () do objeto!
    /// </summary>
    public void MostrarDialogoPorDano()
    {
        if (GerenciadorDeDialogo.Instancia == null)
        {
            Debug.LogWarning("GerenciadorDeDialogo não encontrado na cena!", this);
            return;
        }

        string falaEscolhida = GerenciadorDeDialogo.EscolherTextoPorDano(
            falaVerdeNormal,
            falaLaranjaDano1,
            falaVermelhaDano2
        );

        GerenciadorDeDialogo.Instancia.MostrarTexto(falaEscolhida);
    }
}
