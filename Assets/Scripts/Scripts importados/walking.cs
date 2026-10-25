using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class walking : MonoBehaviour
{
    private CharacterController controller;

    public float speed = 5.0f;
    public float turnSpeed = 180f;

    public InputAction movement;

    public Animator animator; // arraste o Animator, ou ele é buscado nos filhos

    public bool inverterFrente = false; // marque só se a frente estiver invertida

    [Header("Resposta da animação")]
    [Tooltip("Suavização do parâmetro Speed. Menor = anda/para mais rápido. 0 = instantâneo.")]
    public float suavizacaoAnim = 0.05f;

    [Header("Giro no lugar (A/D parado)")]
    [Tooltip("Nome do ESTADO no Animator (igual ao nome do clipe).")]
    public string nomeClipeGiroEsquerda = "Left";
    public string nomeClipeGiroDireita = "Right";
    public float duracaoGiroManual = 0f; // 0 = usa a duração de cada clipe
    [Tooltip("Segundos de mistura ao entrar/repetir a animação de giro.")]
    public float misturaEntreGiros = 0.15f;
    [Tooltip("Segundos de mistura ao voltar ao Idle quando solta A/D.")]
    public float suavizacaoParada = 0.1f;
    [Tooltip("Segundos para a rotação acelerar ao apertar e frear ao soltar A/D.")]
    public float suavizacaoRotacao = 0.12f;

float velGiro, velGiroRef;   // velocidade angular atual (graus/s)

    [Header("Pivô do giro")]
    public Transform pivoGiro;   // vazio = usa o objeto do Animator (AnimaçãoPersonagem)

    Transform Pivo => pivoGiro != null ? pivoGiro : (animator != null ? animator.transform : transform);

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IdleHash = Animator.StringToHash("Idle");

    int estadoEsq, estadoDir;
    float duracaoEsq = 1f;
    float duracaoDir = 1f;

    float giroAtual;        // 0 = sem giro, -1 = esquerda, +1 = direita (A/D segurado)
    float tempoNoClipe;     // tempo desde o início da animação de giro atual
    bool girando;           // true durante Q/E (giro de ângulo fixo)

    void RodarEmTorno(float graus)
    {
        transform.RotateAround(Pivo.position, Vector3.up, graus);
        Physics.SyncTransforms();   // o Character Controller aceita a nova posição
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        estadoEsq = Animator.StringToHash(nomeClipeGiroEsquerda);
        estadoDir = Animator.StringToHash(nomeClipeGiroDireita);

        if (duracaoGiroManual > 0f) duracaoEsq = duracaoDir = duracaoGiroManual;
        else
        {
            duracaoEsq = BuscarDuracaoClipe(nomeClipeGiroEsquerda);
            duracaoDir = BuscarDuracaoClipe(nomeClipeGiroDireita);
        }

        animator.Play(IdleHash, 0, 0f);   // garante que começa no Idle
    }

    void OnEnable()  { movement.Enable(); }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log("Colidiu com: " + hit.gameObject.name, hit.gameObject);
    }

    void OnDisable()
    {
        if (movement != null) movement.Disable();
        
        giroAtual = 0f;
        girando = false;
        velGiro = 0f; // Corta qualquer rotação residual pela raiz

        // O SEGREDO ESTÁ AQUI: Força o Animator a parar instantaneamente
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
        }
    }

    void AtualizarVelocidadeAnim(float valor)
    {
        if (suavizacaoAnim <= 0f) animator.SetFloat(SpeedHash, valor);
        else animator.SetFloat(SpeedHash, valor, suavizacaoAnim, Time.deltaTime);
    }

    void Update()
    {
        Vector2 inputValor = movement.ReadValue<Vector2>();
        float x = inputValor.x;

        // Durante Q/E o personagem não anda
        if (girando)
        {
            AtualizarVelocidadeAnim(0f);
            return;
        }

        // // Q = giro de 90° à esquerda | E = giro completo (4 x 90°)
        // var teclado = Keyboard.current;
        // if (teclado != null)
        // {
        //     if (teclado.qKey.wasPressedThisFrame) { IniciarGiroFixo(1, -1f); return; }
        //     if (teclado.eKey.wasPressedThisFrame) { IniciarGiroFixo(4, -1f); return; }
        // }

        float eixoY = inverterFrente ? -inputValor.y : inputValor.y;

        // Parado + A/D = gira no lugar ENQUANTO a tecla estiver pressionada
        bool parado = Mathf.Abs(eixoY) < 0.1f;
        if (parado && Mathf.Abs(x) > 0.5f)
        {
            GirarEnquantoSegura(Mathf.Sign(x));
            return;
        }

        // Soltou A/D (ou começou a andar): para o giro
        PararGiro();

        // freio suave da rotação depois de soltar A/D
        if (parado && Mathf.Abs(velGiro) > 1f)
        {
            velGiro = Mathf.SmoothDamp(velGiro, 0f, ref velGiroRef, suavizacaoRotacao);
            RodarEmTorno(velGiro * Time.deltaTime);
        }
        else velGiro = 0f;

        // Andando: A/D curvam o personagem enquanto ele anda
        RodarEmTorno(x * turnSpeed * Time.deltaTime);

        Vector3 movDir = transform.forward * eixoY * speed;
        controller.Move(movDir * Time.deltaTime - Vector3.up * 0.1f);

        // positivo = frente (Walk), negativo = ré (Backwards), ~0 = parado (Idle)
        AtualizarVelocidadeAnim(eixoY * speed);
    }

    // sentido: -1 = esquerda, +1 = direita
    void GirarEnquantoSegura(float sentido)
    {
        float duracao = sentido < 0f ? duracaoEsq : duracaoDir;
        int estado = sentido < 0f ? estadoEsq : estadoDir;
        float ciclo = Mathf.Max(0.05f, duracao - misturaEntreGiros);

        if (sentido != giroAtual)
        {
            // começou a girar, ou trocou de lado
            giroAtual = sentido;
            tempoNoClipe = 0f;
            animator.CrossFadeInFixedTime(estado, misturaEntreGiros, 0, 0f);
        }
        else
        {
            tempoNoClipe += Time.deltaTime;
            if (tempoNoClipe >= ciclo)
            {
                // clipe quase no fim e a tecla continua segurada: repete
                tempoNoClipe = 0f;
                animator.CrossFadeInFixedTime(estado, misturaEntreGiros, 0, 0f);
            }
        }

        // 90° por duração do clipe, então o corpo gira na mesma velocidade da animação
        float alvo = sentido * (90f / duracao);
        velGiro = Mathf.SmoothDamp(velGiro, alvo, ref velGiroRef, suavizacaoRotacao);
        RodarEmTorno(velGiro * Time.deltaTime);
        AtualizarVelocidadeAnim(0f);
    }

    void PararGiro()
    {
        if (giroAtual == 0f) return;
        giroAtual = 0f;
        animator.CrossFadeInFixedTime(IdleHash, suavizacaoParada, 0, 0f);
    }

    void IniciarGiroFixo(int vezes, float sentido)
    {
        if (!girando) StartCoroutine(GirarFixo(vezes, sentido));
    }

    // Mantido público para quem chamava StartCoroutine(walking.Girar(...)) em outro script
    public IEnumerator Girar(int vezes, float sentido)
    {
        IniciarGiroFixo(vezes, sentido);
        while (girando) yield return null;
    }

    IEnumerator GirarFixo(int vezes, float sentido)
    {
        velGiro = 0f;
        girando = true;
        giroAtual = 0f;
        sentido = Mathf.Sign(sentido);

        float duracao = sentido < 0f ? duracaoEsq : duracaoDir;
        int estado = sentido < 0f ? estadoEsq : estadoDir;

        for (int i = 0; i < vezes; i++)
        {
            animator.CrossFadeInFixedTime(estado, misturaEntreGiros, 0, 0f);

            float aplicado = 0f;
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                float angulo = 90f * sentido * Mathf.Clamp01(t / duracao);
                RodarEmTorno(angulo - aplicado);
                aplicado = angulo;
                yield return null;
            }
            RodarEmTorno(90f * sentido - aplicado);   // fecha em exatamente 90°
        }

        animator.CrossFadeInFixedTime(IdleHash, suavizacaoParada, 0, 0f);
        girando = false;
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