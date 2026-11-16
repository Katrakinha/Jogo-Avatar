using UnityEngine;
using System.Collections.Generic;

public class BolaAguaKatara : MonoBehaviour
{
    [Header("Configurações de Subida e Pulso (Ataque 3)")]
    public float alturaSubida = 1.2f;
    public float tempoSubida = 0.22f;
    public float tempoDescida = 0.2f;
    public float velocidadePulso = 6f;
    public float intensidadePulso = 0.08f;

    [Header("Disparo da Bola (Ataque 4)")]
    public float velocidadeDisparo = 20f;
    public float forcaCurvaTeleguiado = 12f;
    public float fatorEsticamentoVoo = 1.4f;
    public float raioColisaoVoo = 0.65f;

    [Header("Dano da Explosão no Impacto")]
    public float raioExplosaoDano = 3.5f;
    public float multiplicadorDanoExplosao = 1.5f;

    private Vector3 posicaoChao;
    private Vector3 posicaoFlutuando;
    private Vector3 escalaBase;
    private GameObject prefabSplash;
    private GameObject prefabExplosion;
    private LayerMask layerInimigos;
    private LayerMask layerChao;
    private float danoBase;
    private float forcaKnockback;
    private Transform dono;

    private float timerMovimento;
    private bool subindo = true;
    private bool descendo = false;
    private bool foiDisparada = false;
    private bool destruida = false;

    private Vector3 posicaoInicioDisparo;
    private Vector3 direcaoDisparo;
    private Transform alvoTeleguiado;
    private float alcanceMaximoDisparo;

    public void Inicializar(
        Vector3 posNoChao,
        GameObject vfxSplash,
        GameObject vfxExplosion,
        LayerMask inimigos,
        LayerMask chao,
        float dano,
        float knockback,
        Transform criador)
    {
        posicaoChao = posNoChao;
        posicaoFlutuando = posNoChao + Vector3.up * alturaSubida;
        prefabSplash = vfxSplash;
        prefabExplosion = vfxExplosion;
        layerInimigos = inimigos;
        layerChao = chao;
        danoBase = dano;
        forcaKnockback = knockback;
        dono = criador;

        escalaBase = transform.localScale;
        transform.position = posicaoChao;
        transform.localScale = escalaBase * 0.2f;

        subindo = true;
        descendo = false;
        foiDisparada = false;
        timerMovimento = 0f;

        TocarSplashNoChao();
    }

    void Update()
    {
        if (destruida) return;

        if (foiDisparada)
        {
            AtualizarVooDisparo();
            return;
        }

        if (subindo)
        {
            timerMovimento += Time.deltaTime;
            float t = Mathf.Clamp01(timerMovimento / Mathf.Max(0.01f, tempoSubida));
            float tSuave = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(posicaoChao, posicaoFlutuando, tSuave);
            transform.localScale = Vector3.Lerp(escalaBase * 0.2f, escalaBase, tSuave);

            if (t >= 1f)
            {
                subindo = false;
            }
        }
        else if (descendo)
        {
            timerMovimento += Time.deltaTime;
            float t = Mathf.Clamp01(timerMovimento / Mathf.Max(0.01f, tempoDescida));
            float tSuave = t * t;

            transform.position = Vector3.Lerp(posicaoFlutuando, posicaoChao, tSuave);
            transform.localScale = Vector3.Lerp(escalaBase, escalaBase * 0.2f, tSuave);

            if (t >= 1f)
            {
                TocarSplashNoChao();
                destruida = true;
                Destroy(gameObject);
            }
        }
        else
        {
            float pulso = 1f + Mathf.Sin(Time.time * velocidadePulso) * intensidadePulso;
            float pulsoInverso = 1f - Mathf.Sin(Time.time * velocidadePulso) * (intensidadePulso * 0.5f);
            transform.localScale = new Vector3(
                escalaBase.x * pulso,
                escalaBase.y * pulsoInverso,
                escalaBase.z * pulso
            );
        }
    }

