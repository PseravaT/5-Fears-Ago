using UnityEngine;

/// <summary>
/// Coloque em qualquer objeto e aponte o UnityEvent para Mostrar(string).
/// Sempre usa o GerenciadorDeDialogo vivo da cena, nunca o prefab do Project.
/// </summary>
public class DialogoRelay : MonoBehaviour
{
    public void Mostrar(string texto)
    {
        var gerenciador = GerenciadorDeDialogo.Instancia;

        if (gerenciador == null)
            gerenciador = FindFirstObjectByType<GerenciadorDeDialogo>(FindObjectsInactive.Include);

        if (gerenciador == null)
        {
            Debug.LogError("[DialogoRelay] Nenhum GerenciadorDeDialogo na cena.", this);
            return;
        }

        gerenciador.MostrarTexto(texto);
    }
}