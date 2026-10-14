using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light))]
public class Chuva : MonoBehaviour
{
    public float tamanho = 6f;       // tamanho de cada repetição no mundo
    public float velocidade = 0.5f;  // unidades por segundo (queda)
    public float balanco = 0.03f;    // vento lateral leve

    UniversalAdditionalLightData dados;
    float y;

    void Awake()
    {
        dados = GetComponent<Light>().GetUniversalAdditionalLightData();
        dados.lightCookieSize = new Vector2(tamanho, tamanho);
    }

    void Update()
    {
        y = Mathf.Repeat(y - velocidade * Time.deltaTime, tamanho);
        float x = Mathf.Sin(Time.time * 0.7f) * balanco;
        dados.lightCookieOffset = new Vector2(x, y);
    }
}