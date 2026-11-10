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

        estaInteragindo = true;

        if (cameraDesteObjeto != null)
            cameraDesteObjeto.SetActive(true);

        // Conecta automaticamente ao fim do diálogo para sair da visão igual ao ESC
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(SairDaVisao);
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.AddListener(SairDaVisao);
        }
        
        // Dispara tudo que você configurar no Inspector para desligar
        aoEntrarNaVisao?.Invoke(); 
    }

    void Update()
    {
        if (estaInteragindo && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SairDaVisao();
        }
    }

    public void SairDaVisao()
    {
        if (!estaInteragindo) return;

        estaInteragindo = false;

        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(SairDaVisao);

            // Se saiu apertando ESC enquanto o diálogo ainda estava aberto, esconde o diálogo também
            if (GerenciadorDeDialogo.Instancia.DialogoEstaAtivo)
            {
                GerenciadorDeDialogo.Instancia.EsconderTexto();
            }
        }

        if (cameraDesteObjeto != null)
            cameraDesteObjeto.SetActive(false);
        
        // Dispara tudo que você configurar no Inspector para religar
        aoSairDaVisao?.Invoke();
    }

    void OnDisable()
    {
        if (GerenciadorDeDialogo.Instancia != null)
        {
            GerenciadorDeDialogo.Instancia.aoFecharDialogo.RemoveListener(SairDaVisao);
        }
    }
}