using UnityEngine;
using System.Collections.Generic;

public class InteractionDotManager : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste o Prefab da sua Interrogação (UI) aqui")]
    [SerializeField] private GameObject dotPrefab;
    [Tooltip("Arraste a RawImage que renderiza o jogo (Filtro PS1) aqui. NÃO arraste o Canvas!")]
    [SerializeField] private RectTransform uiContainer; 
    [Tooltip("Arraste o TestPlayer aqui")]
    [SerializeField] private Transform player;

    [Header("Configurações de Distância")]
    [SerializeField] private float showDotRadius = 8f; // Distância para o ponto APARECER na tela
    [SerializeField] private float expandDotRadius = 1.5f; // Distância para o ponto EXPANDIR

    [Header("Escala do Dot (UI)")]
    [SerializeField] private float normalSize = 40f; // Tamanho normal (maior para a interrogação)
    [SerializeField] private float expandedSize = 80f; // Tamanho expandido
    [SerializeField] private float scaleSpeed = 12f;

    private List<Interagivel> allInteractables = new List<Interagivel>();
    private Dictionary<Interagivel, RectTransform> dotInstances = new Dictionary<Interagivel, RectTransform>();

    void Start()
    {
        // Busca todos os objetos interagíveis na cena
        allInteractables.AddRange(Object.FindObjectsByType<Interagivel>(FindObjectsSortMode.None));

        foreach (var interactable in allInteractables)
        {
            if (interactable.CanInteract())
            {
                CreateDotForInteractable(interactable);
            }
        }
    }

    void Update()
    {
        Camera currentCam = Camera.main; 
        
        // Trava de segurança para evitar erros caso falte alguma referência
        if (currentCam == null || player == null || uiContainer == null) return;

        foreach (var pair in dotInstances)
        {
            Interagivel interactable = pair.Key;
            RectTransform dotTransform = pair.Value;

            // Se o objeto não puder mais ser interagido (ex: já pegou a chave), esconde
            if (!interactable.CanInteract())
            {
                dotTransform.gameObject.SetActive(false);
                continue;
            }

            // 1. Calcula a distância real em 3D entre o jogador e o objeto
            float distanceToPlayer = Vector3.Distance(player.position, interactable.transform.position);

            // 2. Se estiver muito longe, esconde a interrogação e pula para o próximo
            if (distanceToPlayer > showDotRadius)
            {
                dotTransform.gameObject.SetActive(false);
                continue;
            }

            // 3. Converte a posição 3D para a Porcentagem da Tela (Viewport)
            Vector3 viewportPoint = currentCam.WorldToViewportPoint(interactable.transform.position);

            // Se o Z for menor ou igual a zero, o objeto está nas costas do jogador (esconde)
            if (viewportPoint.z <= 0f)
            {
                dotTransform.gameObject.SetActive(false);
                continue;
            }

            // Passou por todas as verificações (está perto e na frente da câmera), então mostra!
            dotTransform.gameObject.SetActive(true);

            // 4. A MÁGICA DA RAWIMAGE: 
            // Multiplica a porcentagem pelo tamanho real da RawImage e subtrai a metade para centralizar perfeitamente
            Vector2 canvasPos = new Vector2(
                (viewportPoint.x * uiContainer.rect.width) - (uiContainer.rect.width * 0.5f),
                (viewportPoint.y * uiContainer.rect.height) - (uiContainer.rect.height * 0.5f)
            );

            // Move a interrogação para o lugar exato calculado
            dotTransform.anchoredPosition = canvasPos;

            // 5. Lógica de animação de tamanho (escala)
            float targetSize = normalSize;
            if (distanceToPlayer <= expandDotRadius)
            {
                targetSize = expandedSize;
            }

            // Faz a transição suave do tamanho atual para o novo tamanho
            float currentSize = dotTransform.sizeDelta.x;
            float newSize = Mathf.Lerp(currentSize, targetSize, scaleSpeed * Time.deltaTime);
            dotTransform.sizeDelta = new Vector2(newSize, newSize);
        }
    }

    void CreateDotForInteractable(Interagivel interactable)
    {
        // Instancia a interrogação como FILHA da RawImage
        GameObject newDot = Instantiate(dotPrefab, uiContainer);
        RectTransform dotRT = newDot.GetComponent<RectTransform>();
        
        dotRT.sizeDelta = new Vector2(normalSize, normalSize);
        
        // Garante que as âncoras e o pivô da interrogação sejam exatamente o centro
        dotRT.anchorMin = new Vector2(0.5f, 0.5f);
        dotRT.anchorMax = new Vector2(0.5f, 0.5f);
        dotRT.pivot = new Vector2(0.5f, 0.5f); 
        
        dotInstances.Add(interactable, dotRT);
    }

    // Função para ligar/desligar todo o sistema via UnityEvents
    public void AlternarDots(bool mostrar)
    {
        this.enabled = mostrar; 
        
        foreach (var dot in dotInstances.Values)
        {
            if (dot != null) dot.gameObject.SetActive(mostrar);
        }
    }
}