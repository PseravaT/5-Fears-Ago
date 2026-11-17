using UnityEngine;
using UnityEngine.InputSystem; // 1. Adicione o namespace do novo sistema
using System.Collections.Generic;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Configurações de Interação")]
    // 2. Troque KeyCode por Key
    [SerializeField] private Key botaoDeInteracao = Key.E; 
    [Tooltip("Arraste aqui o objeto filho 'área interação' (BoxCollider) que fica na frente do Player.")]
    [SerializeField] private BoxCollider caixaDeInteracao;
    [SerializeField] private float distanciaDeInteracao = 1.5f;

    private readonly HashSet<Interagivel> objetosNaCaixa = new HashSet<Interagivel>();

    void Awake()
    {
        if (caixaDeInteracao == null)
        {
            caixaDeInteracao = GetComponentInChildren<BoxCollider>();
        }

        if (caixaDeInteracao != null)
        {
            // Garante que a caixa nunca colida fisicamente com paredes ou portas
            caixaDeInteracao.isTrigger = true;
        }
    }

    void Update()
    {
        AtualizarObjetosNaCaixa();

        // Se um diálogo já estiver aberto (ou acabou de fechar neste exato frame com a tecla E),
        // não dispara uma nova interação por cima para não reiniciar o diálogo.
        if (GerenciadorDeDialogo.Instancia != null &&
            (GerenciadorDeDialogo.Instancia.DialogoEstaAtivo || GerenciadorDeDialogo.Instancia.FechouNesteFrame))
        {
            return;
        }

        // Se uma inspeção 3D estiver ativa (ou acabou de fechar neste exato frame com a tecla E),
        // não dispara uma nova interação por cima.
        if (GerenciadorDeInspecao.Instancia != null &&
            (GerenciadorDeInspecao.Instancia.EstaInspecionando || GerenciadorDeInspecao.Instancia.FechouNesteFrame))
        {
            return;
        }

        // Se o Puzzle de Ingredientes estiver ativo (ou acabou de fechar com E),
        // não deixa interagir com o objeto novamente para não reabrir o diálogo no meio do puzzle!
        if (PuzzleIngredientes.Instancia != null &&
            (PuzzleIngredientes.Instancia.EstaAtivo || PuzzleIngredientes.Instancia.FechouNesteFrame))
        {
            return;
        }

        // 3. Atualize a verificação da tecla para o novo sistema
        if (Keyboard.current != null && Keyboard.current[botaoDeInteracao].wasPressedThisFrame)
        {
            TentarInteragir();
        }
    }

    private void AtualizarObjetosNaCaixa()
    {
        objetosNaCaixa.Clear();
        if (caixaDeInteracao == null) return;

        Transform t = caixaDeInteracao.transform;
        Vector3 centroMundo = t.TransformPoint(caixaDeInteracao.center);
        Vector3 metadeTamanho = Vector3.Scale(caixaDeInteracao.size * 0.5f, t.lossyScale);
        metadeTamanho = new Vector3(Mathf.Abs(metadeTamanho.x), Mathf.Abs(metadeTamanho.y), Mathf.Abs(metadeTamanho.z));

        // Detecta qualquer colisor interagível que esteja tocando ou dentro da caixa
        Collider[] colisores = Physics.OverlapBox(centroMundo, metadeTamanho, t.rotation, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < colisores.Length; i++)
        {
            Interagivel inter = colisores[i].GetComponentInParent<Interagivel>();
            if (inter != null && inter.CanInteract())
            {
                objetosNaCaixa.Add(inter);
            }
        }
    }

    public bool EstaDentroDaCaixa(Interagivel interagivel)
    {
        if (interagivel == null || !interagivel.CanInteract()) return false;

        if (caixaDeInteracao != null)
        {
            if (objetosNaCaixa.Contains(interagivel)) return true;

            // Suporte total a Empty Objects ou objetos com pivô descentralizado:
            // Checa a posição do objeto (e a de seus filhos/colisores caso existam)
            Vector3 posAvaliar = interagivel.transform.position;
            if (interagivel.TryGetComponent<Collider>(out var col))
            {
                posAvaliar = col.bounds.center;
            }

            Vector3 posLocal = caixaDeInteracao.transform.InverseTransformPoint(posAvaliar) - caixaDeInteracao.center;
            Vector3 metade = caixaDeInteracao.size * 0.5f;
            return Mathf.Abs(posLocal.x) <= metade.x &&
                   Mathf.Abs(posLocal.y) <= metade.y &&
                   Mathf.Abs(posLocal.z) <= metade.z;
        }

        return Vector3.Distance(transform.position, interagivel.transform.position) <= distanciaDeInteracao;
    }

    void TentarInteragir()
    {
        // Pega todos os objetos interagíveis da cena
        Interagivel[] todosInteragiveis = Object.FindObjectsByType<Interagivel>(FindObjectsSortMode.None);
        
        Interagivel maisProximo = null;
        float menorDistancia = caixaDeInteracao != null ? float.MaxValue : distanciaDeInteracao;
        Vector3 pontoReferencia = caixaDeInteracao != null
            ? caixaDeInteracao.transform.TransformPoint(caixaDeInteracao.center)
            : transform.position;

        // Vasculha para encontrar qual está dentro da caixa (e mais perto do centro dela)
        foreach (var interagivel in todosInteragiveis)
        {
            if (!EstaDentroDaCaixa(interagivel)) continue;

            Vector3 posAlvo = ObterPontoMaisProximo(interagivel, pontoReferencia);

            float distancia = Vector3.Distance(pontoReferencia, posAlvo);
            
            if (distancia <= menorDistancia)
            {
                maisProximo = interagivel;
                menorDistancia = distancia;
            }
        }

        // Se encontrou algo perto o suficiente dentro da caixa, aperta o "gatilho"
        if (maisProximo != null)
        {
            maisProximo.Interact();
        }
    }

    private Vector3 ObterPontoMaisProximo(Interagivel interagivel, Vector3 pontoOrigem)
    {
        if (interagivel.TryGetComponent<Collider>(out var col))
        {
            // MeshCollider não-convexo não suporta ClosestPoint na Unity
            if (col is MeshCollider meshCol && !meshCol.convex)
            {
                return col.bounds.ClosestPoint(pontoOrigem);
            }

            return col.ClosestPoint(pontoOrigem);
        }

        return interagivel.transform.position;
    }
}

