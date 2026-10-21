using UnityEngine;
using TMPro;
using System.Collections;

public class GerenciadorDeDialogo : MonoBehaviour
{
    [Header("Elementos da Interface")]
    [SerializeField] private GameObject painelBalao;
    [SerializeField] private TextMeshProUGUI componenteDeTexto;

    [Header("Configurações de Tempo")]
    [SerializeField] private float tempoParaAparecer = 1.5f; // Tempo que o balão espera antes de surgir
    [SerializeField] private float delayEntreLetras = 0.05f; // Velocidade da máquina de escrever

    private Coroutine corrotinaAtual;

    void Start()
    {
        EsconderTexto();
    }

    public void MostrarTexto(string mensagem)
    {
        // Interrompe qualquer rotina anterior para evitar conflitos
        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
        }
        
        // Inicia a nova rotina com o delay inicial
        corrotinaAtual = StartCoroutine(RotinaMostrarBalão(mensagem));
    }

    private IEnumerator RotinaMostrarBalão(string mensagem)
    {
        // 1. Garante que a caixa preta está invisível enquanto a câmara viaja
        painelBalao.SetActive(false);
        componenteDeTexto.text = "";

        // 2. Espera o tempo configurado (ex: o tempo da transição do Cinemachine)
        yield return new WaitForSeconds(tempoParaAparecer);

        // 3. Agora sim, ativa a caixa preta e começa o diálogo
        painelBalao.SetActive(true);

        // 4. Efeito de digitação letra a letra
        foreach (char letra in mensagem.ToCharArray())
        {
            componenteDeTexto.text += letra;
            yield return new WaitForSeconds(delayEntreLetras);
        }
    }

    public void EsconderTexto()
    {
        // Para qualquer texto que esteja a ser escrito e esconde o balão instantaneamente
        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
        }
        
        painelBalao.SetActive(false);
        componenteDeTexto.text = "";
    }
}