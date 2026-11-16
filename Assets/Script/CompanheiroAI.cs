using UnityEngine;
using UnityEngine.AI;

public class CompanheiroAI : MonoBehaviour
{
    [Header("Referências")]
    public IdentidadePersonagem identidade;
    public PlayerMovement scriptMovimento;
    public PlayerCombat scriptCombate;

    [Header("Configurações de Seguimento")]
    public float distanciaParaSeguir = 10f;
    public float margemParaRecomecar = 1.0f;
    public float zonaDesaceleracao = 1.8f;
    public float suavizacaoCurva = 0.12f;
    public float distanciaTeleporte = 50f;

    [Header("Configurações de Parkour (Pulo)")]
    public float distanciaDeteccaoObstaculo = 4.5f;
    public float alturaLaserBaixo = 0.8f;
    public float alturaLaserMedio = 2.0f;
    public float alturaMaximaPulo = 6.5f;
    public float diferencaAlturaParaSubir = 1.2f;
    public float intervaloEntrePulos = 0.75f;

    [Header("Fase 4: Cão de Guarda (Combate IA)")]
    public bool ativarCaoDeGuarda = true;
    public bool mostrarGizmoCombateIA = true;
    public float raioVisaoInimigo = 50.0f;
    public float distanciaDesistirCombate = 50.0f;
    public float distanciaAtaquePadrao = 15.0f;
    public float distanciaAtaqueAgua = 25.0f;
    public float intervaloClickCombo = 0.20f;

    private Transform liderAtual;
    private UniversalEnemy inimigoAlvo;
    private NavMeshPath caminhoCalculado;
    private float timerRecalculo;
    private float intervaloRecalculo = 0.1f;
    private bool estaSeguindo;
    private Vector3 direcaoSuavizada;
    private Vector3 velocidadeSuavizacao;
    private float cooldownPulo = 0f;
    private float timerClickAtaque = 0f;
    private Rigidbody rb;

    void Awake()
    {
        GarantirReferencias();
    }

    void Start()
    {
        GarantirReferencias();
        caminhoCalculado = new NavMeshPath();
    }

    private void GarantirReferencias()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (identidade == null) identidade = GetComponent<IdentidadePersonagem>();
        if (scriptMovimento == null) scriptMovimento = GetComponent<PlayerMovement>();

        if (scriptCombate == null)
        {
            scriptCombate = GetComponent<PlayerCombat>();
            if (scriptCombate == null) scriptCombate = GetComponentInChildren<PlayerCombat>(true);
            if (scriptCombate == null) scriptCombate = GetComponentInParent<PlayerCombat>();
        }

