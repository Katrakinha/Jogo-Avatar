using UnityEngine;

public class ProjetilAguaKatara : MonoBehaviour
{
    [Header("Tempos do Arco (Fase na Mão)")]
    public float tempoEsticarAteMao = 0.12f;
    public float tempoRecolherArco = 0.08f;

    [Header("Formato do Arco (Ponto A -> Ponto B)")]
    public int quantidadeSegmentosArco = 8;
    public float larguraDoArco = 0.55f;
    public float alturaDoArco = 0.15f;

    [Header("Disparo (Fase Projétil)")]
    public float velocidadeTiro = 16f;
    public float forcaCurvaTeleguiado = 10f;
    public float fatorEsticamentoTiro = 2.2f;
    public float raioColisao = 0.45f;

    private Transform pontoMao;
    private Transform alvoTeleguiado;
    private GameObject prefabWaterHit;
    private LayerMask layerInimigos;
    private LayerMask layerChao;
    private Transform dono;
    private float danoAgua;
    private float forcaKnockback;

    private Vector3 pontoA_Origem;
    private Vector3 direcaoLateralArco;
    private Vector3 posicaoDisparo;
    private Vector3 direcaoDisparo;
    private Vector3 escalaOriginal;
    private float alcanceMaximo;
    private float timerFaseMao;
    private bool foiDisparado;
    private bool jaCausouDano;

    private Transform[] segmentosArco;

    public void Inicializar(
        Transform pontoDaMao,
        Vector3 direcaoFrente,
        float alcanceMax,
        Transform alvo,
        GameObject vfxHit,
        LayerMask inimigos,
        LayerMask chao,
        Transform criador,
        float dano,
        float knockback)
    {
        pontoMao = pontoDaMao;
        direcaoDisparo = direcaoFrente.normalized;
        alcanceMaximo = alcanceMax;
        alvoTeleguiado = alvo;
        prefabWaterHit = vfxHit;
        layerInimigos = inimigos;
        layerChao = chao;
        dono = criador;
        danoAgua = dano;
        forcaKnockback = knockback;

        pontoA_Origem = transform.position;
        escalaOriginal = transform.localScale;
        timerFaseMao = 0f;
        foiDisparado = false;
        jaCausouDano = false;

        if (dono != null)
        {
            Vector3 diff = pontoA_Origem - dono.position;
            float lado = Vector3.Dot(diff, dono.right) >= 0f ? 1f : -1f;
            direcaoLateralArco = dono.right * lado;
        }
        else
        {
            direcaoLateralArco = transform.right;
        }

        CriarSegmentosDoArco();
    }

    private void CriarSegmentosDoArco()
    {
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();

        if (mf == null || mr == null || quantidadeSegmentosArco <= 1) return;

        segmentosArco = new Transform[quantidadeSegmentosArco];

        for (int i = 0; i < quantidadeSegmentosArco; i++)
        {
            GameObject seg = new GameObject("SegmentoArco_" + i);
            seg.transform.position = pontoA_Origem;
            seg.transform.localScale = escalaOriginal;

            MeshFilter segMf = seg.AddComponent<MeshFilter>();
            segMf.sharedMesh = mf.sharedMesh;

            MeshRenderer segMr = seg.AddComponent<MeshRenderer>();
            segMr.sharedMaterials = mr.sharedMaterials;
            segMr.shadowCastingMode = mr.shadowCastingMode;

            segmentosArco[i] = seg.transform;
        }
    }

    void Update()
    {
        if (!foiDisparado)
        {
            AtualizarArcoAgua();
        }
        else
        {
            AtualizarDisparo();
        }
    }

    private void AtualizarArcoAgua()
    {
        if (pontoMao == null)
        {
            IniciarDisparo();
            return;
        }

        timerFaseMao += Time.deltaTime;
        float tempoTotalMao = tempoEsticarAteMao + tempoRecolherArco;

        Vector3 pontoB_Mao = pontoMao.position;
        Vector3 meioReto = (pontoA_Origem + pontoB_Mao) * 0.5f;
        Vector3 pontoControleCurva = meioReto + (direcaoLateralArco * larguraDoArco) + (Vector3.up * alturaDoArco);

        float tCabeca;
        float tCauda;

        if (timerFaseMao <= tempoEsticarAteMao)
        {
            tCabeca = Mathf.Clamp01(timerFaseMao / Mathf.Max(0.01f, tempoEsticarAteMao));
            tCauda = 0f;
        }
        else
        {
            tCabeca = 1f;
            float tempoRecolhendo = timerFaseMao - tempoEsticarAteMao;
            tCauda = Mathf.Clamp01(tempoRecolhendo / Mathf.Max(0.01f, tempoRecolherArco));
        }

        Vector3 posCabeca = CalcularPontoBezier(tCabeca, pontoA_Origem, pontoControleCurva, pontoB_Mao);
        transform.position = posCabeca;

        Vector3 tangenteCabeca = CalcularTangenteBezier(tCabeca, pontoA_Origem, pontoControleCurva, pontoB_Mao);
        if (tangenteCabeca.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(tangenteCabeca.normalized);
        }

        AtualizarPosicaoSegmentos(tCauda, tCabeca, pontoA_Origem, pontoControleCurva, pontoB_Mao);

        if (timerFaseMao >= tempoTotalMao)
        {
            IniciarDisparo();
        }
    }

