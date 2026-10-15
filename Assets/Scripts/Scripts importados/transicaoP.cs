using System.Collections; 
using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para carregar a próxima cena

namespace PesquisaEEstudo
{
    public class TransicaoP : MonoBehaviour
    {
        public GameObject pivoPorta;

        void Start()
        {
            StartCoroutine(Abrirporta());
        }

        IEnumerator Abrirporta()
        {
            // Espera inicial (ex: tela preta antes de mostrar a porta)
            yield return new WaitForSeconds(3);

            // Toca a animação da porta
            pivoPorta.GetComponent<Animator>().Play(1);

            // Espera a animação terminar
            yield return new WaitForSeconds(4);

            // Carrega a próxima sala baseada na variável estática
            if (!string.IsNullOrEmpty(DadosDeTransicao.proximaCena))
            {
                SceneManager.LoadScene(DadosDeTransicao.proximaCena);
            }
            else
            {
                Debug.LogError("A próxima cena não foi definida!");
            }
        }
    }
}