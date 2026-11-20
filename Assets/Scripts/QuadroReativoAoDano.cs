using UnityEngine;

/// <summary>
/// Altera a foto dos quadros (Textura dentro de um Material compartilhado ou Material no MeshRenderer)
/// automaticamente conforme o estágio de vida/dano atual do jogador:
/// Estágio 0 (3 Vidas / Normal) -> Verde
/// Estágio 1 (2 Vidas / 1º Erro) -> Laranja
/// Estágio 2+ (1 Vida / 2º Erro) -> Vermelho
/// </summary>
public class QuadroReativoAoDano : MonoBehaviour
{
    [Header("Opção 1: Mudar a Textura direto no Material das Fotos")]
    [Tooltip("Arraste aqui o Material que está vinculado às fotos dos quadros (ex: Ronaldinho1 / Ronaldinho3). Todos os quadros com este material mudarão juntos!")]
    public Material materialDaFoto;

    [Tooltip("Foto quando o jogador está com a vida cheia (Estágio 0 - Verde)")]
    public Texture2D fotoVerde;

    [Tooltip("Foto após o 1º erro/dano (Estágio 1 - Laranja)")]
    public Texture2D fotoLaranja;

    [Tooltip("Foto após o 2º erro/dano (Estágio 2 - Vermelho)")]
    public Texture2D fotoVermelha;

    [Header("Opção 2 (Opcional): Trocar o Material no MeshRenderer deste Quadro")]
    [Tooltip("Se preferir trocar o Material inteiro em vez da textura, arraste o MeshRenderer do quadro (ou deixe vazio para buscar neste objeto)")]
    public MeshRenderer meshRendererDoQuadro;

    [Tooltip("Qual slot da lista Materials do MeshRenderer é a foto (nos quadros com moldura geralmente o slot 0 é a moldura e o slot 1 é a foto)")]
    public int indiceDoMaterialNoMesh = 1;

    public Material materialVerde;
    public Material materialLaranja;
    public Material materialVermelho;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private int ultimoEstagioAplicado = -1;

    void OnEnable()
    {
        SistemaDeVida.AoMudarEstagioGlobal += AplicarFotoDoEstagio;
        AplicarFotoDoEstagio(ObterEstagioAtual());
    }

    void Start()
    {
        AplicarFotoDoEstagio(ObterEstagioAtual());
    }

    void Update()
    {
        int estagio = ObterEstagioAtual();
        if (estagio != ultimoEstagioAplicado)
        {
            AplicarFotoDoEstagio(estagio);
        }
    }

    void OnDisable()
    {
        SistemaDeVida.AoMudarEstagioGlobal -= AplicarFotoDoEstagio;

        // Restaura a foto Verde no asset do Material ao sair do Play Mode
        // para não deixar o material salvo com a foto vermelha no Editor
        if (materialDaFoto != null && fotoVerde != null)
        {
            AplicarTexturaNoMaterial(materialDaFoto, fotoVerde);
        }
    }

    private int ObterEstagioAtual()
    {
        if (DamageSystem.Instancia != null)
            return Mathf.Max(SistemaDeVida.EstagioGlobalAtual, DamageSystem.Instancia.ShownHits);

        return SistemaDeVida.EstagioGlobalAtual;
    }

    public void AplicarFotoDoEstagio(int estagioDeDano)
    {
        ultimoEstagioAplicado = estagioDeDano;

        // 1. Se configurou o Material compartilhado + Texturas (Verde, Laranja, Vermelha)
        if (materialDaFoto != null)
        {
            Texture2D texturaEscolhida = EscolherTextura(estagioDeDano);
            if (texturaEscolhida != null)
            {
                AplicarTexturaNoMaterial(materialDaFoto, texturaEscolhida);
            }
        }

        // 2. Se configurou troca de Materiais no MeshRenderer deste quadro específico
        Material matEscolhido = EscolherMaterial(estagioDeDano);
        if (matEscolhido != null)
        {
            if (meshRendererDoQuadro == null)
                meshRendererDoQuadro = GetComponent<MeshRenderer>();

            if (meshRendererDoQuadro != null)
            {
                Material[] mats = meshRendererDoQuadro.sharedMaterials;
                int slot = EncontrarIndiceDoMaterial(mats);
                if (mats.Length > 0 && slot >= 0 && slot < mats.Length && mats[slot] != matEscolhido)
                {
                    mats[slot] = matEscolhido;
                    meshRendererDoQuadro.sharedMaterials = mats;
                }
            }
        }
    }

    private int EncontrarIndiceDoMaterial(Material[] mats)
    {
        if (mats == null || mats.Length == 0) return -1;

        // Procura automaticamente o slot que contém qualquer um dos materiais configurados (Verde, Laranja ou Vermelho)
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] != null && (mats[i] == materialVerde || mats[i] == materialLaranja || mats[i] == materialVermelho))
            {
                return i;
            }
        }

        // Fallback: usa o índice configurado no Inspector
        return Mathf.Clamp(indiceDoMaterialNoMesh, 0, mats.Length - 1);
    }

    private void AplicarTexturaNoMaterial(Material mat, Texture2D tex)
    {
        if (mat == null || tex == null) return;

        if (mat.HasProperty(BaseMapId))
            mat.SetTexture(BaseMapId, tex);

        if (mat.HasProperty(MainTexId))
            mat.SetTexture(MainTexId, tex);
    }

    private Texture2D EscolherTextura(int estagio)
    {
        if (estagio <= 0)
            return fotoVerde;
        if (estagio == 1)
            return fotoLaranja != null ? fotoLaranja : fotoVerde;

        return fotoVermelha != null ? fotoVermelha : (fotoLaranja != null ? fotoLaranja : fotoVerde);
    }

    private Material EscolherMaterial(int estagio)
    {
        if (estagio <= 0)
            return materialVerde;
        if (estagio == 1)
            return materialLaranja != null ? materialLaranja : materialVerde;

        return materialVermelho != null ? materialVermelho : (materialLaranja != null ? materialLaranja : materialVerde);
    }
}
