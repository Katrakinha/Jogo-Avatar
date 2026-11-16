using UnityEngine;

public class EfeitoDeslizeGelo : MonoBehaviour
{
    [Header("Referências")]
    public PlayerMovement scriptMovimento;
    public Rigidbody rb;

    [Header("Partículas dos Pés")]
    public ParticleSystem geloPeDireito;
    public ParticleSystem geloPeEsquerdo;

    [Header("Física de Patinação (Deslize Suave)")]
    public bool usarImpulsoLateral = true;
    public float forcaDeslizeLateral = 2.0f;
    public float duracaoPassada = 0.38f;
    public float suavizacaoGelo = 0.14f;

    [Header("Inclinação do Corpo no Gelo")]
    public float anguloInclinacaoLateral = 9f;
    public float anguloGiroQuadril = 7f;
    public float velocidadeInclinacao = 8f;

    private float timerPassada = 0f;
    private float ladoAtual = 0f;
    private float velocidadeLateralAtual = 0f;
    private float refVelSuavizacao = 0f;
    private float inclinacaoAtual = 0f;
    private float timerForaDoChao = 0f;
    private Quaternion rotacaoLocalInicial;

    void Awake()
    {
        if (scriptMovimento == null) scriptMovimento = GetComponentInParent<PlayerMovement>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
        rotacaoLocalInicial = transform.localRotation;
    }

    void Start()
    {
        InicializarParticula(geloPeDireito);
        InicializarParticula(geloPeEsquerdo);
        DesligarTodosOsGelos();
    }

    private void InicializarParticula(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        var emissao = ps.emission;
        emissao.enabled = false;
        if (!ps.isPlaying) ps.Play();
    }

    void Update()
    {
        if (scriptMovimento != null)
        {
            if (scriptMovimento.isGrounded && scriptMovimento.pulosRealizados == 0)
            {
                timerForaDoChao = 0f;
            }
            else
            {
                timerForaDoChao += Time.deltaTime;
            }
        }

        bool podeDeslizar = PodeSoltarGelo();

        if (!podeDeslizar)
        {
            DesligarTodosOsGelos();
            timerPassada = 0f;
        }
        else if (timerPassada > 0f)
        {
            timerPassada -= Time.deltaTime;
        }

        AtualizarInclinacaoVisual(podeDeslizar);
    }

    void FixedUpdate()
    {
        if (!usarImpulsoLateral || rb == null || scriptMovimento == null) return;

        float alvoVelocidadeLateral = 0f;

        if (PodeSoltarGelo() && timerPassada > 0f)
        {
            float progresso = 1f - (timerPassada / Mathf.Max(0.01f, duracaoPassada));
            float curvaArco = Mathf.Sin(progresso * Mathf.PI);
            alvoVelocidadeLateral = ladoAtual * forcaDeslizeLateral * curvaArco;
        }

        velocidadeLateralAtual = Mathf.SmoothDamp(
            velocidadeLateralAtual,
            alvoVelocidadeLateral,
            ref refVelSuavizacao,
            suavizacaoGelo,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        if (Mathf.Abs(velocidadeLateralAtual) > 0.01f && PodeSoltarGelo())
        {
            Vector3 vetorLateral = scriptMovimento.transform.right * velocidadeLateralAtual;
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x + vetorLateral.x,
                rb.linearVelocity.y,
                rb.linearVelocity.z + vetorLateral.z
            );
        }
    }

    private void AtualizarInclinacaoVisual(bool patinando)
    {
        if (!usarImpulsoLateral) return;

        float alvoInclinacao = (patinando && timerPassada > 0f) ? ladoAtual : 0f;
        inclinacaoAtual = Mathf.Lerp(inclinacaoAtual, alvoInclinacao, velocidadeInclinacao * Time.deltaTime);

        Quaternion tilt = Quaternion.Euler(
            0f,
            inclinacaoAtual * anguloGiroQuadril,
            -inclinacaoAtual * anguloInclinacaoLateral
        );

        transform.localRotation = rotacaoLocalInicial * tilt;
    }

    private bool PodeSoltarGelo()
    {
        if (scriptMovimento == null) return true;

        bool noChaoComTolerancia = (scriptMovimento.isGrounded || timerForaDoChao < 0.12f) && scriptMovimento.pulosRealizados == 0 && !scriptMovimento.didJump;
        bool emMovimento = scriptMovimento.currentSpeed > 0.15f;
        bool livreDeAcoes = !scriptMovimento.isAttacking && !scriptMovimento.isGliding && !scriptMovimento.isEarthSlamming;

        return noChaoComTolerancia && emMovimento && livreDeAcoes;
    }

    public void LigarGeloDireito()
    {
        if (!PodeSoltarGelo()) return;

        AtivarEmissaoGelo(geloPeDireito, true);
        IniciarPassadaPatins(-1f);
    }

    public void DesligarGeloDireito()
    {
        AtivarEmissaoGelo(geloPeDireito, false);
    }

    public void LigarGeloEsquerdo()
    {
        if (!PodeSoltarGelo()) return;

        AtivarEmissaoGelo(geloPeEsquerdo, true);
        IniciarPassadaPatins(1f);
    }

    public void DesligarGeloEsquerdo()
    {
        AtivarEmissaoGelo(geloPeEsquerdo, false);
    }

    private void AtivarEmissaoGelo(ParticleSystem ps, bool ativo)
    {
        if (ps == null) return;

        if (!ps.isPlaying && ativo)
        {
            ps.Play();
        }

        var emissao = ps.emission;
        emissao.enabled = ativo;

        if (ativo)
        {
            ps.Emit(1);
        }
    }

    private void IniciarPassadaPatins(float lado)
    {
        if (!usarImpulsoLateral) return;
        ladoAtual = lado;
        timerPassada = duracaoPassada;
    }

    public void DesligarTodosOsGelos()
    {
        AtivarEmissaoGelo(geloPeDireito, false);
        AtivarEmissaoGelo(geloPeEsquerdo, false);
    }
}