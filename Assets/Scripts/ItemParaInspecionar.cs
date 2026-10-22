using UnityEngine;

public class ItemParaInspecionar : MonoBehaviour
{
    [Header("Modelo 3D")]
    [Tooltip("Arraste o modelo 3D (ou Prefab) que vai aparecer na tela escura")]
    public GameObject modeloDoObjeto;

    // Conecte esta função no seu "On Interact ()" do Interagivel
    public void MandarParaAInspecao()
    {
        if (GerenciadorDeInspecao.Instancia != null && modeloDoObjeto != null)
        {
            GerenciadorDeInspecao.Instancia.IniciarInspecao(modeloDoObjeto);
        }
        else
        {
            Debug.LogError("Faltou colocar o modelo 3D no script ou falta o Gerenciador na cena!");
        }
    }
}