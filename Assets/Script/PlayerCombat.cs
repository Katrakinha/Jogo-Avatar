using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Banco de Dados")]
    public AvatarCombatData data;

    [Header("Controles")]
    public InputActionReference controleAtacar;

    [Header("Configurações de Combo")]
    public int maxPassosCombo = 4;
    [Range(0.7f, 0.98f)]
    public float porcentagemFimAnimacao = 0.88f;
    public float tempoSegurancaAtaque = 1.5f;

    [Header("Estado do Combate")]
    public int comboStep = 0;
    public bool isAttacking = false;

    [Header("Hitboxes")]
    public GameObject hitboxSoco;

    [Header("Dobra de Água (Exclusivo Katara)")]
    public bool usarDobraAgua = false;
    public bool mostrarGizmoAgua = false;
    public Transform pontoAguaDireita;
    public Transform pontoAguaEsquerda;
    public Transform pontoAguaMeio;
    public ProjetilAguaKatara prefabProjetilAgua;
    public BolaAguaKatara prefabBolaAgua;
    public GameObject vfxSplashChao;
    public GameObject vfxWaterExplosion;
    public GameObject vfxWaterHit;
    public float alcanceMaximoAgua = 6f;
    public float distanciaNascimentoAtras = 0.7f;
    public LayerMask layerInimigos;

    [Header("Referências")]
    public PlayerMovement scriptMovimento;
    public PlayerAnimator scriptAnimador;
    public IdentidadePersonagem identidade;

    [HideInInspector] public bool inputFakeAtaque = false;

    private float comboTimer = 0f;
    private float tempoDecorridoAtaque = 0f;
    private bool entrouNoEstadoDeAtaque = false;
    private bool inputBuffer = false;
    private bool estaEmDash = false;
    private Rigidbody rb;
    private Coroutine rotinaImpulso;
    private BolaAguaKatara bolaAguaAtiva;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (identidade == null) identidade = GetComponent<IdentidadePersonagem>();
        if (scriptMovimento == null) scriptMovimento = GetComponent<PlayerMovement>();
        if (scriptAnimador == null) scriptAnimador = GetComponent<PlayerAnimator>();
        if (hitboxSoco != null) hitboxSoco.SetActive(false);
    }

    void OnEnable()
    {
        if (controleAtacar != null) controleAtacar.action.Enable();
    }

    void OnDisable()
    {
        if (controleAtacar != null) controleAtacar.action.Disable();
    }

    void Update()
    {
        if (identidade == null || identidade.isPlayerControlled)
        {
            if (controleAtacar != null && controleAtacar.action.WasPressedThisFrame())
            {
                if (scriptMovimento != null && scriptMovimento.isEarthSlamming) return;
                inputBuffer = true;
            }
        }
        else if (inputFakeAtaque)
        {
            inputFakeAtaque = false;
            if (scriptMovimento == null || !scriptMovimento.isEarthSlamming)
            {
                inputBuffer = true;
            }
        }

        if (isAttacking)
        {
            tempoDecorridoAtaque += Time.deltaTime;
            VerificarProgressoAnimacao();
        }
        else
        {
            if (comboStep > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    ZerarCombo();
                }
            }

            if (inputBuffer)
            {
                inputBuffer = false;
                Atacar();
            }
        }
    }

    void FixedUpdate()
    {
        if (isAttacking && !estaEmDash && rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    private void VerificarProgressoAnimacao()
    {
        if (scriptAnimador != null && scriptAnimador.anim != null)
        {
            Animator anim = scriptAnimador.anim;
            bool emTransicao = anim.IsInTransition(0);

            if (!entrouNoEstadoDeAtaque)
            {
                if (tempoDecorridoAtaque > 0.08f && !emTransicao)
                {
                    entrouNoEstadoDeAtaque = true;
                }
            }
            else
            {
                AnimatorStateInfo estadoAtual = anim.GetCurrentAnimatorStateInfo(0);

                if ((!emTransicao && estadoAtual.normalizedTime >= porcentagemFimAnimacao) || emTransicao)
                {
                    FinalizarAtaqueAtual();
                    return;
                }
            }
        }

        if (tempoDecorridoAtaque >= tempoSegurancaAtaque)
        {
            FinalizarAtaqueAtual();
        }
    }

    public void Atacar()
    {
        if (comboStep >= maxPassosCombo) comboStep = 0;

        comboStep++;
        isAttacking = true;
        tempoDecorridoAtaque = 0f;
        entrouNoEstadoDeAtaque = false;
        comboTimer = data != null ? data.tempoJanelaCombo : 1.0f;

        if (comboStep == 1 && bolaAguaAtiva != null)
        {
            bolaAguaAtiva.DescerBola();
            bolaAguaAtiva = null;
        }

        if (scriptMovimento != null)
        {
            scriptMovimento.isAttacking = true;
        }

        if (rotinaImpulso != null) StopCoroutine(rotinaImpulso);
        estaEmDash = false;

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        if (scriptAnimador != null)
        {
            scriptAnimador.DispararAnimacaoDeAtaque(comboStep);
        }
    }

    private void ZerarCombo()
    {
        comboStep = 0;
        inputBuffer = false;
        estaEmDash = false;
        if (hitboxSoco != null) hitboxSoco.SetActive(false);

        if (bolaAguaAtiva != null)
        {
            bolaAguaAtiva.DescerBola();
            bolaAguaAtiva = null;
        }
    }

    private LayerMask ObterLayerInimigosValida()
    {
        if (layerInimigos.value != 0) return layerInimigos;
        if (data != null && data.layerInimigos.value != 0) return data.layerInimigos;
        return ~0;
    }

    public void AnimEvent_LigarAguaDireita()
    {
        if (!usarDobraAgua) return;
        DispararJabAgua(pontoAguaDireita, 0.35f);
    }

    public void AnimEvent_LigarAguaEsquerda()
    {
        if (!usarDobraAgua) return;
        DispararJabAgua(pontoAguaEsquerda, -0.35f);
    }

    public void AnimEvent_CriarBolaAgua()
    {
        if (!usarDobraAgua || prefabBolaAgua == null) return;

        if (bolaAguaAtiva != null)
        {
            bolaAguaAtiva.DescerBola();
        }

        float dano = data != null ? data.danoAtaqueBase : 10f;
        float knockback = data != null ? data.forcaKnockback : 3f;
        LayerMask mascaraChao = scriptMovimento != null ? scriptMovimento.groundLayer : 0;

        Vector3 posChao = pontoAguaMeio != null ? pontoAguaMeio.position : (transform.position + transform.forward * 1.2f);
        bolaAguaAtiva = Instantiate(prefabBolaAgua, posChao, Quaternion.identity);
        bolaAguaAtiva.Inicializar(
            posChao,
            vfxSplashChao,
            vfxWaterExplosion,
            ObterLayerInimigosValida(),
            mascaraChao,
            dano,
            knockback,
            transform
        );
    }

    public void AnimEvent_EstourarBolaAgua()
    {
        if (!usarDobraAgua) return;

        if (bolaAguaAtiva != null)
        {
            LayerMask mascaraInimigos = ObterLayerInimigosValida();
            Transform alvo = BuscarInimigoNoAlcance(mascaraInimigos);

            if (alvo != null)
            {
                Vector3 dirOlhar = alvo.position - transform.position;
                dirOlhar.y = 0f;
                if (dirOlhar.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(dirOlhar.normalized);
                }
            }

            bolaAguaAtiva.DispararBola(transform.forward, alvo, alcanceMaximoAgua);
            bolaAguaAtiva = null;
        }
    }

    private void DispararJabAgua(Transform pontoMao, float desvioLateralNascimento)
    {
        if (prefabProjetilAgua == null) return;

        Transform refMao = pontoMao != null ? pontoMao : transform;
        Vector3 posAtras = transform.position
            - (transform.forward * distanciaNascimentoAtras)
            + (transform.right * desvioLateralNascimento)
            + (Vector3.up * 0.9f);

        LayerMask mascaraInimigos = ObterLayerInimigosValida();
        Transform alvo = BuscarInimigoNoAlcance(mascaraInimigos);
        if (alvo != null)
        {
            Vector3 dirOlhar = alvo.position - transform.position;
            dirOlhar.y = 0f;
            if (dirOlhar.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(dirOlhar.normalized);
            }
        }

        float dano = data != null ? data.danoAtaqueBase : 10f;
        float knockback = data != null ? data.forcaKnockback : 3f;
        LayerMask mascaraChao = scriptMovimento != null ? scriptMovimento.groundLayer : 0;

        ProjetilAguaKatara novoProjetil = Instantiate(prefabProjetilAgua, posAtras, Quaternion.LookRotation(transform.forward));
        novoProjetil.Inicializar(
            refMao,
            transform.forward,
            alcanceMaximoAgua,
            alvo,
            vfxWaterHit,
            mascaraInimigos,
            mascaraChao,
            transform,
            dano,
            knockback
        );
    }

    private Transform BuscarInimigoNoAlcance(LayerMask mascaraInimigos)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alcanceMaximoAgua, mascaraInimigos);
        Transform melhorAlvo = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Collider col in hits)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            UniversalEnemy inimigo = col.GetComponentInParent<UniversalEnemy>();
            if (inimigo == null || inimigo.currentState == UniversalEnemy.EnemyState.Dead) continue;

            Vector3 dirParaAlvo = (inimigo.transform.position - transform.position);
            dirParaAlvo.y = 0f;
            float distancia = dirParaAlvo.magnitude;

            if (Vector3.Angle(transform.forward, dirParaAlvo.normalized) <= 120f && distancia < menorDistancia)
            {
                menorDistancia = distancia;
                melhorAlvo = inimigo.transform;
            }
        }

        return melhorAlvo;
    }

    public void AnimEvent_ImpulsoAtaque()
    {
        if (data == null) return;
        if (rotinaImpulso != null) StopCoroutine(rotinaImpulso);
        rotinaImpulso = StartCoroutine(RotinaDashSuave(data.avancoAtaque));
    }

    private IEnumerator RotinaDashSuave(float forcaMax)
    {
        estaEmDash = true;
        float tempoDoDash = 0.15f;
        float timer = 0f;

        Vector3 direcao = transform.forward;
        direcao.y = 0f;
        direcao.Normalize();

        while (timer < tempoDoDash && isAttacking)
        {
            float velocidadeAtual = Mathf.Lerp(forcaMax, 0f, timer / tempoDoDash);
            rb.linearVelocity = new Vector3(direcao.x * velocidadeAtual, rb.linearVelocity.y, direcao.z * velocidadeAtual);

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        estaEmDash = false;
    }

    public void AnimEvent_LigarHitbox()
    {
        if (hitboxSoco != null) hitboxSoco.SetActive(true);
    }

    public void AnimEvent_DesligarHitbox()
    {
        if (hitboxSoco != null) hitboxSoco.SetActive(false);
    }

    public void AnimEvent_FimDoAtaque()
    {
        if (!isAttacking || !entrouNoEstadoDeAtaque || tempoDecorridoAtaque < 0.25f) return;

        if (scriptAnimador != null && scriptAnimador.anim != null)
        {
            if (scriptAnimador.anim.IsInTransition(0)) return;

            AnimatorStateInfo estadoAtual = scriptAnimador.anim.GetCurrentAnimatorStateInfo(0);
            if (estadoAtual.normalizedTime < 0.75f) return;
        }

        FinalizarAtaqueAtual();
    }

    private void FinalizarAtaqueAtual()
    {
        if (!isAttacking) return;

        isAttacking = false;
        estaEmDash = false;
        entrouNoEstadoDeAtaque = false;

        if (rotinaImpulso != null) StopCoroutine(rotinaImpulso);

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        if (scriptMovimento != null)
        {
            scriptMovimento.isAttacking = false;
        }

        if (hitboxSoco != null) hitboxSoco.SetActive(false);

        if (inputBuffer)
        {
            inputBuffer = false;
            Atacar();
        }
    }

    void OnDrawGizmos()
    {
        if (!mostrarGizmoAgua) return;

        Gizmos.color = new Color(0f, 0.75f, 1f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, alcanceMaximoAgua);

        Gizmos.color = Color.blue;
        if (pontoAguaDireita != null) Gizmos.DrawWireSphere(pontoAguaDireita.position, 0.12f);
        if (pontoAguaEsquerda != null) Gizmos.DrawWireSphere(pontoAguaEsquerda.position, 0.12f);

        Gizmos.color = Color.cyan;
        if (pontoAguaMeio != null) Gizmos.DrawWireSphere(pontoAguaMeio.position, 0.2f);
    }
}