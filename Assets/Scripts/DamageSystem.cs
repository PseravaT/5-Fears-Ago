using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Configuracao de UM nivel de dano (veias + audio do batimento + musica ambiente)
[System.Serializable]
public class DamageStage
{
    public Color color = new Color(0.6f, 0.03f, 0.05f, 1f);
    [Tooltip("Veias por altura de tela")]
    [Range(4f, 30f)] public float density = 12f;
    [Tooltip("Espessura na raiz")]
    [Range(0.01f, 0.2f)] public float width = 0.06f;
    [Tooltip("Ate onde as veias chegam, em alturas de tela (0.5 = metade da altura)")]
    [Range(0f, 0.9f)] public float reach = 0.4f;
    [Tooltip("Velocidade da animacao: quadros por segundo")]
    [Range(1f, 30f)] public float fps = 10f;
    [Tooltip("Quantos desenhos diferentes antes de repetir")]
    [Range(2, 8)] public int frames = 4;
    [Tooltip("O quanto um quadro difere do outro (0 = parado)")]
    [Range(0f, 1f)] public float boil = 0.8f;
    [Tooltip("Textura de giz/carvao nas veias: bordas esfareladas e falhas do pincel (0 = liso)")]
    [Range(0f, 1f)] public float veinRough = 0.5f;

    [Header("Contorno (rabisco)")]
    public Color outlineColor = new Color(0.03f, 0f, 0.01f, 1f);
    [Tooltip("Espessura do contorno em volta da veia (0 = sem contorno)")]
    [Range(0f, 0.08f)] public float outlineWidth = 0.03f;
    [Tooltip("O quanto o contorno se desalinha da veia e treme entre quadros (0 = colado, 1 = bem rabiscado)")]
    [Range(0f, 1f)] public float outlineScribble = 0.7f;
    [Tooltip("Textura de giz/carvao no contorno (0 = liso)")]
    [Range(0f, 1f)] public float outlineRough = 0.5f;

    [Header("Rabiscos grossos (giz / carvao)")]
    public Color brushColor = new Color(0.02f, 0.02f, 0.02f, 1f);
    [Tooltip("Ate onde os rabiscos chegam, em alturas de tela (0 = sem rabiscos neste dano)")]
    [Range(0f, 0.6f)] public float brushReach = 0.15f;
    [Tooltip("Espessura dos tracos")]
    [Range(0.05f, 0.8f)] public float brushThickness = 0.22f;
    [Tooltip("Quantos tracos por altura de tela")]
    [Range(3f, 16f)] public float brushDensity = 9f;
    [Tooltip("Borda esfarelada e listras de pincel seco (0 = traco liso)")]
    [Range(0f, 1f)] public float brushRough = 0.6f;

    [Header("Batimento")]
    [Tooltip("Segundos entre um batimento e o proximo (menor = mais rapido). Vale pra borda e pro brilho das veias")]
    [Range(0.25f, 3f)] public float pulseInterval = 1.2f;

    [Header("Audio do batimento")]
    [Tooltip("Velocidade do audio (1 = normal). Acelera o batimento e deixa mais agudo")]
    [Range(0.5f, 3f)] public float audioSpeed = 1f;
    [Tooltip("Volume do batimento neste dano")]
    [Range(0f, 1f)] public float audioVolume = 0.8f;

    [Header("Musica ambiente (loop)")]
    [Tooltip("Musica em loop enquanto o jogador esta NESTE dano. Vazio = sem musica neste dano")]
    public AudioClip musicClip;
    [Tooltip("Volume da musica neste dano")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Tooltip("Segundos de silencio antes da musica deste dano comecar a tocar (a musica anterior some na hora). 0 = comeca ja")]
    [Min(0f)] public float musicDelay = 0f;
}

public class DamageSystem : MonoBehaviour
{
    public static DamageSystem Instancia { get; private set; }

