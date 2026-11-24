using UnityEngine;
using UnityEngine.SceneManagement;

public class TrocarDeCena : MonoBehaviour
{
    [SerializeField] private string nomeDaCena;

    public void CarregarCena()
    {
        SceneManager.LoadScene(nomeDaCena);
    }
}