        if (scriptCombate != null && !scriptCombate.enabled)
        {
            scriptCombate.enabled = true;
        }
    }

    private Vector3 ObterPosicaoPe()
    {
        if (scriptMovimento != null && scriptMovimento.groundCheck != null)
        {
            return scriptMovimento.groundCheck.position;
        }
        return transform.position - Vector3.up * 0.9f;
    }

    void Update()
    {
        GarantirReferencias();

        if (cooldownPulo > 0f) cooldownPulo -= Time.deltaTime;
        if (timerClickAtaque > 0f) timerClickAtaque -= Time.deltaTime;

        if (identidade != null && identidade.isPlayerControlled)
        {
            PararMovimentoIA();
            return;
        }

        EncontrarLider();

        bool temLiderVivo = liderAtual != null && liderAtual.gameObject.activeInHierarchy;
        float distTotalLider = temLiderVivo ? Vector3.Distance(transform.position, liderAtual.position) : 0f;

        if (temLiderVivo && distTotalLider > distanciaTeleporte)
        {
            TeleportarParaPertoDoLider();
            return;
        }

        if (ativarCaoDeGuarda && (!temLiderVivo || distTotalLider <= distanciaDesistirCombate))
        {
            ProcurarInimigoProximo();
        }
        else
        {
            inimigoAlvo = null;
        }

        if (inimigoAlvo != null)
        {
            ExecutarModoCombate();
        }
        else if (temLiderVivo)
        {
            ExecutarModoSeguirLider();
        }
        else
        {
            PararMovimentoIA();
        }
    }

    public void TeleportarParaPertoDoLider()
    {
        if (liderAtual == null) EncontrarLider();
        if (liderAtual == null) return;

        Vector3 posicaoNasCostas = liderAtual.position - (liderAtual.forward * 2f);
        if (NavMesh.SamplePosition(posicaoNasCostas, out NavMeshHit hitTp, 5f, NavMesh.AllAreas))
        {
            posicaoNasCostas = hitTp.position + Vector3.up * 1f;
        }
        transform.position = posicaoNasCostas;
        if (rb != null) rb.linearVelocity = Vector3.zero;
        inimigoAlvo = null;
        PararMovimentoIA();
    }

    private void ProcurarInimigoProximo()
    {
        if (inimigoAlvo != null)
        {
            if (inimigoAlvo.gameObject.activeInHierarchy && inimigoAlvo.currentState != UniversalEnemy.EnemyState.Dead)
            {
                float distInimigo = CalcularDistanciaPlanaAteInimigo(inimigoAlvo);
                if (distInimigo <= raioVisaoInimigo * 1.4f)
                {
                    return;
                }
            }
            inimigoAlvo = null;
        }

        UniversalEnemy[] todosInimigos = FindObjectsByType<UniversalEnemy>(FindObjectsSortMode.None);
        float menorDistancia = Mathf.Infinity;
        UniversalEnemy melhorInimigo = null;

        foreach (UniversalEnemy inimigo in todosInimigos)
        {
            if (inimigo == null || !inimigo.gameObject.activeInHierarchy) continue;
            if (inimigo.currentState == UniversalEnemy.EnemyState.Dead) continue;

            float distCompanheiro = CalcularDistanciaPlanaAteInimigo(inimigo);
            float distLider = liderAtual != null ? Vector3.Distance(
                new Vector3(liderAtual.position.x, 0f, liderAtual.position.z),
                new Vector3(inimigo.transform.position.x, 0f, inimigo.transform.position.z)
            ) : distCompanheiro;

            if (distCompanheiro <= raioVisaoInimigo || distLider <= raioVisaoInimigo)
            {
                if (distCompanheiro < menorDistancia)
                {
                    menorDistancia = distCompanheiro;
                    melhorInimigo = inimigo;
                }
            }
        }

        inimigoAlvo = melhorInimigo;
    }

    private float CalcularDistanciaPlanaAteInimigo(UniversalEnemy inimigo)
    {
        Vector3 minhaPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 posInimigo = new Vector3(inimigo.transform.position.x, 0f, inimigo.transform.position.z);
        float distCentro = Vector3.Distance(minhaPos, posInimigo);

        Collider colInimigo = inimigo.GetComponent<Collider>();
        if (colInimigo != null && colInimigo.enabled)
        {
            Vector3 pontoMaisProximo = colInimigo.ClosestPoint(transform.position);
            Vector3 pontoPlano = new Vector3(pontoMaisProximo.x, 0f, pontoMaisProximo.z);
            float distBorda = Vector3.Distance(minhaPos, pontoPlano);
            return Mathf.Min(distCentro, distBorda);
        }

        return distCentro;
    }

    private void ExecutarModoCombate()
    {
        Transform transInimigo = inimigoAlvo.transform;
        float distanciaPlana = CalcularDistanciaPlanaAteInimigo(inimigoAlvo);
        float alcanceNecessario = ObterAlcanceDeAtaqueIdeal();

        bool dentroDoAlcance = distanciaPlana <= alcanceNecessario;

        if (!dentroDoAlcance && (scriptCombate == null || !scriptCombate.isAttacking))
        {
            timerRecalculo -= Time.deltaTime;
            if (timerRecalculo <= 0f)
            {
                timerRecalculo = intervaloRecalculo;
                AtualizarCaminhoNavMesh(transInimigo.position);
            }

            float diferencaAltura = transInimigo.position.y - transform.position.y;
            bool alvoMaisAlto = diferencaAltura > diferencaAlturaParaSubir;
            Vector3 direcaoDesejada = ObterDirecaoDoCaminho(transInimigo.position, alvoMaisAlto);
            VerificarNecessidadeDePulo(direcaoDesejada, transInimigo, alvoMaisAlto);

            direcaoSuavizada = Vector3.SmoothDamp(direcaoSuavizada, direcaoDesejada, ref velocidadeSuavizacao, suavizacaoCurva);
            scriptMovimento.inputFakeAtivo = true;
            scriptMovimento.direcaoMovimentoIA = direcaoSuavizada;
        }
        else
        {
            estaSeguindo = false;
            direcaoSuavizada = Vector3.zero;
            velocidadeSuavizacao = Vector3.zero;
            scriptMovimento.inputFakeAtivo = true;
            scriptMovimento.inputFakePulo = false;
            scriptMovimento.direcaoMovimentoIA = Vector3.zero;
            scriptMovimento.currentSpeed = 0f;

            if (rb != null && (scriptCombate == null || !scriptCombate.isAttacking))
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            Vector3 dirOlhar = transInimigo.position - transform.position;
            dirOlhar.y = 0f;
            if (dirOlhar.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirOlhar.normalized), 20f * Time.deltaTime);
            }

            if (scriptCombate != null && timerClickAtaque <= 0f)
            {
                scriptCombate.inputFakeAtaque = true;
                if (!scriptCombate.isAttacking)
                {
                    scriptCombate.Atacar();
                }
                timerClickAtaque = intervaloClickCombo;
            }
        }
    }

    private float ObterAlcanceDeAtaqueIdeal()
    {
        if (scriptCombate != null && scriptCombate.usarDobraAgua)
        {
            if (distanciaAtaqueAgua > 0.1f) return distanciaAtaqueAgua;
            if (scriptCombate.alcanceMaximoAgua > 0.1f) return scriptCombate.alcanceMaximoAgua;
        }

        float maiorConfigurado = Mathf.Max(distanciaAtaquePadrao, distanciaAtaqueAgua);
        return maiorConfigurado > 0.5f ? maiorConfigurado : 5.0f;
    }

    private void ExecutarModoSeguirLider()
    {
        Vector3 minhaPosPlana = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 alvoPosPlana = new Vector3(liderAtual.position.x, 0f, liderAtual.position.z);
        float distanciaPlana = Vector3.Distance(minhaPosPlana, alvoPosPlana);
        float diferencaAlturaLider = liderAtual.position.y - transform.position.y;

        bool longeNaHorizontal = distanciaPlana > (distanciaParaSeguir + margemParaRecomecar);
        bool liderMaisAlto = diferencaAlturaLider > diferencaAlturaParaSubir;

        if (!estaSeguindo && (longeNaHorizontal || liderMaisAlto))
        {
            estaSeguindo = true;
        }
        else if (estaSeguindo && distanciaPlana <= distanciaParaSeguir && !liderMaisAlto)
        {
            estaSeguindo = false;
        }

        if (estaSeguindo)
        {
            timerRecalculo -= Time.deltaTime;
            if (timerRecalculo <= 0f)
            {
                timerRecalculo = intervaloRecalculo;
                AtualizarCaminhoNavMesh(liderAtual.position);
            }

            Vector3 direcaoDesejada = ObterDirecaoDoCaminho(liderAtual.position, liderMaisAlto);

            VerificarNecessidadeDePulo(direcaoDesejada, liderAtual, liderMaisAlto);

            float fatorVelocidade = Mathf.Clamp01((distanciaPlana - distanciaParaSeguir) / Mathf.Max(0.1f, zonaDesaceleracao));
            if (liderMaisAlto || !scriptMovimento.isGrounded)
            {
                fatorVelocidade = 1f;
            }
            else
            {
                fatorVelocidade = Mathf.Max(0.5f, fatorVelocidade);
            }

            Vector3 vetorAlvo = direcaoDesejada * fatorVelocidade;
            direcaoSuavizada = Vector3.SmoothDamp(direcaoSuavizada, vetorAlvo, ref velocidadeSuavizacao, suavizacaoCurva);

            scriptMovimento.inputFakeAtivo = true;
            scriptMovimento.direcaoMovimentoIA = direcaoSuavizada;
        }
        else
        {
            direcaoSuavizada = Vector3.SmoothDamp(direcaoSuavizada, Vector3.zero, ref velocidadeSuavizacao, suavizacaoCurva * 0.8f);
            scriptMovimento.inputFakeAtivo = true;

            if (direcaoSuavizada.sqrMagnitude < 0.005f) direcaoSuavizada = Vector3.zero;

            scriptMovimento.direcaoMovimentoIA = direcaoSuavizada;
        }
    }

    private void VerificarNecessidadeDePulo(Vector3 direcaoMovimento, Transform alvoRef, bool alvoMaisAlto)
    {
        if (!scriptMovimento.isGrounded || cooldownPulo > 0f) return;

        Vector3 dirParaAlvo = alvoRef != null ? (alvoRef.position - transform.position) : transform.forward;
        dirParaAlvo.y = 0f;
        Vector3 dirDiretaNorm = dirParaAlvo.sqrMagnitude > 0.01f ? dirParaAlvo.normalized : transform.forward;
        Vector3 dirMovNorm = direcaoMovimento.sqrMagnitude > 0.01f ? direcaoMovimento.normalized : dirDiretaNorm;

        bool devePular = TestarLasersDePuloNaDirecao(dirMovNorm);

        if (!devePular && (alvoMaisAlto || Vector3.Angle(dirMovNorm, dirDiretaNorm) > 15f))
        {
            devePular = TestarLasersDePuloNaDirecao(dirDiretaNorm);
        }

        if (!devePular && caminhoCalculado != null && caminhoCalculado.corners.Length > 1)
        {
            Vector3 posPe = ObterPosicaoPe();
            Vector3 proximoCanto = caminhoCalculado.corners[1];
            float distPlanaCanto = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(proximoCanto.x, proximoCanto.z));
            float diffAlturaCanto = proximoCanto.y - posPe.y;

            if (diffAlturaCanto > 0.5f && distPlanaCanto < distanciaDeteccaoObstaculo)
            {
                devePular = true;
            }
        }

        if (devePular)
        {
            scriptMovimento.inputFakePulo = true;
            cooldownPulo = intervaloEntrePulos;
        }
    }

    private bool TestarLasersDePuloNaDirecao(Vector3 direcao)
    {
        Vector3 posPe = ObterPosicaoPe();
        Vector3 origemBaixa = posPe + Vector3.up * alturaLaserBaixo;
        Vector3 origemMedia = posPe + Vector3.up * alturaLaserMedio;
        Vector3 origemAcimaCabeca = posPe + Vector3.up * alturaMaximaPulo;

        bool bateuEmbaixo = DetectarObstaculoValido(origemBaixa, direcao) || DetectarObstaculoValido(origemMedia, direcao);
        if (!bateuEmbaixo) return false;

        bool livreEmCima = !DetectarObstaculoValido(origemAcimaCabeca, direcao);
        return livreEmCima;
    }

    private bool DetectarObstaculoValido(Vector3 origem, Vector3 direcao)
    {
        RaycastHit[] hits = Physics.RaycastAll(origem, direcao, distanciaDeteccaoObstaculo, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (liderAtual != null && (hit.transform == liderAtual || hit.transform.IsChildOf(liderAtual))) continue;
            if (inimigoAlvo != null && (hit.transform == inimigoAlvo.transform || hit.transform.IsChildOf(inimigoAlvo.transform))) continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > 40f)
            {
                return true;
            }
        }
        return false;
    }

    private void AtualizarCaminhoNavMesh(Vector3 posDestino)
    {
        Vector3 origem = ObterPosicaoPe();
        Vector3 destino = posDestino;

        if (NavMesh.SamplePosition(origem, out NavMeshHit hitOrigem, 5f, NavMesh.AllAreas)) origem = hitOrigem.position;
        if (NavMesh.SamplePosition(destino, out NavMeshHit hitDestino, 10f, NavMesh.AllAreas)) destino = hitDestino.position;

        NavMesh.CalculatePath(origem, destino, NavMesh.AllAreas, caminhoCalculado);
    }

    private Vector3 ObterDirecaoDoCaminho(Vector3 posDestino, bool alvoMaisAlto)
    {
        Vector3 dirDireta = posDestino - transform.position;
        dirDireta.y = 0f;
        Vector3 dirDiretaNorm = dirDireta.sqrMagnitude > 0.001f ? dirDireta.normalized : Vector3.zero;

        if (alvoMaisAlto || !scriptMovimento.isGrounded)
        {
            return dirDiretaNorm;
        }

        if (caminhoCalculado != null && caminhoCalculado.status == NavMeshPathStatus.PathComplete && caminhoCalculado.corners.Length > 1)
        {
            int indiceCanto = 1;
            while (indiceCanto < caminhoCalculado.corners.Length - 1)
            {
                Vector3 canto = caminhoCalculado.corners[indiceCanto];
                float distCanto = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(canto.x, canto.z));
                if (distCanto < 0.8f) indiceCanto++;
                else break;
            }

            Vector3 proximoPonto = caminhoCalculado.corners[indiceCanto];
            Vector3 dir = proximoPonto - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f) return dir.normalized;
        }

        return dirDiretaNorm;
    }

    private void PararMovimentoIA()
    {
        estaSeguindo = false;
        inimigoAlvo = null;
        direcaoSuavizada = Vector3.zero;
        velocidadeSuavizacao = Vector3.zero;
        scriptMovimento.inputFakeAtivo = false;
        scriptMovimento.inputFakePulo = false;
        scriptMovimento.direcaoMovimentoIA = Vector3.zero;
    }

    private void EncontrarLider()
    {
        if (liderAtual != null && liderAtual.gameObject.activeInHierarchy)
        {
            IdentidadePersonagem idAlvo = liderAtual.GetComponent<IdentidadePersonagem>();
            if (idAlvo != null && idAlvo.isPlayerControlled) return;
        }

        liderAtual = null;
        IdentidadePersonagem[] todosPersonagens = FindObjectsByType<IdentidadePersonagem>(FindObjectsSortMode.None);

        foreach (IdentidadePersonagem p in todosPersonagens)
        {
            if (p != null && p.gameObject.activeInHierarchy && p.isPlayerControlled && p != this.identidade)
            {
                liderAtual = p.transform;
                break;
            }
        }
    }

    void OnDrawGizmos()
    {
        GarantirReferencias();

        Vector3 dirOlhar = Application.isPlaying && direcaoSuavizada.sqrMagnitude > 0.01f ? direcaoSuavizada.normalized : transform.forward;
        Vector3 posPe = ObterPosicaoPe();

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(posPe + Vector3.up * alturaLaserBaixo, posPe + Vector3.up * alturaLaserBaixo + dirOlhar * distanciaDeteccaoObstaculo);
        Gizmos.DrawLine(posPe + Vector3.up * alturaLaserMedio, posPe + Vector3.up * alturaLaserMedio + dirOlhar * distanciaDeteccaoObstaculo);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(posPe + Vector3.up * alturaMaximaPulo, posPe + Vector3.up * alturaMaximaPulo + dirOlhar * distanciaDeteccaoObstaculo);

        if (mostrarGizmoCombateIA && ativarCaoDeGuarda)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, raioVisaoInimigo);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, ObterAlcanceDeAtaqueIdeal());
        }
    }
}