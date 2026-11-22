using System.Collections;
using UnityEngine;

/// <summary>
/// Sincroniza os sons de porta abrindo e fechando na cena de transição (Porta / Porta entrada).
/// Pode disparar por tempo (em segundos no Inspector) ou via Animation Event (TocarSomAbrindo / TocarSomFechando).
/// </summary>
public class SincronizadorSomPorta : MonoBehaviour
{
    [Header("Fontes de Áudio (arraste PortaAbrindo e PortaFechando)")]
    [SerializeField] private AudioSource audioPortaAbrindo;
    [SerializeField] private AudioSource audioPortaFechando;

    [Header("Sincronização por Tempo (Recomendado)")]
    [Tooltip("Se marcado, dispara os sons nos segundos definidos abaixo assim que a cena inicia.")]
    [SerializeField] private bool tocarPorTempo = true;
    [Tooltip("Segundos após iniciar a cena para tocar o som de porta abrindo.")]
    [Min(0f)] [SerializeField] private float atrasoSomAbrindo = 0.2f;
    [Tooltip("Segundos após iniciar a cena para tocar o som de porta fechando.")]
    [Min(0f)] [SerializeField] private float atrasoSomFechando = 2.0f;

    [Header("Clipes Opcionais (caso o AudioSource não tenha AudioClip atribuído)")]
    [SerializeField] private AudioClip clipAbrindo;
    [SerializeField] private AudioClip clipFechando;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    void Awake()
    {
        // Garante que não toquem sozinhos no frame 0 se estiver usando sincronização por tempo ou Animation Event
        if (audioPortaAbrindo != null) audioPortaAbrindo.playOnAwake = false;
        if (audioPortaFechando != null) audioPortaFechando.playOnAwake = false;
    }

    void Start()
    {
        if (tocarPorTempo)
        {
            StartCoroutine(RotinaTocarDepois(atrasoSomAbrindo, TocarSomAbrindo));
            StartCoroutine(RotinaTocarDepois(atrasoSomFechando, TocarSomFechando));
        }
    }

    private IEnumerator RotinaTocarDepois(float atraso, System.Action acao)
    {
        if (atraso > 0f)
            yield return new WaitForSeconds(atraso);

        acao?.Invoke();
    }

    /// <summary>
    /// Toca o som da porta abrindo (pode ser chamado por Animation Event no frame exato).
    /// </summary>
    public void TocarSomAbrindo()
    {
        TocarFonte(audioPortaAbrindo, clipAbrindo);
    }

    /// <summary>
    /// Toca o som da porta fechando (pode ser chamado por Animation Event no frame exato).
    /// </summary>
    public void TocarSomFechando()
    {
        TocarFonte(audioPortaFechando, clipFechando);
    }

    private void TocarFonte(AudioSource fonte, AudioClip clipOpcional)
    {
        if (fonte == null) return;

        fonte.volume = volume;

        if (clipOpcional != null)
        {
            fonte.PlayOneShot(clipOpcional, volume);
        }
        else if (fonte.clip != null)
        {
            fonte.Play();
        }
    }
}