    public void DispararBola(Vector3 direcaoFrente, Transform alvo, float alcanceMax)
    {
        if (destruida || foiDisparada) return;

        subindo = false;
        descendo = false;
        foiDisparada = true;

        posicaoInicioDisparo = transform.position;
        alvoTeleguiado = alvo;
        alcanceMaximoDisparo = alcanceMax > 0.5f ? alcanceMax : 15f;

        if (alvoTeleguiado != null)
        {
            Vector3 dirAlvo = (alvoTeleguiado.position + Vector3.up * 0.8f) - transform.position;
            direcaoDisparo = dirAlvo.sqrMagnitude > 0.01f ? dirAlvo.normalized : direcaoFrente.normalized;
        }
        else
        {
            direcaoDisparo = direcaoFrente.sqrMagnitude > 0.01f ? direcaoFrente.normalized : Vector3.forward;
        }

        Collider[] colsImediatos = Physics.OverlapSphere(transform.position, raioColisaoVoo, layerInimigos, QueryTriggerInteraction.Ignore);
        foreach (Collider col in colsImediatos)
        {
            UniversalEnemy inimigo = col.GetComponentInParent<UniversalEnemy>();
            if (inimigo != null && inimigo.currentState != UniversalEnemy.EnemyState.Dead)
            {
                ExplodirNoImpacto(transform.position);
                return;
            }
        }
    }

    private void AtualizarVooDisparo()
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

        float passo = velocidadeDisparo * Time.deltaTime;
        Vector3 proximoPonto = transform.position + direcaoDisparo * passo;

        if (direcaoDisparo.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direcaoDisparo);
        }

        transform.localScale = new Vector3(
            escalaBase.x * 0.9f,
            escalaBase.y * 0.9f,
            escalaBase.z * fatorEsticamentoVoo
        );

        RaycastHit[] hitsInimigos = Physics.SphereCastAll(transform.position, raioColisaoVoo, direcaoDisparo, passo, layerInimigos, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hitsInimigos)
        {
            if (dono != null && (hit.transform == dono || hit.transform.IsChildOf(dono))) continue;

            UniversalEnemy inimigo = hit.collider.GetComponentInParent<UniversalEnemy>();
            if (inimigo != null && inimigo.currentState != UniversalEnemy.EnemyState.Dead)
            {
                Vector3 pontoExplosao = hit.point != Vector3.zero ? hit.point : transform.position;
                ExplodirNoImpacto(pontoExplosao);
                return;
            }
        }

        if (Physics.SphereCast(transform.position, raioColisaoVoo * 0.6f, direcaoDisparo, out RaycastHit hitChao, passo, layerChao, QueryTriggerInteraction.Ignore))
        {
            if (dono == null || (hitChao.transform != dono && !hitChao.transform.IsChildOf(dono)))
            {
                ExplodirNoImpacto(hitChao.point);
                return;
            }
        }

        transform.position = proximoPonto;

        if (Vector3.Distance(posicaoInicioDisparo, transform.position) >= alcanceMaximoDisparo)
        {
            ExplodirNoImpacto(transform.position);
        }
    }

    public void DescerBola()
    {
        if (destruida || descendo || foiDisparada) return;

        subindo = false;
        descendo = true;
        posicaoFlutuando = transform.position;
        timerMovimento = 0f;
    }

    public void ExplodirNoImpacto(Vector3 pontoExplosao)
    {
        if (destruida) return;

        destruida = true;
        transform.position = pontoExplosao;

        AplicarDanoEmArea();

        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, pontoExplosao, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (!foiDisparada || destruida) return;

        if ((layerInimigos.value & (1 << outro.gameObject.layer)) > 0)
        {
            UniversalEnemy inimigo = outro.GetComponentInParent<UniversalEnemy>();
            if (inimigo != null && inimigo.currentState != UniversalEnemy.EnemyState.Dead)
            {
                ExplodirNoImpacto(transform.position);
            }
        }
    }

    private void AplicarDanoEmArea()
    {
        Collider[] atingidos = Physics.OverlapSphere(transform.position, raioExplosaoDano, layerInimigos, QueryTriggerInteraction.Ignore);
        HashSet<UniversalEnemy> inimigosJaAtingidos = new HashSet<UniversalEnemy>();

        foreach (Collider col in atingidos)
        {
            UniversalEnemy inimigo = col.GetComponentInParent<UniversalEnemy>();
            if (inimigo != null && inimigo.currentState != UniversalEnemy.EnemyState.Dead && !inimigosJaAtingidos.Contains(inimigo))
            {
                inimigosJaAtingidos.Add(inimigo);
                Transform atacanteRef = dono != null ? dono : transform;
                inimigo.ReceberDano(danoBase * multiplicadorDanoExplosao, forcaKnockback * 1.3f, atacanteRef);
            }
        }
    }

    private void TocarSplashNoChao()
    {
        if (prefabSplash != null)
        {
            Instantiate(prefabSplash, posicaoChao, Quaternion.identity);
        }
    }
}