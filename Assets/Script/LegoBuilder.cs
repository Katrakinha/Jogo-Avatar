using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class LegoBuilder : MonoBehaviour
{
    [Header("Controles")]
    [Tooltip("Arraste aqui a Input Action que vai acionar a construção (ex: botão de Interação).")]
    public InputActionReference controleConstruir;

    [Header("Animação do Jogador")]
    [Tooltip("Arraste o modelo do Aang (que tem o Animator) para cá.")]
    public Animator animadorJogador;
    
    [Tooltip("O nome exato do parâmetro Bool que você criou no Animator (ex: isBuilding).")]
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
            if (controleConstruir.action.IsPressed())
            {
                if (!construindoAgora) IniciarConstrucao();
            }
            else
            {
                if (construindoAgora) PararConstrucao();
            }
        }
    }

    public void IniciarConstrucao()
    {
        if (construindoAgora || estaConstruido) return;
        
        // NOVO: Liga o loop da animação do Aang
        if (animadorJogador != null) animadorJogador.SetBool(parametroConstruindo, true);
        
        rotinaPrincipal = StartCoroutine(RotinaDeConstrucao());
    }

    public void PararConstrucao()
    {
        if (finalizandoConstrucao) return; 
        
        if (rotinaPrincipal != null) StopCoroutine(rotinaPrincipal);
        construindoAgora = false;
        
        // NOVO: Desliga a animação se o jogador soltar o botão no meio
        if (animadorJogador != null) animadorJogador.SetBool(parametroConstruindo, false);
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

        // NOVO: A última peça foi atirada. O Aang já pode parar a animação e voltar à base!
        if (animadorJogador != null) animadorJogador.SetBool(parametroConstruindo, false);

        yield return new WaitForSeconds(tempoPorPeca); 

        yield return StartCoroutine(AnimarPulinhoFinal());

        estaConstruido = true;
        construindoAgora = false;
        finalizandoConstrucao = false;

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
    }
}