using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Agenda um ou mais AudioClips para dar Play e Pause/Stop em momentos específicos (em segundos).
/// Cada entrada usa um AudioSource próprio (ou o opcional configurado) para poderem tocar juntos sem se cortar.
/// </summary>
public class AudioComTempo : MonoBehaviour
{
    [Serializable]
    public class SomAgendado
    {
        [Tooltip("Nome opcional só para identificar no Inspector.")]
        public string nome;
        public AudioClip som;
        [Range(0f, 1f)] public float volume = 1f;
        public bool repetirEnquantoToca = false;

        [Header("Momento em Segundos")]
        [Tooltip("Segundo (a partir do início) em que o áudio dá Play.")]
        public float segundoParaPlay = 0f;
        [Tooltip("Segundo (a partir do início) em que o áudio dá Pause/Stop. Deixe 0 (ou <= Play) para tocar até o fim.")]
        public float segundoParaPause = 0f;
        [Tooltip("Se marcado, usa Pause(); se desmarcado, usa Stop().")]
        public bool usarPauseEmVezDeStop = true;

        [Tooltip("Opcional: AudioSource específico. Se vazio, cria um automaticamente em tempo de execução.")]
        public AudioSource audioSource;

        [HideInInspector] public AudioSource fonteEmUso;
    }

    [SerializeField] private bool iniciarAutomaticamente = true;
    [SerializeField] private List<SomAgendado> sons = new List<SomAgendado>();

    private readonly List<Coroutine> rotinas = new List<Coroutine>();

    private void Start()
    {
        if (iniciarAutomaticamente)
            Iniciar();
    }

    public void Iniciar()
    {
        PararTudo();

        for (int i = 0; i < sons.Count; i++)
        {
            SomAgendado item = sons[i];
            if (item == null || item.som == null) continue;

            if (item.fonteEmUso == null)
            {
                item.fonteEmUso = item.audioSource != null
                    ? item.audioSource
                    : gameObject.AddComponent<AudioSource>();
                item.fonteEmUso.playOnAwake = false;
            }

            rotinas.Add(StartCoroutine(ExecutarSom(item)));
        }
    }

    public void PararTudo()
    {
        for (int i = 0; i < rotinas.Count; i++)
        {
            if (rotinas[i] != null) StopCoroutine(rotinas[i]);
        }
        rotinas.Clear();

        for (int i = 0; i < sons.Count; i++)
        {
            if (sons[i] != null && sons[i].fonteEmUso != null)
                sons[i].fonteEmUso.Stop();
        }
    }

    private IEnumerator ExecutarSom(SomAgendado item)
    {
        float playEm = Mathf.Max(0f, item.segundoParaPlay);
        if (playEm > 0f)
            yield return new WaitForSeconds(playEm);

        item.fonteEmUso.clip = item.som;
        item.fonteEmUso.volume = item.volume;
        item.fonteEmUso.loop = item.repetirEnquantoToca;
        item.fonteEmUso.Play();

        if (item.segundoParaPause > playEm)
        {
            yield return new WaitForSeconds(item.segundoParaPause - playEm);

            if (item.usarPauseEmVezDeStop)
                item.fonteEmUso.Pause();
            else
                item.fonteEmUso.Stop();
        }
    }
}