    [Header("Referencias")]
    [Tooltip("Mesmo material usado no Pass Material da Full Screen Pass Renderer Feature")]
    public Material crtMaterial;
    [Tooltip("Opcional: outros materiais CRT de outras cenas (ex: Cozinha). O script tambem sincroniza automaticamente todos os materiais Custom/CRTFilterURP carregados.")]
    public Material[] outrosMateriaisCRT;
    [Tooltip("Se ligado, mantem o DamageManager vivo ao trocar de cena (Sala <-> Cozinha) para nao perder o dano, o batimento e a musica")]
    public bool persistirEntreCenas = true;

    [Header("Dano")]
    [Tooltip("Hits ate perder. Com 3: padrao (0) > amarelo (1) > vermelho (2) > preto (3 = perdeu)")]
    public int maxHits = 3;

    [Header("Cores de dano (sem dano = visual padrao do shader, sem tom)")]
    public Color[] stageColors =
    {
        new Color(1.00f, 0.85f, 0.20f), // amarelo
        new Color(1.00f, 0.15f, 0.10f)  // vermelho
    };

    [Header("Config por dano (veias + audio): 1 item por dano (Element 0 = 1o dano, Element 1 = 2o, ...)")]
    [Tooltip("Se tomar mais dano que itens na lista, repete o ultimo")]
    [FormerlySerializedAs("veinStages")]
    public DamageStage[] stages =
    {
        new DamageStage { reach = 0.20f, density = 8f, width = 0.10f, fps = 4f,  frames = 4, boil = 0.4f, outlineWidth = 0.02f, brushReach = 0.12f, brushThickness = 0.18f, brushRough = 0.5f, veinRough = 0.5f, outlineRough = 0.5f, pulseInterval = 1.2f, audioSpeed = 1.0f, audioVolume = 0.5f },
        new DamageStage { reach = 0.35f, density = 8f, width = 0.12f, fps = 8f,  frames = 5, boil = 0.7f, outlineWidth = 0.03f, brushReach = 0.24f, brushThickness = 0.22f, brushRough = 0.6f, veinRough = 0.6f, outlineRough = 0.6f, pulseInterval = 0.8f, audioSpeed = 1.3f, audioVolume = 0.7f },
        new DamageStage { reach = 0.55f, density = 8f, width = 0.14f, fps = 14f, frames = 6, boil = 1.0f, outlineWidth = 0.045f, brushReach = 0.38f, brushThickness = 0.28f, brushRough = 0.7f, veinRough = 0.7f, outlineRough = 0.7f, pulseInterval = 0.5f, audioSpeed = 1.7f, audioVolume = 0.9f }
    };

    [Header("Tempos (segundos)")]
    [Tooltip("Tempo pra o nivel de dano (batimento e borda) acompanhar o novo valor")]
    public float levelSmoothTime = 0.6f;
    [Tooltip("Tempo pra a cor do ambiente trocar de um estagio pro outro")]
    public float colorTransitionTime = 0.8f;
    [Tooltip("Tempo pra veias, batimento e audio mudarem ate os valores do novo dano")]
    public float veinTransitionTime = 0.8f;
    [Tooltip("Duracao do flash de impacto")]
    public float flashDuration = 0.5f;
    [Tooltip("Tempo pra tela ir a preto quando o jogador perde")]
    public float blackoutTime = 2f;

    [Header("Audio do batimento")]
    [Tooltip("Instante (s) do clip em que o 1o toque do batimento comeca. No coracao_1s_loop.wav = 0.074")]
    public float beatOffsetSeconds = 0.074f;
    [Tooltip("Ajuste fino em segundos: positivo = visual atrasa, negativo = visual adianta")]
    public float syncTrim = 0f;
    [Tooltip("AudioSource com o som de batimento. O script liga o Loop e controla volume e velocidade")]
    public AudioSource heartbeatSource;
    [Tooltip("Ao perder, o batimento some (silencio)")]
    public bool silenceOnDeath = true;
    [Tooltip("Duracao de UM batimento no seu clip, em segundos (0 = desligado). Se maior que 0, a velocidade do audio acompanha o Pulse Interval de cada dano e o Audio Speed e ignorado")]
    public float clipBeatSeconds = 0f;

