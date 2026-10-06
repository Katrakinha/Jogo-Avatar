using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class UniversalEnemy : MonoBehaviour
{
    [Header("Configurações")]
    public EnemyData data;
    public Animator anim;
    
    [Header("Armas e Combate")]
    public GameObject hitboxAtaque; // Arrasta o objeto da lança/mão aqui
    public Transform pontoDisparoFogo; // Ojeto vazio na frente da mão de onde sai a bola de fogo
    
    [Header("Detecção e Física")]
    public Transform pontoDaCabeca; 
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Patrulha")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;

    // ADICIONADO: Novo estado de Ataque!
    public enum EnemyState { Patrol, Suspicious, Chase, Attacking, Stunned, Dead }
    public EnemyState currentState;

    private NavMeshAgent agent;
    private Transform playerTarget;
    
    private float temporizadorSuspeita = 0f;
    private float tempoSemVerJogador = 0f;
    private bool vendoJogadorAgora = false;

    private float vidaAtual;
    private float timerCooldownAtaque = 0f; // Impede spam de socos

    private Coroutine rotinaStunAtual;
    private Coroutine rotinaKnockback;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = EnemyState.Patrol;
        
        if (data != null) vidaAtual = data.vidaMaxima;
        
        if (hitboxAtaque != null) hitboxAtaque.SetActive(false);
        
        if (waypoints.Length > 0 && agent.isOnNavMesh)
        {
            agent.speed = data.velocidadePatrulha;
            agent.SetDestination(waypoints[0].position);
        }
    }

    void Update()
    {
        if (!agent.isOnNavMesh || data == null) return;

        // Cronómetro para ele voltar a poder bater
        if (timerCooldownAtaque > 0) timerCooldownAtaque -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Patrol:
                if (anim != null) anim.SetFloat("Speed", agent.velocity.magnitude);
                ProcurarJogador();
                RotinaPatrulha();
                break;
            case EnemyState.Suspicious:
                if (anim != null) anim.SetFloat("Speed", 0f);
                ProcurarJogador();
                RotinaSuspeita();
                break;
            case EnemyState.Chase:
                if (anim != null) anim.SetFloat("Speed", agent.velocity.magnitude);
                RotinaPerseguicao();
                break;
            case EnemyState.Attacking:
                if (anim != null) anim.SetFloat("Speed", 0f);
                // Continua a olhar para o Aang enquanto ataca para não bater no vazio se ele der um passinho para o lado
                if (playerTarget != null)
                {
                    Vector3 dir = (playerTarget.position - transform.position).normalized;
                    dir.y = 0;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
                }
                break;
            case EnemyState.Stunned:
            case EnemyState.Dead:
                if (anim != null) anim.SetFloat("Speed", 0f); 
                break;
        }
    }

    // ==========================================
    // FUNÇÕES CHAMADAS PELO ANIMATION EVENT
    // ==========================================
    public void AnimEvent_LigarHitbox() 
    { 
        // TRAVA: Só liga a hitbox se ele estiver de fato atacando (ignora se estiver atordoado)
        if (currentState != EnemyState.Attacking) return;
        
        if (hitboxAtaque != null) hitboxAtaque.SetActive(true); 
    }
    
    public void AnimEvent_DesligarHitbox() { if (hitboxAtaque != null) hitboxAtaque.SetActive(false); }
    
    public void AnimEvent_AtirarProjetil() 
    { 
        // TRAVA: Cancela o tiro se ele tomou dano enquanto conjurava
        if (currentState != EnemyState.Attacking) return;

        if (data.atiradorDeFogo && data.projetilFogoPrefab != null && pontoDisparoFogo != null)
        {
            Instantiate(data.projetilFogoPrefab, pontoDisparoFogo.position, transform.rotation);
        }
    }
    
    public void AnimEvent_FimDoAtaque() 
    { 
        if (currentState == EnemyState.Attacking)
        {
            currentState = EnemyState.Chase; 
            timerCooldownAtaque = data.tempoEntreAtaques; // Reseta o relógio de paciência
            if (hitboxAtaque != null) hitboxAtaque.SetActive(false); // Segurança
        }
    }
    // ==========================================


    public void ReceberDano(float dano, float forcaKnockback, Transform atacante)
    {
        if (currentState == EnemyState.Dead) return;

        // Se ele apanhar a meio de um ataque, desliga a hitbox imediatamente!
        if (hitboxAtaque != null) hitboxAtaque.SetActive(false);

        // TRAVA: Zera a vontade dele de bater. Assim ele não te dá um soco instantâneo mal acabe o stun!
        timerCooldownAtaque = data.tempoEntreAtaques;

        vidaAtual -= dano;
        Debug.Log("+++ IMPACTO! Inimigo sofreu " + dano + " de dano. Vida restante: " + vidaAtual + " +++");

        currentState = EnemyState.Stunned;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath(); 

        if (anim != null) anim.SetTrigger("Hit");

        // --- NOVO SISTEMA: Chama o piscar universal ---
        LegoExplosion efeitosLego = GetComponent<LegoExplosion>();
        if (efeitosLego != null) efeitosLego.PiscarVermelho();

        if (rotinaKnockback != null) StopCoroutine(rotinaKnockback);
        rotinaKnockback = StartCoroutine(EfeitoKnockback(atacante, forcaKnockback));

        if (vidaAtual <= 0) Morrer();
        else
        {
            if (rotinaStunAtual != null) StopCoroutine(rotinaStunAtual);
            rotinaStunAtual = StartCoroutine(RotinaImpacto(atacante));
        }
    }

    private IEnumerator EfeitoKnockback(Transform atacante, float forca)
    {
        float tempoDoDeslize = 0.2f; 
        float timer = 0f;
        Vector3 direcaoEmpurrao = (transform.position - atacante.position).normalized;
        direcaoEmpurrao.y = 0; 
        while (timer < tempoDoDeslize)
        {
            if (agent.isOnNavMesh) agent.Move(direcaoEmpurrao * (forca * Time.deltaTime / tempoDoDeslize));
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RotinaImpacto(Transform atacante)
    {
        yield return new WaitForSeconds(data.tempoStunDano);
        if (currentState != EnemyState.Dead) IniciarPerseguicao(atacante);
    }

    private void Morrer()
    {
        Debug.Log("Inimigo Morreu! EXPLOSÃO DE LEGO!");
        currentState = EnemyState.Dead;
        
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.enabled = false; 
        if (anim != null) anim.enabled = false; 

        Collider colPrincipal = GetComponent<Collider>();
        if (colPrincipal != null) colPrincipal.enabled = false;

        // --- NOVO SISTEMA: Chama a explosão universal ---
        LegoExplosion explosao = GetComponent<LegoExplosion>();
        if (explosao != null) 
        {
            explosao.Explodir();
        }
        else
        {
            Destroy(gameObject); // Backup
        }
    }

    private void ProcurarJogador()
    {
        vendoJogadorAgora = false; 
        Vector3 origemVisao = pontoDaCabeca != null ? pontoDaCabeca.position : transform.position + Vector3.up * 1.5f;
        Vector3 direcaoOlhar = pontoDaCabeca != null ? pontoDaCabeca.forward : transform.forward;
        Collider[] hits = Physics.OverlapSphere(origemVisao, data.raioSuspeita, playerLayer);

        if (hits.Length > 0)
        {
            Transform alvoEncontrado = hits[0].transform;
            Vector3 direcaoParaAlvo = (alvoEncontrado.position - origemVisao).normalized;
            float distancia = Vector3.Distance(origemVisao, alvoEncontrado.position);

            if (distancia <= data.raioVisao && Vector3.Angle(direcaoOlhar, direcaoParaAlvo) < data.anguloVisao / 2)
            {
                if (!Physics.Raycast(origemVisao, direcaoParaAlvo, distancia, obstacleLayer))
                {
                    IniciarPerseguicao(alvoEncontrado);
                    return;
                }
            }

            if (distancia <= data.raioSuspeita && Vector3.Angle(direcaoOlhar, direcaoParaAlvo) < data.anguloSuspeita / 2)
            {
                if (!Physics.Raycast(origemVisao, direcaoParaAlvo, distancia, obstacleLayer))
                {
                    vendoJogadorAgora = true;
                    playerTarget = alvoEncontrado;
                    
                    if (currentState == EnemyState.Patrol)
                    {
                        currentState = EnemyState.Suspicious;
                        agent.isStopped = true; 
                        agent.velocity = Vector3.zero; 
                        if (anim != null) anim.SetTrigger("LookAround"); 
                    }
                }
            }
        }
    }

    private void RotinaSuspeita()
    {
        if (vendoJogadorAgora)
        {
            tempoSemVerJogador = 0f; 
            temporizadorSuspeita += Time.deltaTime;
            if (temporizadorSuspeita >= data.tempoParaDescobrir) IniciarPerseguicao(playerTarget);
        }
        else
        {
            tempoSemVerJogador += Time.deltaTime;
            if (tempoSemVerJogador >= 1.5f)
            {
                temporizadorSuspeita = 0f;
                tempoSemVerJogador = 0f;
                currentState = EnemyState.Patrol;
                agent.isStopped = false; 
            }
        }
    }

    private void IniciarPerseguicao(Transform alvo)
    {
        playerTarget = alvo;
        currentState = EnemyState.Chase;
        agent.isStopped = false;
        agent.speed = data.velocidadePerseguicao;
        temporizadorSuspeita = 0f;
        tempoSemVerJogador = 0f;
    }

    private void RotinaPatrulha()
    {
        if (waypoints.Length <= 1 || isWaiting) return;
        agent.speed = data.velocidadePatrulha;
        if (!agent.pathPending && agent.remainingDistance < 0.5f) StartCoroutine(EsperarNoPonto());
    }

    private IEnumerator EsperarNoPonto()
    {
        isWaiting = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero; 
        if (data.olharAoRedorNoPonto && anim != null) anim.SetTrigger("LookAround");

        yield return new WaitForSeconds(data.tempoDeEsperaNoPonto);

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
        
        agent.isStopped = false;
        isWaiting = false;
    }

    private void RotinaPerseguicao()
    {
        if (playerTarget == null) return;
        
        agent.SetDestination(playerTarget.position);
        float distanciaProPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanciaProPlayer > data.raioFuga)
        {
            playerTarget = null;
            currentState = EnemyState.Patrol;
            if (waypoints.Length > 0) agent.SetDestination(waypoints[currentWaypointIndex].position);
            return;
        }

        // ==========================================
        // LÓGICA DE ATAQUE INTELIGENTE
        // ==========================================
        float distanciaAlvoParaBater = data.atiradorDeFogo ? data.distanciaAtaqueFogo : data.distanciaAtaqueMelee;

        if (distanciaProPlayer <= distanciaAlvoParaBater)
        {
            agent.isStopped = true; 
            
            // Fica sempre a olhar para o Aang
            Vector3 direcaoOlhar = (playerTarget.position - transform.position).normalized;
            direcaoOlhar.y = 0; 
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direcaoOlhar), 8f * Time.deltaTime);

            // Verifica se o ataque já recarregou
            if (timerCooldownAtaque <= 0f)
            {
                currentState = EnemyState.Attacking;
                
                if (data.atiradorDeFogo)
                {
                    if (anim != null) anim.SetTrigger("AttackRanged"); 
                }
                else
                {
                    if (anim != null) anim.SetTrigger("AttackMelee"); 
                }
            }
        }
        else
        {
            agent.isStopped = false; 
        }
    }

    private void OnDrawGizmos()
    {
        if (data == null) return;

        Vector3 origemVisao = pontoDaCabeca != null ? pontoDaCabeca.position : transform.position + Vector3.up * 1.5f;
        Vector3 direcaoOlhar = pontoDaCabeca != null ? pontoDaCabeca.forward : transform.forward;

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        Vector3 limiteEsqSuspeita = Quaternion.Euler(0, -data.anguloSuspeita / 2, 0) * direcaoOlhar;
        Vector3 limiteDirSuspeita = Quaternion.Euler(0, data.anguloSuspeita / 2, 0) * direcaoOlhar;
        Gizmos.DrawRay(origemVisao, limiteEsqSuspeita * data.raioSuspeita);
        Gizmos.DrawRay(origemVisao, limiteDirSuspeita * data.raioSuspeita);
        Gizmos.DrawWireSphere(origemVisao, data.raioSuspeita); 

        Gizmos.color = Color.red;
        Vector3 limiteEsqVisao = Quaternion.Euler(0, -data.anguloVisao / 2, 0) * direcaoOlhar;
        Vector3 limiteDirVisao = Quaternion.Euler(0, data.anguloVisao / 2, 0) * direcaoOlhar;
        Gizmos.DrawRay(origemVisao, limiteEsqVisao * data.raioVisao);
        Gizmos.DrawRay(origemVisao, limiteDirVisao * data.raioVisao);

        Gizmos.color = new Color(1, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, data.raioFuga);

        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                    int nextIndex = (i + 1) % waypoints.Length;
                    if (waypoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
                    }
                }
            }
        }
        // ==========================================
        // ÁREA DE ATAQUE (A ROXA!)
        // ==========================================
        Gizmos.color = Color.magenta; // Cor Roxa/Magenta
        
        // Puxa do Data a distância certa (se for mago puxa a de fogo, se for soldado puxa a melee)
        float distanciaAtaque = data.atiradorDeFogo ? data.distanciaAtaqueFogo : data.distanciaAtaqueMelee;
        
        // Desenha a esfera em volta do inimigo
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}