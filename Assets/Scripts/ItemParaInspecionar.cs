using UnityEngine;
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

    [Header("História do Objeto (Tecla Q)")]
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

    // Conecte esta função no seu "On Interact ()" do Interagivel
    public void MandarParaAInspecao()
    {
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
                posicaoInicial
            );
        }
        else
        {
            Debug.LogError("Faltou colocar o modelo 3D no script ou falta o Gerenciador na cena!", this);
        }
    }

    void OnDisable()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AbrirInspecaoAposDialogo);
        }
    }

    // Mantido por compatibilidade caso algum botão/evento chame AcionarInspecao
    public void AcionarInspecao() => MandarParaAInspecao();
}