    [Header("Musica ambiente")]
    [Tooltip("Musica enquanto NAO ha dano. Vazio = silencio. A musica de cada dano fica na lista Stages")]
    public AudioClip idleMusicClip;
    [Range(0f, 1f)] public float idleMusicVolume = 0.5f;
    [Tooltip("Segundos de espera antes da musica sem dano comecar a tocar (no inicio da cena e ao curar/resetar)")]
    [Min(0f)] public float idleMusicDelay = 0f;
    [Tooltip("Segundos para uma musica sair e a outra entrar (crossfade)")]
    public float musicFadeTime = 1.5f;
    [Tooltip("Ao perder, a musica some")]
    public bool silenceMusicOnDeath = true;

    [Header("Atraso dos efeitos (o som de dano toca primeiro)")]
    [Tooltip("Se ligado, rabiscos, cor, flash, batimento e musica so comecam quando o som de dano termina")]
    public bool waitForDamageSound = true;
    [Tooltip("Segundos somados a duracao do som. Negativo = os efeitos comecam um pouco antes do som acabar. Sem som de dano, vale so este valor (minimo 0)")]
    public float damageEffectOffset = 0f;

    [Header("Sons (dano e game over)")]
    [Tooltip("Opcional: AudioSource para os sons pontuais. Vazio = o script cria um sozinho. Nao use o mesmo do batimento")]
    public AudioSource sfxSource;
    [Tooltip("Som de dano: toca ao tomar qualquer dano, antes dos efeitos comecarem")]
    public AudioClip damageClip;
    [Range(0f, 1f)] public float damageVolume = 1f;
    [Tooltip("Som de game over, tocado quando a tela termina de ficar preta")]
    public AudioClip gameOverClip;
    [Range(0f, 1f)] public float gameOverVolume = 1f;
    [Tooltip("Segundos de tela preta em silencio antes do som de game over")]
    public float gameOverDelay = 0.5f;

    [Header("Eventos")]
    public UnityEvent onDamage;
    public UnityEvent onDeath;
    [Tooltip("Disparado na tela preta, junto com o som de game over (use pra mostrar menu, reiniciar etc.)")]
    public UnityEvent onGameOver;

    [Header("Teste (so no Editor / Development Build)")]
    [Tooltip("T = tomar 1 dano | R = resetar")]
    public bool enableDebugKeys = true;

    int hits;
    float currentLevel;
    float levelVelocity;
    float flash;
    float phase;
    float darkness;
    Color currentColor;

    // valores atuais das veias (os que mudam suave e os que mudam de uma vez)
    Color veinColor = new Color(0.6f, 0.03f, 0.05f, 1f);
    float veinReach;
    float veinWidth = 0.06f;
    float veinDensity = 12f;
    float veinFps = 10f;
    float veinFrames = 4f;
    float veinBoil = 0.8f;
    Color veinOutlineColor = new Color(0.03f, 0f, 0.01f, 1f);
    float veinOutline;
    float veinScribble = 0.7f;
    Color brushColorNow = new Color(0.02f, 0.02f, 0.02f, 1f);
    float brushReachNow;
    float brushThicknessNow = 0.22f;
    float brushDensityNow = 9f;
    float brushRoughNow = 0.6f;
    float veinRoughNow = 0.5f;
    float outlineRoughNow = 0.5f;

    float heartVolume;
    float heartSpeed = 1f;
    float pulseIntervalNow = 1.2f;
    bool gameOverPlayed;
    float blackTimer;

    // hits = dano real (logica do jogo, muda na hora)
    // shownHits = dano que os efeitos ja mostram (so muda depois do som de dano)
    int shownHits;
    bool effectsPending;
    float effectsTimer;

    // musica ambiente: 2 fontes para fazer crossfade entre musicas
    AudioSource[] musicSrc = new AudioSource[2];
    float[] musicVol = new float[2];
    int musicActive = -1;          // fonte com a musica atual (-1 = nenhuma)
    AudioClip musicCurrentClip;
    bool musicPending;             // musica nova esperando o delay acabar
    float musicDelayTimer;

