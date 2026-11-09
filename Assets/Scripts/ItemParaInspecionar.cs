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

    // Conecte esta função no seu "On Interact ()" do Interagivel
    public void MandarParaAInspecao()
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

    // Mantido por compatibilidade caso algum botão/evento chame AcionarInspecao
    public void AcionarInspecao() => MandarParaAInspecao();
}
