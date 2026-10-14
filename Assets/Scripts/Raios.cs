using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Raios : MonoBehaviour
{
    [Header("Luz do clarão")]
    public Light luz;                      // a luz azul que entra pela janela
    public float intensidadeRaio = 6f;     // intensidade no pico do clarão

    [Header("Pós-processamento (opcional)")]
    public Volume volume;                  // o Global Volume
    public float exposicaoRaio = 2f;       // Post Exposure no pico

    [Header("Áudio")]
    public AudioSource fonteTrovao;
    public AudioClip[] trovoes;

    [Header("Tempos (segundos)")]
    public float intervaloMin = 6f;
    public float intervaloMax = 16f;
    public float atrasoTrovaoMin = 0.4f;
    public float atrasoTrovaoMax = 2.5f;
    public float duracaoDecaimento = 0.5f;

    float intensidadeBase;
    float exposicaoBase;
    ColorAdjustments ajuste;

    void Start()
    {
        if (luz) intensidadeBase = luz.intensity;
        if (volume && volume.profile.TryGet(out ajuste))
            exposicaoBase = ajuste.postExposure.value;
        StartCoroutine(Ciclo());
    }

    IEnumerator Ciclo()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervaloMin, intervaloMax));
            Disparar();
        }
    }

    public void Disparar()
    {
        StartCoroutine(Raio());
        StartCoroutine(Trovao());
    }

    IEnumerator Raio()
    {
        int piscadas = Random.Range(1, 4);
        for (int i = 0; i < piscadas; i++)
        {
            Definir(1f);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
            Definir(Random.Range(0.1f, 0.4f));
            yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
        }

        Definir(1f);
        yield return new WaitForSeconds(0.08f);

        float t = 0;
        while (t < duracaoDecaimento)
        {
            t += Time.deltaTime;
            Definir(1f - t / duracaoDecaimento);
            yield return null;
        }
        Definir(0f);
    }

    IEnumerator Trovao()
    {
        yield return new WaitForSeconds(Random.Range(atrasoTrovaoMin, atrasoTrovaoMax));
        if (fonteTrovao && trovoes != null && trovoes.Length > 0)
            fonteTrovao.PlayOneShot(trovoes[Random.Range(0, trovoes.Length)], Random.Range(0.7f, 1f));
    }

    void Definir(float k)
    {
        if (luz) luz.intensity = Mathf.Lerp(intensidadeBase, intensidadeRaio, k);
        if (ajuste != null)
            ajuste.postExposure.value = Mathf.Lerp(exposicaoBase, exposicaoRaio, k);
    }
}