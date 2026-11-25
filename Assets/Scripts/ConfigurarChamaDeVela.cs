using UnityEngine;

/// <summary>
/// Coloque no mesmo objeto de um Particle System. Clique nos três pontinhos do componente
/// (ou botão direito no nome dele) e escolha "Configurar chama de vela": todos os módulos
/// do Particle System são ajustados de uma vez, sem você mexer em nada.
/// Depois disso o script pode ser removido.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ConfigurarChamaDeVela : MonoBehaviour
{
    [Tooltip("Tamanho máximo de cada partícula, em metros. Vela comum: 0.04 a 0.08")]
    [SerializeField] private float tamanho = 0.06f;
    [Tooltip("Partículas por segundo. Para o visual PS1, use poucas (10 a 15)")]
    [SerializeField] private int quantidade = 25;
    [Tooltip("Quanto a chama sobe, em segundos de vida das partículas")]
    [SerializeField] private float vidaMaxima = 0.7f;

    [ContextMenu("Configurar chama de vela")]
    public void Configurar()
    {
        var ps = GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMaxima * 0.6f, vidaMaxima);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanho * 0.6f, tamanho);
        main.startColor = Color.white; // a cor real vem do Color over Lifetime
        main.maxParticles = 60;

        var emissao = ps.emission;
        emissao.enabled = true;
        emissao.rateOverTime = quantidade;

        // Sai de um ponto minúsculo, apontando para cima (independe da rotação do objeto)
        var forma = ps.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 3f;
        forma.radius = 0.003f;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        // Amarelo claro na base -> laranja -> vermelho escuro, sumindo no final
        var cor = ps.colorOverLifetime;
        cor.enabled = true;
        var gradiente = new Gradient();
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.6f, 0.1f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            });
        cor.color = new ParticleSystem.MinMaxGradient(gradiente);

        // Nasce pequena, engorda um pouco e afina até sumir na ponta
        var tamanhoNaVida = ps.sizeOverLifetime;
        tamanhoNaVida.enabled = true;
        tamanhoNaVida.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0f)));

        // Ondulação leve, para a chama não subir reta
        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.15f;
        ruido.frequency = 1.5f;
        ruido.scrollSpeed = 0.8f;

        var renderizador = ps.GetComponent<ParticleSystemRenderer>();
        renderizador.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Play();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(ps);
#endif
    }
}
