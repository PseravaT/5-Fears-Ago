using UnityEngine;
using TMPro; // Biblioteca do TextMeshPro

public class GerenciadorDeDialogo : MonoBehaviour
{
    [Header("Elementos da Interface")]
    [SerializeField] private GameObject painelBalao; // A imagem de fundo (a caixa preta)
    [SerializeField] private TextMeshProUGUI componenteDeTexto; // O texto que vai aparecer

    void Start()
    {
        // Garante que o balão comece invisível quando o jogo iniciar
        EsconderTexto();
    }

    // Essa é a mágica: O UnityEvent vai mandar a frase (string) direto para cá!
    public void MostrarTexto(string mensagem)
    {
        painelBalao.SetActive(true);
        componenteDeTexto.text = mensagem;
    }

    public void EsconderTexto()
    {
        painelBalao.SetActive(false);
        componenteDeTexto.text = "";
    }
}