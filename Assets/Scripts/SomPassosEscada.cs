
using UnityEngine;

public class SomPassosEscada : MonoBehaviour
{
    [Header("Sons")]
    public AudioClip[] sonsPassos;
    [Range(0f, 1f)] public float volume = 1f;
    public AudioSource audioSource;

    [Header("Cadência")]
    [Min(0.05f)]
    public float intervaloPassos = 0.45f;

    [Header("Quantidade")]
    [Min(1)]
    public int quantidadeTotalPassos = 10;

    [Header("Controle")]
    public bool tocarAutomaticamente = true;

    private float temporizador;
    private bool tocando;
    private int passosExecutados;
    private int ultimoSom = -1;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (tocarAutomaticamente)
            IniciarPassos();
    }

    private void Update()
    {
        if (!tocando || sonsPassos == null || sonsPassos.Length == 0)
            return;

        temporizador += Time.deltaTime;

        if (temporizador >= intervaloPassos)
        {
            temporizador = 0f;
            TocarPasso();

            passosExecutados++;

            if (passosExecutados >= quantidadeTotalPassos)
                PararPassos();
        }
    }

    public void IniciarPassos()
    {
        passosExecutados = 0;
        temporizador = intervaloPassos;
        tocando = true;
    }

    public void PararPassos()
    {
        tocando = false;
        temporizador = 0f;
    }

    private void TocarPasso()
    {
        if (sonsPassos.Length == 0)
            return;

        int indice = Random.Range(0, sonsPassos.Length);

        if (sonsPassos.Length > 1 && indice == ultimoSom)
            indice = (indice + 1) % sonsPassos.Length;

        ultimoSom = indice;

        audioSource.PlayOneShot(sonsPassos[indice], volume);
    }

    private void OnDisable()
    {
        tocando = false;
    }
}
