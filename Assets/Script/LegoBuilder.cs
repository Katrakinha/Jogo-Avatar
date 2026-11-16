using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class LegoBuilder : MonoBehaviour
{
    [Header("Controles e Permissão por ID")]
    public InputActionReference controleConstruir;
    public PersonagemID quemPodeConstruir = PersonagemID.QualquerUm;
    public List<IdentidadePersonagem> personagensDaCena = new List<IdentidadePersonagem>();
    public float raioParaConstruir = 4.5f;

    [Header("Animação do Jogador")]
    public Animator animadorJogador;
    public string parametroConstruindo = "isBuilding";

    [Header("Configurações da Construção")]
    public float tempoPorPeca = 0.4f;
    public float intervaloEntrePecas = 0.05f;
    public float alturaDoPuloConstrucao = 1.5f;

    [Header("Efeito Final (O Pulinho)")]
    public float alturaPuloFinal = 0.3f;
    public float tempoPuloFinal = 0.25f;

    [Header("Formato da Pilha (A Bagunça)")]
    public float raioDaPilha = 2f;
    public float espacamentoMinimo = 0.3f;
    public Vector3 deslocamentoDaPilha = Vector3.zero;

    [Header("Efeito 'Peixe Fora D'água'")]
    public float alturaMinimaDoChao = 0.3f;
    public float alturaPuloIdle = 0.4f;
    public float velPuloIdleBase = 12f;

    [Header("Referências")]
    public Collider colisorFinal;

    private class PecaLego
    {
        public Transform transform;
        public Vector3 posOriginal;
        public Quaternion rotOriginal;

        public Vector3 posBaguncadaBase;
        public float offsetTempo;
        public float velPulo;
        public bool indoProLugar = false;
    }

    private List<PecaLego> listaDePecas = new List<PecaLego>();
    public bool estaConstruido = false;

    private bool construindoAgora = false;
    private bool finalizandoConstrucao = false;
    private Coroutine rotinaPrincipal;
    private IdentidadePersonagem construtorAtual;
    private Animator animadorAtivo;

    void OnEnable()
    {
        if (controleConstruir != null) controleConstruir.action.Enable();
    }

    void OnDisable()
    {
        if (controleConstruir != null) controleConstruir.action.Disable();
    }

    void Start()
    {
        if (colisorFinal == null) colisorFinal = GetComponent<Collider>();
        if (colisorFinal != null) colisorFinal.enabled = false;

        if (personagensDaCena.Count == 0)
        {
            IdentidadePersonagem[] encontrados = FindObjectsByType<IdentidadePersonagem>(FindObjectsSortMode.None);
            personagensDaCena.AddRange(encontrados);
        }

        MemorizarEBaguncar();
    }

    private void MemorizarEBaguncar()
    {
        MeshRenderer[] todasAsPecas = GetComponentsInChildren<MeshRenderer>();
        Vector3 centroDaBaguncaMundo = transform.position + deslocamentoDaPilha;

        List<Vector3> posicoesOcupadasNoChao = new List<Vector3>();

        foreach (MeshRenderer pecaRenderer in todasAsPecas)
        {
            Transform peca = pecaRenderer.transform;
            PecaLego dados = new PecaLego();
            dados.transform = peca;

            dados.posOriginal = peca.localPosition;
            dados.rotOriginal = peca.localRotation;

            Vector3 posicaoMundoEncontrada = Vector3.zero;
            bool posicaoValida = false;
            int tentativas = 0;

            while (!posicaoValida && tentativas < 30)
            {
                Vector2 circuloHorizontal = Random.insideUnitCircle * raioDaPilha;

                posicaoMundoEncontrada = new Vector3(
                    centroDaBaguncaMundo.x + circuloHorizontal.x,
                    centroDaBaguncaMundo.y + Random.Range(alturaMinimaDoChao, alturaMinimaDoChao + 0.05f),
                    centroDaBaguncaMundo.z + circuloHorizontal.y
                );

                posicaoValida = true;
                foreach (Vector3 posOcupada in posicoesOcupadasNoChao)
                {
                    if (Vector3.Distance(posicaoMundoEncontrada, posOcupada) < espacamentoMinimo)
                    {
                        posicaoValida = false;
                        break;
                    }
                }
                tentativas++;
            }

            posicoesOcupadasNoChao.Add(posicaoMundoEncontrada);

            peca.position = posicaoMundoEncontrada;
            peca.rotation = Random.rotation;

            dados.posBaguncadaBase = peca.localPosition;

            dados.offsetTempo = Random.Range(0f, 100f);
            dados.velPulo = Random.Range(velPuloIdleBase - 3f, velPuloIdleBase + 3f);
            dados.indoProLugar = false;

            listaDePecas.Add(dados);
        }
    }

    void Update()
    {
        if (!estaConstruido)
        {
            foreach (PecaLego peca in listaDePecas)
            {
                if (!peca.indoProLugar)
                {
                    Vector3 posAnimada = peca.posBaguncadaBase;
                    float quique = Mathf.Abs(Mathf.Sin(Time.time * peca.velPulo + peca.offsetTempo));
                    posAnimada.y += quique * alturaPuloIdle;

                    peca.transform.localPosition = posAnimada;
                    peca.transform.Rotate(Vector3.up * (peca.velPulo * 2f * Time.deltaTime), Space.World);
                }
            }
        }

        if (controleConstruir != null && !estaConstruido && !finalizandoConstrucao)
        {
            IdentidadePersonagem liderValido = ObterConstrutorAtivoNaArea();

            if (controleConstruir.action.IsPressed() && liderValido != null)
            {
                if (!construindoAgora)
                {
                    IniciarConstrucao(liderValido);
                }
                else if (construtorAtual != liderValido)
                {
                    PararConstrucao();
                }
            }
            else
            {
                if (construindoAgora)
                {
                    PararConstrucao();
                }
            }
        }
    }

    private IdentidadePersonagem ObterConstrutorAtivoNaArea()
    {
        Vector3 centroPilha = transform.position + deslocamentoDaPilha;
        Vector2 centroPilhaPlano = new Vector2(centroPilha.x, centroPilha.z);

        for (int i = 0; i < personagensDaCena.Count; i++)
        {
            IdentidadePersonagem p = personagensDaCena[i];
            if (p == null || !p.isPlayerControlled) continue;

            if (quemPodeConstruir != PersonagemID.QualquerUm && p.idDoPersonagem != quemPodeConstruir)
            {
                continue;
            }

            Vector2 posPlayerPlano = new Vector2(p.transform.position.x, p.transform.position.z);
            float distanciaPlana = Vector2.Distance(posPlayerPlano, centroPilhaPlano);

            if (distanciaPlana <= raioParaConstruir)
            {
                return p;
            }
        }

        return null;
    }

    public void IniciarConstrucao(IdentidadePersonagem lider)
    {
        if (construindoAgora || estaConstruido) return;

        construtorAtual = lider;
        animadorAtivo = lider != null ? lider.GetComponentInChildren<Animator>() : animadorJogador;
        if (animadorAtivo == null) animadorAtivo = animadorJogador;

        if (animadorAtivo != null) animadorAtivo.SetBool(parametroConstruindo, true);

        rotinaPrincipal = StartCoroutine(RotinaDeConstrucao());
    }

    public void PararConstrucao()
    {
        if (finalizandoConstrucao) return;

        if (rotinaPrincipal != null) StopCoroutine(rotinaPrincipal);
        construindoAgora = false;

        if (animadorAtivo != null) animadorAtivo.SetBool(parametroConstruindo, false);
        if (animadorJogador != null) animadorJogador.SetBool(parametroConstruindo, false);

        construtorAtual = null;
    }

    private IEnumerator RotinaDeConstrucao()
    {
        construindoAgora = true;

        int pecasRestantes = 0;
        foreach (PecaLego p in listaDePecas) if (!p.indoProLugar) pecasRestantes++;

        foreach (PecaLego peca in listaDePecas)
        {
            if (peca.indoProLugar) continue;

            StartCoroutine(AnimarPeca(peca));
            pecasRestantes--;

            if (pecasRestantes > 0)
            {
                yield return new WaitForSeconds(intervaloEntrePecas);
            }
        }

        finalizandoConstrucao = true;

        if (animadorAtivo != null) animadorAtivo.SetBool(parametroConstruindo, false);
        if (animadorJogador != null) animadorJogador.SetBool(parametroConstruindo, false);

        yield return new WaitForSeconds(tempoPorPeca);

        yield return StartCoroutine(AnimarPulinhoFinal());

        estaConstruido = true;
        construindoAgora = false;
        finalizandoConstrucao = false;
        construtorAtual = null;

        if (colisorFinal != null) colisorFinal.enabled = true;
    }

    private IEnumerator AnimarPeca(PecaLego peca)
    {
        peca.indoProLugar = true;
        float timer = 0f;

        Vector3 posInicial = peca.transform.localPosition;
        Quaternion rotInicial = peca.transform.localRotation;

        while (timer < tempoPorPeca)
        {
            timer += Time.deltaTime;
            float progresso = timer / tempoPorPeca;
            float smoothProgresso = Mathf.SmoothStep(0f, 1f, progresso);

            peca.transform.localRotation = Quaternion.Slerp(rotInicial, peca.rotOriginal, smoothProgresso);

            Vector3 posAtual = Vector3.Lerp(posInicial, peca.posOriginal, smoothProgresso);
            posAtual.y += Mathf.Sin(progresso * Mathf.PI) * alturaDoPuloConstrucao;

            peca.transform.localPosition = posAtual;

            yield return null;
        }

        peca.transform.localPosition = peca.posOriginal;
        peca.transform.localRotation = peca.rotOriginal;
    }

    private IEnumerator AnimarPulinhoFinal()
    {
        float timer = 0f;
        Vector3 posInicial = transform.localPosition;

        while (timer < tempoPuloFinal)
        {
            timer += Time.deltaTime;
            float progresso = timer / tempoPuloFinal;

            Vector3 posAtual = posInicial;
            posAtual.y += Mathf.Sin(progresso * Mathf.PI) * alturaPuloFinal;

            transform.localPosition = posAtual;

            yield return null;
        }

        transform.localPosition = posInicial;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + deslocamentoDaPilha, raioDaPilha);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + deslocamentoDaPilha, raioParaConstruir);
    }
}