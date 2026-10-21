using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class walking : MonoBehaviour
{
    private CharacterController controller;

    public float speed = 5.0f;
    public float turnSpeed = 180f;
    float xAtual;   // valor atual de A/D, lido todo frame

    public InputAction movement;

    public Animator animator; // arraste o Animator, ou ele é buscado nos filhos

    public bool inverterFrente = false; // marque só se a frente estiver invertida

    [Header("Resposta da animação")]
    [Tooltip("Suavização do parâmetro Speed. Menor = anda/para mais rápido. 0 = instantâneo (pode dar 'pulo' na pose).")]
    public float suavizacaoAnim = 0.05f;

    [Header("Giro de 90° (clipe Left)")]
    public string nomeClipeGiro = "Left";
    public float duracaoGiroManual = 0f; // 0 = usa a duração do clipe automaticamente
    [Tooltip("Segundos antes do fim de um giro em que o próximo já é disparado, para os giros emendarem sem passar pelo Idle.")]
    public float misturaEntreGiros = 0.15f;
    [Tooltip("Quantos giros por aperto podem ficar esperando na fila.")]
    public int maxGirosNaFila = 3;

    [Header("Pivô do giro")]
    public Transform pivoGiro;   // vazio = usa o objeto do Animator (AnimaçãoPersonagem)

    Transform Pivo => pivoGiro != null ? pivoGiro : (animator != null ? animator.transform : transform);

    void RodarEmTorno(float graus)
    {
        transform.RotateAround(Pivo.position, Vector3.up, graus);
        Physics.SyncTransforms();   // o Character Controller aceita a nova posição
    }

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int TurnLeftHash = Animator.StringToHash("TurnLeft");
    static readonly int TurnRightHash = Animator.StringToHash("TurnRight");

    bool girando;
    bool emFinalDeGiro;                 // true nos últimos instantes de um giro (hora de emendar o próximo)
    float duracaoGiro = 1f;
    float xAnterior;                    // para detectar um aperto novo de A/D
    readonly Queue<float> filaGiros = new Queue<float>();   // sentido de cada giro pendente (-1 / +1)
    Coroutine rotinaGiro;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        duracaoGiro = duracaoGiroManual > 0f ? duracaoGiroManual : BuscarDuracaoClipe(nomeClipeGiro);
    }

    void OnEnable()  { movement.Enable(); }

    void OnDisable()
    {
        movement.Disable();
        // Se o objeto for desligado no meio de um giro, não deixa o estado "travado"
        filaGiros.Clear();
        girando = false;
        emFinalDeGiro = false;
        rotinaGiro = null;
    }

    void AtualizarVelocidadeAnim(float valor)
    {
        if (suavizacaoAnim <= 0f) animator.SetFloat(SpeedHash, valor);
        else animator.SetFloat(SpeedHash, valor, suavizacaoAnim, Time.deltaTime);
    }

    void Update()
    {
        Vector2 inputValor = movement.ReadValue<Vector2>();

        // Detecta um aperto NOVO de A/D (vale também durante um giro, para enfileirar o próximo)
        float x = inputValor.x;
        xAtual = x;
        bool apertouLado = Mathf.Abs(x) > 0.5f &&
                           (Mathf.Abs(xAnterior) <= 0.5f || Mathf.Sign(x) != Mathf.Sign(xAnterior));
        xAnterior = x;

        // Durante o giro, o personagem não anda
        if (girando)
        {
            AtualizarVelocidadeAnim(0f);

            // Aperto novo = mais um giro na fila (segurar é tratado dentro da coroutine)
            if (apertouLado && filaGiros.Count < maxGirosNaFila)
                EnfileirarGiro(1, Mathf.Sign(x));
            return;
        }

        // Q = giro de 90° à esquerda | E = giro completo (4 x 90°)
        var teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.qKey.wasPressedThisFrame) { EnfileirarGiro(1, -1f); return; }
            if (teclado.eKey.wasPressedThisFrame) { EnfileirarGiro(4, -1f); return; }
        }

        float eixoY = inverterFrente ? -inputValor.y : inputValor.y;

        // Parado + A/D = giro de 90° no lugar, com animação
        bool parado = Mathf.Abs(eixoY) < 0.1f;
        if (parado && Mathf.Abs(x) > 0.5f)
        {
            EnfileirarGiro(1, Mathf.Sign(x));
            return;
        }

        // Andando: A/D curvam o personagem enquanto ele anda
        RodarEmTorno(x * turnSpeed * Time.deltaTime);

        Vector3 movDir = transform.forward * eixoY * speed;
        controller.Move(movDir * Time.deltaTime - Vector3.up * 0.1f);

        // positivo = frente (Walk), negativo = ré (Backwards), ~0 = parado (Idle)
        AtualizarVelocidadeAnim(eixoY * speed);
    }

    // sentido: -1 = esquerda, +1 = direita
    void EnfileirarGiro(int vezes, float sentido)
    {
        for (int i = 0; i < vezes; i++) filaGiros.Enqueue(Mathf.Sign(sentido));
        if (rotinaGiro == null) rotinaGiro = StartCoroutine(ProcessarGiros());
    }

    // Mantido público para quem chamava StartCoroutine(walking.Girar(...)) em outro script
    public IEnumerator Girar(int vezes, float sentido)
    {
        EnfileirarGiro(vezes, sentido);
        while (girando) yield return null;
    }

    IEnumerator ProcessarGiros()
    {
        girando = true;
        bool gatilhoJaEnviado = false;

        while (filaGiros.Count > 0)
        {
            float sentido = filaGiros.Dequeue();
            float porGiro = 90f * Mathf.Sign(sentido);

            // Se o gatilho já foi disparado antes do fim do giro anterior,
            // a animação já está adiantada em "misturaEntreGiros" segundos
            bool encadeado = gatilhoJaEnviado;
            if (!encadeado)
                animator.SetTrigger(sentido < 0f ? TurnLeftHash : TurnRightHash);
            gatilhoJaEnviado = false;
            emFinalDeGiro = false;

            float inicio = encadeado ? misturaEntreGiros : 0f;
            float aplicado = 0f;

            for (float t = inicio; t < duracaoGiro; t += Time.deltaTime)
            {
                if (t >= duracaoGiro - misturaEntreGiros)
                {
                    emFinalDeGiro = true;

                    // Tecla ainda segurada e nada na fila: emenda mais um giro
                    if (filaGiros.Count == 0 && Mathf.Abs(xAtual) > 0.5f)
                        filaGiros.Enqueue(Mathf.Sign(xAtual));

                    // Dispara o próximo antes do fim, para emendar sem voltar ao Idle
                    if (!gatilhoJaEnviado && filaGiros.Count > 0)
                    {
                        animator.SetTrigger(filaGiros.Peek() < 0f ? TurnLeftHash : TurnRightHash);
                        gatilhoJaEnviado = true;
                    }
                }

                float angulo = porGiro * Mathf.Clamp01((t - inicio) / (duracaoGiro - inicio));
                RodarEmTorno(angulo - aplicado);
                aplicado = angulo;
                yield return null;
            }
            RodarEmTorno(porGiro - aplicado);   // fecha em exatamente 90°
        }

        emFinalDeGiro = false;
        girando = false;
        rotinaGiro = null;
    }

    float BuscarDuracaoClipe(string nome)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 1f;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == nome) return clip.length;

        Debug.LogWarning($"Clipe '{nome}' não encontrado no Animator.");
        return 1f;
    }
}