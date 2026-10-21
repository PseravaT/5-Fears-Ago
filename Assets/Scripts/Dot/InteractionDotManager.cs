using UnityEngine;
using System.Collections.Generic;

public class InteractionDotManager : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private RectTransform uiContainer; 
    [SerializeField] private Transform player;

    [Header("Configurações de Distância")]
    [SerializeField] private float showDotRadius = 8f; // Distância para o ponto APARECER na tela
    [SerializeField] private float expandDotRadius = 1.5f; // Distância para o ponto EXPANDIR

    [Header("Escala do Dot (UI)")]
    [SerializeField] private float normalSize = 10f;
    [SerializeField] private float expandedSize = 25f;
    [SerializeField] private float scaleSpeed = 12f;

    private List<Interagivel> allInteractables = new List<Interagivel>();
    private Dictionary<Interagivel, RectTransform> dotInstances = new Dictionary<Interagivel, RectTransform>();

    void Start()
    {
        // Se estiver usando uma versão mais antiga do Unity onde FindObjectsByType não existe, 
        // mude para FindObjectsOfType<Interagivel>()
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
        if (currentCam == null || player == null) return;

        foreach (var pair in dotInstances)
        {
            Interagivel interactable = pair.Key;
            RectTransform dotTransform = pair.Value;

            if (!interactable.CanInteract())
            {
                dotTransform.gameObject.SetActive(false);
                continue;
            }

            // 1. Calcula a distância entre o jogador e o objeto primeiro
            float distanceToPlayer = Vector3.Distance(player.position, interactable.transform.position);

            // 2. Se estiver mais longe que o raio de visão configurado, esconde a bolinha e pula para o próximo
            if (distanceToPlayer > showDotRadius)
            {
                dotTransform.gameObject.SetActive(false);
                continue;
            }

            // 3. A MÁGICA ESTÁ AQUI: Pegamos a porcentagem da câmera (0.0 até 1.0)
            Vector3 viewportPoint = currentCam.WorldToViewportPoint(interactable.transform.position);

            // Se estiver atrás da câmera, esconde
            if (viewportPoint.z <= 0f)
            {
                dotTransform.gameObject.SetActive(false);
                continue;
            }

            // Se passou por todas as verificações (está perto e na frente da câmera), mostra a bolinha
            dotTransform.gameObject.SetActive(true);

            // Multiplicamos essa porcentagem pelo tamanho real da sua RawImage
            Vector2 canvasPos = new Vector2(
                (viewportPoint.x * uiContainer.rect.width) - (uiContainer.rect.width * 0.5f),
                (viewportPoint.y * uiContainer.rect.height) - (uiContainer.rect.height * 0.5f)
            );

            // Movemos a bolinha para o lugar exato
            dotTransform.anchoredPosition = canvasPos;

            // 4. Lógica de expandir o tamanho se estiver muito perto
            float targetSize = normalSize;
            
            if (distanceToPlayer <= expandDotRadius)
            {
                targetSize = expandedSize;
            }

            // Animação do tamanho
            float currentSize = dotTransform.sizeDelta.x;
            float newSize = Mathf.Lerp(currentSize, targetSize, scaleSpeed * Time.deltaTime);
            dotTransform.sizeDelta = new Vector2(newSize, newSize);
        }
    }

    void CreateDotForInteractable(Interagivel interactable)
    {
        // Instancia os pontos como filhos da RawImage
        GameObject newDot = Instantiate(dotPrefab, uiContainer);
        RectTransform dotRT = newDot.GetComponent<RectTransform>();
        
        dotRT.sizeDelta = new Vector2(normalSize, normalSize);
        
        // Garante que a âncora do ponto seja o centro da RawImage
        dotRT.anchorMin = new Vector2(0.5f, 0.5f);
        dotRT.anchorMax = new Vector2(0.5f, 0.5f);
        
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