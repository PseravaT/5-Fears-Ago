using UnityEngine;
using UnityEngine.Serialization;

public class ItemParaInspecionar : MonoBehaviour
{
    [Header("Modelo 3D")]
    [Tooltip("Arraste o modelo 3D (ou Prefab) que vai aparecer flutuando na tela")]
    [FormerlySerializedAs("modelo3DParaInspecao")]
    public GameObject modeloDoObjeto;

    [Header("História do Objeto (Tecla Q)")]
    [Tooltip("Texto customizado que aparece ao apertar Q durante a inspeção")]
    [TextArea(5, 10)]
    public string textoDetalhado;

    [Header("Diálogo Prévio (Opcional)")]
    [Tooltip("Se preenchido, exibe este monólogo/diálogo antes de abrir o 3D. O 3D só abre quando o jogador apertar E na última frase!")]
    [TextArea(2, 4)]
    public string dialogoAntesDeInspecionar;

    // Conecte esta função no seu "On Interact ()" do Interagivel
    public void MandarParaAInspecao()
    {
        // Se houver um diálogo prévio configurado, exibe primeiro o diálogo e só abre o 3D ao fechar!
        if (!string.IsNullOrEmpty(dialogoAntesDeInspecionar) && GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(AbrirInspecaoAposDialogo);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(AbrirInspecaoAposDialogo);
            GerenciadorDeDialogo.Instancia.MostrarTexto(dialogoAntesDeInspecionar);
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
            GerenciadorDeInspecao.Instancia.IniciarInspecao(modeloDoObjeto, textoDetalhado);
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
