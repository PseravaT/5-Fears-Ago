using UnityEngine;

/// <summary>
/// Um item do jogo. Crie pelo menu: Assets > Create > Inventario > Item
/// e salve dentro de Assets/Resources/Itens para o inventário encontrá-lo.
/// </summary>
[CreateAssetMenu(fileName = "NovoItem", menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    [Tooltip("ID único, sem vírgulas. É o que fica salvo. Se vazio, usa o nome do arquivo")]
    [SerializeField] private string id;
    [SerializeField] private string nomeDoItem;
    public Sprite icone;
    [TextArea(2, 5)] public string descricaoCurta;
    
    [Tooltip("X/Y deslocam o objeto na tela; Z soma na distância da câmera (negativo = mais perto)")]
    public Vector3 posicaoInicialInspecao;

    [Header("Inspeção (opcional)")]
    [Tooltip("Se preenchido, aparece o botão Inspecionar no inventário")]
    public GameObject modelo3D;
    public Vector3 rotacaoInicialGraus;
    [Tooltip("Escala do modelo na inspeção (1,1,1 = tamanho original do prefab)")]
    public Vector3 escalaInspecao = Vector3.one;
    [Header("Inspeção")]
    [Tooltip("Se ligado, a inspeção (tecla E) usa a rotação abaixo em vez da rotação inicial do preview")]
    public bool usarRotacaoPropriaNaInspecao;
    [Tooltip("Rotação do modelo na inspeção, em graus (relativa à câmera)")]
    public Vector3 rotacaoNaInspecaoGraus;
    [TextArea(3, 8)] public string textoDetalhado;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id.Trim();
    public string Nome => string.IsNullOrWhiteSpace(nomeDoItem) ? name : nomeDoItem;
}