    // Lista de todos os materiais CRT ativos (Sala, Cozinha, etc.) para manter o efeito visual em todas as cenas
    readonly List<Material> materiaisCRTAtivos = new List<Material>();

    static readonly int DamageLevelId = Shader.PropertyToID("_DamageLevel");
    static readonly int PulsePhaseId = Shader.PropertyToID("_PulsePhase");
    static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
    static readonly int DamageColorId = Shader.PropertyToID("_DamageColor");
    static readonly int DamageDarknessId = Shader.PropertyToID("_DamageDarkness");
    static readonly int VeinColorId = Shader.PropertyToID("_VeinColor");
    static readonly int VeinDensityId = Shader.PropertyToID("_VeinDensity");
    static readonly int VeinWidthId = Shader.PropertyToID("_VeinWidth");
    static readonly int VeinReachId = Shader.PropertyToID("_VeinReach");
    static readonly int VeinFpsId = Shader.PropertyToID("_VeinFps");
    static readonly int VeinFramesId = Shader.PropertyToID("_VeinFrames");
    static readonly int VeinBoilId = Shader.PropertyToID("_VeinBoil");
    static readonly int VeinOutlineColorId = Shader.PropertyToID("_VeinOutlineColor");
    static readonly int VeinOutlineId = Shader.PropertyToID("_VeinOutline");
    static readonly int VeinOutlineJitterId = Shader.PropertyToID("_VeinOutlineJitter");
    static readonly int BrushColorId = Shader.PropertyToID("_BrushColor");
    static readonly int BrushReachId = Shader.PropertyToID("_BrushReach");
    static readonly int BrushThicknessId = Shader.PropertyToID("_BrushThickness");
    static readonly int BrushDensityId = Shader.PropertyToID("_BrushDensity");
    static readonly int BrushRoughId = Shader.PropertyToID("_BrushRough");
    static readonly int VeinRoughId = Shader.PropertyToID("_VeinRough");
    static readonly int VeinOutlineRoughId = Shader.PropertyToID("_VeinOutlineRough");

