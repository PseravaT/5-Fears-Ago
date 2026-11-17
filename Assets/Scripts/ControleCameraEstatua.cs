using UnityEngine;
using Unity.Cinemachine;

public class ControleCameraEstatua : MonoBehaviour
{
    [Header("Referência do Puzzle da Estátua")]
    public PuzzleRelogioHumano puzzleEstatua;

    [Header("Câmeras por Posição")]
    [Tooltip("Câmera padrão/inicial (ativa nas 6h ou quando nenhuma das outras estiver no horário exato)")]
    public CinemachineCamera camera6hCentral;
    public CinemachineCamera camera12h;
    public CinemachineCamera camera3h;
    public CinemachineCamera camera9h;

    [Header("Configuração de Prioridades Cinemachine")]
    [SerializeField] private int prioridadeAtiva = 10;
    [SerializeField] private int prioridadeInativa = 0;

    private int ultimaHoraProcessada = -1;
    private CinemachineCamera cameraAtivaAtual;

    void Start()
    {
        if (puzzleEstatua == null)
            puzzleEstatua = FindFirstObjectByType<PuzzleRelogioHumano>();

        // Começa com a câmera central das 6h como ativa
        cameraAtivaAtual = camera6hCentral;

        // Registra o evento para trocar a câmera assim que a hora mudar no puzzle
        if (puzzleEstatua != null)
        {
            puzzleEstatua.aoMudarHora.RemoveListener(AtualizarCameraPorHora);
            puzzleEstatua.aoMudarHora.AddListener(AtualizarCameraPorHora);
            AtualizarCameraPorHora(puzzleEstatua.HoraAtual);
        }
        else
        {
            AtualizarCameraPorHora(6);
        }
    }

    void Update()
    {
        // Garante a sincronia caso a hora mude externamente sem disparo de evento
        if (puzzleEstatua != null && puzzleEstatua.HoraAtual != ultimaHoraProcessada)
        {
            AtualizarCameraPorHora(puzzleEstatua.HoraAtual);
        }
    }

    public void AtualizarCameraPorHora(int hora)
    {
        ultimaHoraProcessada = hora;

        // Só troca de câmera se a estátua atingir um dos 4 marcos (6, 9, 12 ou 3).
        // Durante as horas intermediárias (ex: 7h, 8h, 10h, etc.), MANTÉM a última câmera ativa!
        CinemachineCamera novaCamera = ObterCameraEspecificaPorHora(hora);

        if (novaCamera != null)
        {
            cameraAtivaAtual = novaCamera;
        }

        AplicarPrioridades();
    }

    private CinemachineCamera ObterCameraEspecificaPorHora(int hora)
    {
        switch (hora)
        {
            case 6:
                return camera6hCentral;
            case 9:
                return camera9h != null ? camera9h : camera6hCentral;
            case 12:
                return camera12h != null ? camera12h : camera6hCentral;
            case 3:
                return camera3h != null ? camera3h : camera6hCentral;
            default:
                // Horas intermediárias (1, 2, 4, 5, 7, 8, 10, 11): retorna null para NÃO trocar e manter a atual
                return null;
        }
    }

    private void AplicarPrioridades()
    {
        DefinirPrioridade(camera6hCentral, cameraAtivaAtual == camera6hCentral);
        DefinirPrioridade(camera12h, cameraAtivaAtual == camera12h);
        DefinirPrioridade(camera3h, cameraAtivaAtual == camera3h);
        DefinirPrioridade(camera9h, cameraAtivaAtual == camera9h);
    }

    private void DefinirPrioridade(CinemachineCamera cam, bool ativa)
    {
        if (cam != null)
        {
            cam.Priority = ativa ? prioridadeAtiva : prioridadeInativa;
        }
    }

    void OnDestroy()
    {
        if (puzzleEstatua != null)
        {
            puzzleEstatua.aoMudarHora.RemoveListener(AtualizarCameraPorHora);
        }
    }
}
