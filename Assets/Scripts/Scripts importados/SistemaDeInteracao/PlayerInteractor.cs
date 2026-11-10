using UnityEngine;
using UnityEngine.InputSystem; // 1. Adicione o namespace do novo sistema

public class PlayerInteractor : MonoBehaviour
{
    [Header("Configurações de Interação")]
    // 2. Troque KeyCode por Key
    [SerializeField] private Key botaoDeInteracao = Key.E; 
    [SerializeField] private float distanciaDeInteracao = 1.5f;

    void Update()
    {
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

        // 3. Atualize a verificação da tecla para o novo sistema
        if (Keyboard.current != null && Keyboard.current[botaoDeInteracao].wasPressedThisFrame)
        {
            TentarInteragir();
        }
    }

    void TentarInteragir()
    {
        // Pega todos os objetos interagíveis da cena
        Interagivel[] todosInteragiveis = Object.FindObjectsByType<Interagivel>(FindObjectsSortMode.None);
        
        Interagivel maisProximo = null;
        float menorDistancia = distanciaDeInteracao;

        // Vasculha para encontrar qual está perto o suficiente do jogador
        foreach (var interagivel in todosInteragiveis)
        {
            if (!interagivel.CanInteract()) continue;

            float distancia = Vector3.Distance(transform.position, interagivel.transform.position);
            
            // Se estiver dentro da distância e for o mais perto até agora, salva a referência
            if (distancia <= menorDistancia)
            {
                maisProximo = interagivel;
                menorDistancia = distancia;
            }
        }

        // Se encontrou algo perto o suficiente, aperta o "gatilho"
        if (maisProximo != null)
        {
            maisProximo.Interact();
        }
    }
}