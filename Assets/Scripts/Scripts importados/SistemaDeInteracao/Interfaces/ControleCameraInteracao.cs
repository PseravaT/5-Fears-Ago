using UnityEngine;
using UnityEngine.Events; // Necessário para os eventos visuais
using UnityEngine.InputSystem;

public class ControleCameraInteracao : MonoBehaviour
{
    [Header("Câmera do Objeto")]
    [SerializeField] private GameObject cameraDesteObjeto;

    [Header("O que acontece ao ENTRAR?")]
    public UnityEvent aoEntrarNaVisao;

    [Header("O que acontece ao SAIR?")]
    public UnityEvent aoSairDaVisao;

    private bool estaInteragindo = false;

    public void EntrarNaVisao()
    {
        if (estaInteragindo) return;

        cameraDesteObjeto.SetActive(true);
        
        // Dispara tudo que você configurar no Inspector para desligar
        aoEntrarNaVisao?.Invoke(); 

        estaInteragindo = true;
    }

    void Update()
    {
        if (estaInteragindo && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SairDaVisao();
        }
    }

    private void SairDaVisao()
    {
        cameraDesteObjeto.SetActive(false);
        
        // Dispara tudo que você configurar no Inspector para religar
        aoSairDaVisao?.Invoke();

        estaInteragindo = false;
    }
}