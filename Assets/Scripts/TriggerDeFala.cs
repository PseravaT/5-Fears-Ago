using UnityEngine;
using System.Collections; // Obrigatório para usar temporizadores (IEnumerator)

public class TriggerDeFala : MonoBehaviour
{
    [Header("Configurações do Texto")]
    [TextArea(3, 5)] 
    public string mensagemDoGatilho;
    
    [Tooltip("Quantos segundos o texto vai ficar na tela antes de sumir?")]
    public float tempoNaTela = 4f; 
    
    [Header("Referências")]
    public GerenciadorDeDialogo gerenciadorDeDialogo;

    private bool jaFoiAtivado = false; 

    private void OnTriggerEnter(Collider outro)
    {
        if (outro.CompareTag("Player") && !jaFoiAtivado)
        {
            jaFoiAtivado = true; 

            if (gerenciadorDeDialogo != null)
            {
                gerenciadorDeDialogo.MostrarTexto(mensagemDoGatilho);
                
                // Inicia o cronômetro para esconder o texto
                StartCoroutine(EsconderDepoisDeTempo());
            }
            else
            {
                Debug.LogWarning("Esqueceu de arrastar o Gerenciador de Diálogo para o Inspector do gatilho!");
            }

            // Desliga apenas o colisor, mantendo o objeto vivo para o temporizador terminar
            GetComponent<Collider>().enabled = false;
        }
    }

    private IEnumerator EsconderDepoisDeTempo()
    {
        // Espera os segundos configurados no Inspector
        yield return new WaitForSeconds(tempoNaTela);

        // Manda o gerenciador apagar o balão de fala
        if (gerenciadorDeDialogo != null)
        {
            gerenciadorDeDialogo.EsconderTexto();
        }
    }
}