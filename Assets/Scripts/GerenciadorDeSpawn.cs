using UnityEngine;

public class GerenciadorDeSpawn : MonoBehaviour
{
    void Start()
    {
        // Verifica se existe um local de destino salvo na memória
        if (!string.IsNullOrEmpty(DadosDeTransicao.pontoDeSpawnAlvo))
        {
            // Procura na cena o objeto que tem exatamente esse nome
            GameObject pontoDeSpawn = GameObject.Find(DadosDeTransicao.pontoDeSpawnAlvo);
            
            if (pontoDeSpawn != null)
            {
                // É obrigatório desligar o CharacterController por um frame para o teleporte funcionar no Unity
                CharacterController controller = GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;

                // Move o jogador e copia a rotação (para ele nascer olhando para a direção certa)
                transform.position = pontoDeSpawn.transform.position;
                transform.rotation = pontoDeSpawn.transform.rotation;

                if (controller != null) controller.enabled = true;
                
                // Limpa a variável para evitar bugs ao recarregar cenas normais
                DadosDeTransicao.pontoDeSpawnAlvo = "";
            }
            else
            {
                Debug.LogWarning("Ponto de Spawn não encontrado: " + DadosDeTransicao.pontoDeSpawnAlvo);
            }
        }
    }
}