    private void AtualizarPosicaoSegmentos(float tInicio, float tFim, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        if (segmentosArco == null) return;

        int total = segmentosArco.Length;
        for (int i = 0; i < total; i++)
        {
            if (segmentosArco[i] == null) continue;

            float frac = (float)i / total;
            float tSeg = Mathf.Lerp(tInicio, tFim, frac);
            float tProx = Mathf.Lerp(tInicio, tFim, (float)(i + 1) / total);

            Vector3 posAtual = CalcularPontoBezier(tSeg, p0, p1, p2);
            Vector3 posProx = CalcularPontoBezier(tProx, p0, p1, p2);
            Vector3 vetorTrecho = posProx - posAtual;
            float distTrecho = vetorTrecho.magnitude;

            segmentosArco[i].position = (posAtual + posProx) * 0.5f;

            if (distTrecho > 0.001f)
            {
                segmentosArco[i].rotation = Quaternion.LookRotation(vetorTrecho.normalized);
            }

            float afinamento = Mathf.Sin(frac * Mathf.PI * 0.85f + 0.15f);
            float grossura = Mathf.Max(0.35f, afinamento) * 0.75f;
            float comprimentoZ = Mathf.Max(escalaOriginal.z * 0.8f, distTrecho * 1.35f);

            segmentosArco[i].localScale = new Vector3(
                escalaOriginal.x * grossura,
                escalaOriginal.y * grossura,
                comprimentoZ
            );
        }
    }

    private Vector3 CalcularPontoBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
    }

    private Vector3 CalcularTangenteBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
    }

    private void LimparSegmentosDoArco()
    {
        if (segmentosArco == null) return;

        for (int i = 0; i < segmentosArco.Length; i++)
        {
            if (segmentosArco[i] != null)
            {
                Destroy(segmentosArco[i].gameObject);
            }
        }
        segmentosArco = null;
    }

    private void IniciarDisparo()
    {
        foiDisparado = true;
        LimparSegmentosDoArco();

        if (pontoMao != null)
        {
            transform.position = pontoMao.position;
        }

        posicaoDisparo = transform.position;

        if (alvoTeleguiado != null)
        {
            Vector3 dirAlvo = (alvoTeleguiado.position + Vector3.up * 0.8f) - transform.position;
            if (dirAlvo.sqrMagnitude > 0.01f)
            {
                direcaoDisparo = dirAlvo.normalized;
            }
        }
        else if (dono != null)
        {
            direcaoDisparo = dono.forward;
        }

        VerificarColisaoImediata();
    }

    private void AtualizarDisparo()
    {
        if (alvoTeleguiado != null)
        {
            UniversalEnemy inimigoAlvo = alvoTeleguiado.GetComponentInParent<UniversalEnemy>();
            if (inimigoAlvo != null && inimigoAlvo.currentState != UniversalEnemy.EnemyState.Dead)
            {
                Vector3 pontoAlvo = alvoTeleguiado.position + Vector3.up * 0.8f;
                Vector3 dirDesejada = (pontoAlvo - transform.position).normalized;
                direcaoDisparo = Vector3.RotateTowards(
                    direcaoDisparo,
                    dirDesejada,
                    forcaCurvaTeleguiado * Time.deltaTime,
                    0f
                ).normalized;
            }
        }

        float passo = velocidadeTiro * Time.deltaTime;
        Vector3 proximoPonto = transform.position + direcaoDisparo * passo;

        if (direcaoDisparo.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direcaoDisparo);
        }

        transform.localScale = new Vector3(
            escalaOriginal.x * 0.75f,
            escalaOriginal.y * 0.75f,
            escalaOriginal.z * fatorEsticamentoTiro
        );

        if (ChecarImpactoNoTrajeto(passo))
        {
            return;
        }

        transform.position = proximoPonto;

        float distanciaPercorrida = Vector3.Distance(posicaoDisparo, transform.position);
        if (distanciaPercorrida >= alcanceMaximo)
        {
            EstourarAgua(transform.position);
        }
    }

    private void VerificarColisaoImediata()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, raioColisao, layerInimigos, QueryTriggerInteraction.Ignore);
        foreach (Collider col in cols)
        {
            if (TentarCausarDano(col, transform.position))
            {
                return;
            }
        }
    }

    private bool ChecarImpactoNoTrajeto(float distanciaPasso)
    {
        RaycastHit[] hitsInimigos = Physics.SphereCastAll(transform.position, raioColisao, direcaoDisparo, distanciaPasso, layerInimigos, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hitsInimigos)
        {
            if (TentarCausarDano(hit.collider, hit.point != Vector3.zero ? hit.point : transform.position))
            {
                return true;
            }
        }

        if (Physics.SphereCast(transform.position, raioColisao * 0.6f, direcaoDisparo, out RaycastHit hitChao, distanciaPasso, layerChao, QueryTriggerInteraction.Ignore))
        {
            if (dono == null || (hitChao.transform != dono && !hitChao.transform.IsChildOf(dono)))
            {
                EstourarAgua(hitChao.point);
                return true;
            }
        }

        return false;
    }

    private bool TentarCausarDano(Collider col, Vector3 pontoImpacto)
    {
        if (jaCausouDano || col == null) return false;
        if (dono != null && (col.transform == dono || col.transform.IsChildOf(dono))) return false;

        UniversalEnemy inimigo = col.GetComponentInParent<UniversalEnemy>();
        if (inimigo != null && inimigo.currentState != UniversalEnemy.EnemyState.Dead)
        {
            jaCausouDano = true;
            Transform atacanteRef = dono != null ? dono : transform;
            inimigo.ReceberDano(danoAgua, forcaKnockback, atacanteRef);
            EstourarAgua(pontoImpacto);
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (!foiDisparado || jaCausouDano) return;

        if ((layerInimigos.value & (1 << outro.gameObject.layer)) > 0)
        {
            TentarCausarDano(outro, transform.position);
        }
    }

    private void EstourarAgua(Vector3 pontoImpacto)
    {
        LimparSegmentosDoArco();

        if (prefabWaterHit != null)
        {
            Instantiate(prefabWaterHit, pontoImpacto, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        LimparSegmentosDoArco();
    }
}