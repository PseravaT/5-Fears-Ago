using UnityEngine;
using System.Collections.Generic;

public class InteractionDotManager : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private RectTransform uiContainer; 
    [SerializeField] private Transform player;

    [Header("Configurações de Distância")]
    [SerializeField] private float expandDotRadius = 1.5f;

    [Header("Escala do Dot (UI)")]
    [SerializeField] private float normalSize = 10f;
    [SerializeField] private float expandedSize = 25f;
    [SerializeField] private float scaleSpeed = 12f;

    private List<Interagivel> allInteractables = new List<Interagivel>();
    private Dictionary<Interagivel, RectTransform> dotInstances = new Dictionary<Interagivel, RectTransform>();

    void Start()
    {
        allInteractables.AddRange(Object.FindObjectsByType<Interagivel>());

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

            // A MÁGICA ESTÁ AQUI: Pegamos a porcentagem da câmera (0.0 até 1.0)
            Vector3 viewportPoint = currentCam.WorldToViewportPoint(interactable.transform.position);

            if (viewportPoint.z <= 0f)
            {
                dotTransform.gameObject.SetActive(false);
                continue;
            }

            dotTransform.gameObject.SetActive(true);

            // Multiplicamos essa porcentagem pelo tamanho real da sua RawImage
            Vector2 canvasPos = new Vector2(
                (viewportPoint.x * uiContainer.rect.width) - (uiContainer.rect.width * 0.5f),
                (viewportPoint.y * uiContainer.rect.height) - (uiContainer.rect.height * 0.5f)
            );

            // Movemos a bolinha para o lugar exato
            dotTransform.anchoredPosition = canvasPos;

            // Distância para expandir
            float distanceToPlayer = Vector3.Distance(player.position, interactable.transform.position);
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

    // Coloque isso no final do InteractionDotManager
    public void AlternarDots(bool mostrar)
    {
        this.enabled = mostrar; // Liga/desliga a atualização matemática
        
        // Esconde ou mostra as imagens das bolinhas
        foreach (var dot in dotInstances.Values)
        {
            if (dot != null) dot.gameObject.SetActive(mostrar);
        }
    }
}