using UnityEngine;

[RequireComponent(typeof(Light))]
public class LuzInspecaoRecuo : MonoBehaviour
{
    [Tooltip("Distância que a luz mantém do objeto. Aumente se ainda estiver estourando.")]
    [SerializeField] private float distanciaDaLuzAoObjeto = 2.5f;

    [Tooltip("Se ligado, a luz só recua (nunca passa da posição original em direção ao objeto)")]
    [SerializeField] private bool soRecua = true;

    [Tooltip("Quanto a luz pode recuar no máximo atrás da posição original. 0 = sem limite")]
    [SerializeField] private float recuoMaximo = 0f;

    [Header("Range")]
    [Tooltip("Acompanha a distância para o objeto continuar dentro do alcance da luz")]
    [SerializeField] private bool ajustarRange = true;
    [Tooltip("Alcance extra além da distância até o objeto (cobre o tamanho do modelo)")]
    [SerializeField] private float folgaDoRange = 1.5f;

    private Light luz;
    private Vector3 posicaoLocalOriginal;
    private float rangeOriginal;

    private void Awake()
    {
        luz = GetComponent<Light>();
        posicaoLocalOriginal = transform.localPosition;
        rangeOriginal = luz.range;
    }

    private void OnDisable()
    {
        Restaurar();
    }

    private void LateUpdate()
    {
        var gerenciador = GerenciadorDeInspecao.Instancia;

        if (gerenciador == null || !gerenciador.EstaInspecionando)
        {
            Restaurar();
            return;
        }

        // Z local em que a luz fica exatamente a "distanciaDaLuzAoObjeto" do objeto
        float zDesejado = gerenciador.DistanciaAtualDoObjeto - distanciaDaLuzAoObjeto;

        float zFinal = soRecua ? Mathf.Min(posicaoLocalOriginal.z, zDesejado) : zDesejado;

        if (recuoMaximo > 0f)
        {
            zFinal = Mathf.Max(zFinal, posicaoLocalOriginal.z - recuoMaximo);
        }

        Vector3 pos = posicaoLocalOriginal;
        pos.z = zFinal;
        transform.localPosition = pos;

        if (ajustarRange)
        {
            // Distância real entre a luz e o objeto + folga
            float distanciaReal = gerenciador.DistanciaAtualDoObjeto - zFinal;
            luz.range = Mathf.Max(rangeOriginal, distanciaReal + folgaDoRange);
        }
    }

    private void Restaurar()
    {
        if (luz == null) return;
        transform.localPosition = posicaoLocalOriginal;
        luz.range = rangeOriginal;
    }
}