    public int Hits => hits;
    public bool IsDead => hits >= maxHits;
    public int ShownHits => shownHits;
    bool ShownDead => shownHits >= maxHits;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        if (persistirEntreCenas && transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    // Chame isso quando o jogador errar um puzzle
    public void TakeDamage(int amount = 1)
    {
        if (IsDead) return;

        hits = Mathf.Min(hits + amount, maxHits);
        SistemaDeVida.SincronizarPeloDamageSystem(hits, maxHits);

        // 1) o som de dano toca na hora
        AudioClip clip = damageClip;
        if (clip != null)
            GetSfx().PlayOneShot(clip, damageVolume);

        // 2) os efeitos (rabiscos, cor, flash, batimento, musica) comecam depois do som
        float delay = damageEffectOffset;
        if (waitForDamageSound && clip != null)
            delay += clip.length;
        effectsTimer = Mathf.Max(0f, delay);
        if (effectsTimer <= 0f)
            ApplyShownDamage();
        else
            effectsPending = true;

        onDamage?.Invoke();

        if (IsDead)
            onDeath?.Invoke();
    }

    public void Heal(int amount = 1)
    {
        hits = Mathf.Max(hits - amount, 0);
        shownHits = hits;
        effectsPending = false;
        SistemaDeVida.SincronizarPeloDamageSystem(hits, maxHits);
    }

    public void ResetDamage()
    {
        hits = 0;
        shownHits = 0;
        effectsPending = false;
        SistemaDeVida.SincronizarPeloDamageSystem(hits, maxHits);
    }

    // Permite que o SistemaDeVida defina diretamente o estagio de dano (com ou sem som de impacto)
    public void SetDamageStage(int targetHits, bool playEffects = true)
    {
        targetHits = Mathf.Clamp(targetHits, 0, maxHits);
        if (targetHits == hits) return;

        if (targetHits == 0)
        {
            ResetDamage();
        }
        else if (targetHits > hits && playEffects)
        {
            TakeDamage(targetHits - hits);
        }
        else
        {
            hits = targetHits;
            shownHits = hits;
            effectsPending = false;
            SistemaDeVida.SincronizarPeloDamageSystem(hits, maxHits);
        }
    }

    // Chamado quando o som de dano acaba: agora os efeitos mostram o dano novo
    void ApplyShownDamage()
    {
        effectsPending = false;
        shownHits = hits;
        flash = 1f;
    }

    // Fase do pulso (0..1) lida direto da posicao do audio: o visual nunca se desencontra do som
    bool AudioPhase(out float p)
    {
        p = 0f;
        if (heartbeatSource == null || clipBeatSeconds <= 0f) return false;
        if (!heartbeatSource.isPlaying || heartbeatSource.clip == null) return false;
        float t = (float)heartbeatSource.timeSamples / heartbeatSource.clip.frequency; // posicao no clip, em s
        p = Mathf.Repeat((t - beatOffsetSeconds - syncTrim) / clipBeatSeconds, 1f);
        return true;
    }

    [ContextMenu("Testar: tomar 1 dano")]
    void TestDamage() => TakeDamage();

    [ContextMenu("Testar: resetar dano")]
    void TestReset() => ResetDamage();

    // Indice em stageColors para hits >= 1 (hits = 0 nao usa cor)
    int StageIndex()
    {
        if (stageColors == null || stageColors.Length == 0) return 0;
        float t = (float)(shownHits - 1) / Mathf.Max(1, maxHits - 1);
        return Mathf.Clamp(Mathf.FloorToInt(t * stageColors.Length), 0, stageColors.Length - 1);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AtualizarListaMateriaisCRT();

        // Garante que existe um componente SistemaDeVida ativo junto ao DamageSystem
        if (SistemaDeVida.Instancia == null && GetComponent<SistemaDeVida>() == null)
        {
            gameObject.AddComponent<SistemaDeVida>();
        }

        currentColor = Color.clear;
        veinReach = 0f;
        brushReachNow = 0f;
        heartVolume = 0f;
        shownHits = hits;
        effectsPending = false;
        musicActive = -1;
        musicCurrentClip = null;
        musicPending = false;
        musicDelayTimer = 0f;
        musicVol[0] = musicVol[1] = 0f;
        if (heartbeatSource != null)
        {
            heartbeatSource.loop = true;
            heartbeatSource.playOnAwake = false;
            heartbeatSource.volume = 0f;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
        {
            ResetDamage();
        }
        AtualizarListaMateriaisCRT();
    }

    void AtualizarListaMateriaisCRT()
    {
        materiaisCRTAtivos.Clear();
        if (crtMaterial != null)
            materiaisCRTAtivos.Add(crtMaterial);

        if (outrosMateriaisCRT != null)
        {
            foreach (var mat in outrosMateriaisCRT)
            {
                if (mat != null && !materiaisCRTAtivos.Contains(mat))
                    materiaisCRTAtivos.Add(mat);
            }
        }

        // Busca automaticamente todos os materiais CRT carregados (ex: Cozinha, Fora, Casa) exceto o do Menu
        var todosMateriais = Resources.FindObjectsOfTypeAll<Material>();
        foreach (var mat in todosMateriais)
        {
            if (mat != null && mat.shader != null &&
                mat.shader.name == "Custom/CRTFilterURP" &&
                !mat.name.Contains("Menu") &&
                !materiaisCRTAtivos.Contains(mat))
            {
                materiaisCRTAtivos.Add(mat);
            }
        }
    }

    void SetCrtFloat(int id, float val)
    {
        for (int i = 0; i < materiaisCRTAtivos.Count; i++)
        {
            if (materiaisCRTAtivos[i] != null)
                materiaisCRTAtivos[i].SetFloat(id, val);
        }
    }

    void SetCrtColor(int id, Color val)
    {
        for (int i = 0; i < materiaisCRTAtivos.Count; i++)
        {
            if (materiaisCRTAtivos[i] != null)
                materiaisCRTAtivos[i].SetColor(id, val);
        }
    }

    void DebugKeys()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!enableDebugKeys) return;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.tKey.wasPressedThisFrame) TakeDamage();
        if (kb.rKey.wasPressedThisFrame) ResetDamage();
#else
        if (Input.GetKeyDown(KeyCode.T)) TakeDamage();
        if (Input.GetKeyDown(KeyCode.R)) ResetDamage();
#endif
#endif
    }

    void UpdateVeins()
    {
        float k = 1f - Mathf.Exp(-3f * Time.deltaTime / Mathf.Max(0.01f, veinTransitionTime));
        float targetReach = 0f;
        float targetBrushReach = 0f;

        if (shownHits > 0 && stages != null && stages.Length > 0)
        {
            DamageStage st = stages[Mathf.Clamp(shownHits - 1, 0, stages.Length - 1)];
            targetReach = st.reach;
            targetBrushReach = st.brushReach;

            // veias invisiveis ate aqui: ja comecam com a cor e a espessura certas
            if (veinReach < 0.001f)
            {
                veinColor = st.color;
                veinWidth = st.width;
                veinOutlineColor = st.outlineColor;
                veinOutline = st.outlineWidth;
            }

            // crescem/mudam suave
            veinColor = Color.Lerp(veinColor, st.color, k);
            veinWidth = Mathf.Lerp(veinWidth, st.width, k);
            veinOutlineColor = Color.Lerp(veinOutlineColor, st.outlineColor, k);
            veinOutline = Mathf.Lerp(veinOutline, st.outlineWidth, k);
            veinRoughNow = Mathf.Lerp(veinRoughNow, st.veinRough, k);
            outlineRoughNow = Mathf.Lerp(outlineRoughNow, st.outlineRough, k);

            // mudam de uma vez, no momento do dano (o flash disfarca a troca)
            veinDensity = st.density;
            veinFps = st.fps;
            veinFrames = st.frames;
            veinBoil = st.boil;
            veinScribble = st.outlineScribble;

            // rabiscos grossos: invisiveis ate aqui, entao ja comecam com cor e espessura certas
            if (brushReachNow < 0.001f)
            {
                brushColorNow = st.brushColor;
                brushThicknessNow = st.brushThickness;
            }
            brushColorNow = Color.Lerp(brushColorNow, st.brushColor, k);
            brushThicknessNow = Mathf.Lerp(brushThicknessNow, st.brushThickness, k);
            brushDensityNow = st.brushDensity;   // muda de uma vez, no dano
            brushRoughNow = st.brushRough;
        }

        veinReach = Mathf.Lerp(veinReach, targetReach, k);
        brushReachNow = Mathf.Lerp(brushReachNow, targetBrushReach, k);

        SetCrtColor(VeinColorId, veinColor);
        SetCrtFloat(VeinDensityId, veinDensity);
        SetCrtFloat(VeinWidthId, veinWidth);
        SetCrtFloat(VeinReachId, veinReach);
        SetCrtFloat(VeinFpsId, veinFps);
        SetCrtFloat(VeinFramesId, veinFrames);
        SetCrtFloat(VeinBoilId, veinBoil);
        SetCrtColor(VeinOutlineColorId, veinOutlineColor);
        SetCrtFloat(VeinOutlineId, veinOutline);
        SetCrtFloat(VeinOutlineJitterId, veinScribble);
        SetCrtColor(BrushColorId, brushColorNow);
        SetCrtFloat(BrushReachId, brushReachNow);
        SetCrtFloat(BrushThicknessId, brushThicknessNow);
        SetCrtFloat(BrushDensityId, brushDensityNow);
        SetCrtFloat(BrushRoughId, brushRoughNow);
        SetCrtFloat(VeinRoughId, veinRoughNow);
        SetCrtFloat(VeinOutlineRoughId, outlineRoughNow);
    }

    AudioSource GetSfx()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }
        return sfxSource;
    }

    void UpdateAudio()
    {
        if (heartbeatSource == null) return;

        float k = 1f - Mathf.Exp(-3f * Time.deltaTime / Mathf.Max(0.01f, veinTransitionTime));
        float targetVolume = 0f;

        if (shownHits > 0 && stages != null && stages.Length > 0)
        {
            DamageStage st = stages[Mathf.Clamp(shownHits - 1, 0, stages.Length - 1)];
            float targetSpeed = clipBeatSeconds > 0f
                ? clipBeatSeconds / Mathf.Max(0.05f, st.pulseInterval)
                : st.audioSpeed;
            targetSpeed = Mathf.Clamp(targetSpeed, 0.1f, 3f);
            targetVolume = st.audioVolume;

            // som ainda inaudivel: ja comeca na velocidade certa
            if (heartVolume < 0.001f)
                heartSpeed = targetSpeed;
            heartSpeed = Mathf.Lerp(heartSpeed, targetSpeed, k);
        }

        if (ShownDead && silenceOnDeath)
            targetVolume = 0f;

        heartVolume = Mathf.Lerp(heartVolume, targetVolume, k);
        heartbeatSource.volume = heartVolume;
        heartbeatSource.pitch = heartSpeed;

        bool audible = heartVolume > 0.002f;
        if (audible && !heartbeatSource.isPlaying)
            heartbeatSource.Play();
        else if (!audible && heartbeatSource.isPlaying)
            heartbeatSource.Stop();
    }

    AudioSource GetMusicSource(int i)
    {
        if (musicSrc[i] == null)
        {
            musicSrc[i] = gameObject.AddComponent<AudioSource>();
            musicSrc[i].playOnAwake = false;
            musicSrc[i].loop = true;
            musicSrc[i].spatialBlend = 0f;
            musicSrc[i].volume = 0f;
        }
        return musicSrc[i];
    }

    void UpdateMusic()
    {
        AudioClip targetClip = null;
        float targetVol = 0f;
        float targetDelay = 0f;

        if (shownHits <= 0)
        {
            targetClip = idleMusicClip;
            targetVol = idleMusicVolume;
            targetDelay = idleMusicDelay;
        }
        else if (stages != null && stages.Length > 0)
        {
            DamageStage st = stages[Mathf.Clamp(shownHits - 1, 0, stages.Length - 1)];
            targetClip = st.musicClip;
            targetVol = st.musicVolume;
            targetDelay = st.musicDelay;
        }

        if (ShownDead && silenceMusicOnDeath)
            targetVol = 0f;

        // musica diferente da atual: a antiga some na hora, a nova espera o delay e entra
        if (targetClip != musicCurrentClip)
        {
            musicCurrentClip = targetClip;
            if (targetClip != null)
            {
                // se ainda estava esperando outro delay, reaproveita a mesma fonte (ainda muda)
                int next = musicPending ? musicActive : (musicActive < 0 ? 0 : 1 - musicActive);
                AudioSource src = GetMusicSource(next);
                src.Stop();
                src.clip = targetClip;
                musicVol[next] = 0f;
                src.volume = 0f;
                musicActive = next;
                musicPending = true;
                musicDelayTimer = Mathf.Max(0f, targetDelay);
            }
            else
            {
                musicActive = -1;
                musicPending = false;
            }
        }

        // contagem do delay: so depois dela a musica nova comeca a tocar
        if (musicPending)
        {
            musicDelayTimer -= Time.deltaTime;
            if (musicDelayTimer <= 0f)
            {
                musicPending = false;
                GetMusicSource(musicActive).Play();
            }
        }

        float step = Time.deltaTime / Mathf.Max(0.01f, musicFadeTime);
        for (int i = 0; i < 2; i++)
        {
            AudioSource src = musicSrc[i];
            if (src == null) continue;

            float goal = (i == musicActive && !musicPending) ? targetVol : 0f;
            musicVol[i] = Mathf.MoveTowards(musicVol[i], goal, step);
            src.volume = musicVol[i];

            if (i != musicActive && src.isPlaying && musicVol[i] <= 0.001f)
                src.Stop();
        }
    }

    void Update()
    {
        DebugKeys();

        // espera o som de dano acabar para os efeitos comecarem
        if (effectsPending)
        {
            effectsTimer -= Time.deltaTime;
            if (effectsTimer <= 0f)
                ApplyShownDamage();
        }

        if (materiaisCRTAtivos.Count == 0 && crtMaterial == null) return;

        float target = maxHits > 0 ? (float)shownHits / maxHits : 0f;
        currentLevel = Mathf.SmoothDamp(currentLevel, target, ref levelVelocity, levelSmoothTime);

        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime / Mathf.Max(0.01f, flashDuration));

        // Intervalo entre batimentos (segundos) do dano atual, com transicao suave
        if (shownHits > 0 && stages != null && stages.Length > 0)
        {
            float targetInterval = stages[Mathf.Clamp(shownHits - 1, 0, stages.Length - 1)].pulseInterval;
            if (currentLevel < 0.02f)
                pulseIntervalNow = targetInterval;   // pulso ainda invisivel: ja comeca no ritmo certo
            float ki = 1f - Mathf.Exp(-3f * Time.deltaTime / Mathf.Max(0.01f, veinTransitionTime));
            pulseIntervalNow = Mathf.Lerp(pulseIntervalNow, targetInterval, ki);
        }
        if (AudioPhase(out float audioPhase))
            phase = audioPhase;   // visual preso ao audio: nao existe deriva
        else
            phase = Mathf.Repeat(phase + Time.deltaTime / Mathf.Max(0.05f, pulseIntervalNow), 1f);

        // Cor do ambiente: troca suave entre estagios
        // alpha da cor = quanto do tom entra no shader (0 = visual padrao)
        Color targetColor;
        if (shownHits <= 0 || stageColors == null || stageColors.Length == 0)
        {
            // sem dano: some o tom, mantendo o rgb pra desvanecer sem mudar de cor
            targetColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
        }
        else
        {
            targetColor = stageColors[StageIndex()];
            targetColor.a = 1f;
            // primeiro dano: o tom e invisivel (alpha 0), entao ja comeca na cor certa
            if (currentColor.a < 0.01f)
                currentColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        }
        float k = 1f - Mathf.Exp(-3f * Time.deltaTime / Mathf.Max(0.01f, colorTransitionTime));
        currentColor = Color.Lerp(currentColor, targetColor, k);
        SetCrtColor(DamageColorId, currentColor);

        // Perdeu: escurece ate preto
        darkness = Mathf.MoveTowards(darkness, ShownDead ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, blackoutTime));

        // Game over: so depois que a tela ficou totalmente preta (+ gameOverDelay segundos)
        if (!ShownDead)
        {
            gameOverPlayed = false;
            blackTimer = 0f;
        }
        else if (!gameOverPlayed && darkness >= 0.999f)
        {
            blackTimer += Time.deltaTime;
            if (blackTimer >= gameOverDelay)
            {
                gameOverPlayed = true;
                if (gameOverClip != null)
                    GetSfx().PlayOneShot(gameOverClip, gameOverVolume);
                onGameOver?.Invoke();
            }
        }

        SetCrtFloat(DamageLevelId, currentLevel);
        SetCrtFloat(PulsePhaseId, phase);
        SetCrtFloat(HitFlashId, flash * flash); // curva quadratica: ataque forte, queda suave
        SetCrtFloat(DamageDarknessId, Mathf.SmoothStep(0f, 1f, darkness));

        UpdateVeins();
        UpdateAudio();
        UpdateMusic();
    }

    // O material e um asset: sem isso, os valores de dano ficam salvos nele depois do Play
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        for (int i = 0; i < 2; i++)
            if (musicSrc[i] != null) { musicSrc[i].Stop(); musicSrc[i].volume = 0f; }

        if (heartbeatSource != null)
        {
            heartbeatSource.Stop();
            heartbeatSource.volume = 0f;
        }

        SetCrtFloat(DamageLevelId, 0f);
        SetCrtFloat(PulsePhaseId, 0f);
        SetCrtFloat(HitFlashId, 0f);
        SetCrtColor(DamageColorId, Color.clear);
        SetCrtFloat(DamageDarknessId, 0f);
        SetCrtFloat(VeinReachId, 0f);
        SetCrtFloat(BrushReachId, 0f);
    }
}