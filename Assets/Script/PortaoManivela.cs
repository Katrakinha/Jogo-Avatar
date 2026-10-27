using UnityEngine;
using UnityEngine.InputSystem;

public class PortaoManivela : MonoBehaviour
{
    [Header("Controles")]
    public InputActionReference controleInteracao;

    [Header("Animação do Jogador (Dobra de Ar)")]
    public Animator animadorJogador;
    public string parametroGirando = "isBendingCrank"; 

    [Header("Efeitos Visuais (VFX)")]
    [Tooltip("Arraste o efeito de Tornado que já está posicionado na sua cena para cá.")]
    public GameObject efeitoTornadoVFX;

    [Header("Mecânica da Manivela")]
    public float tempoParaAbrirTotal = 3f;
    public Vector3 eixoDeGiro = new Vector3(0, 1, 0);
    
    [Header("Referências do Portão")]
    public Transform barrasBaixo;
    public Transform pivotBarrasCima;
    public Collider colisorPortao;

    [Header("Configurações da FASE 1 (Subida)")]
    public Vector3 eixoDeSubida = new Vector3(0, 1, 0);
    public float distanciaSubida = 2f;

    [Header("Configurações da FASE 2 (Esmagamento)")]
    public Vector3 eixoDeEsmagamento = new Vector3(0, 1, 0);
    public float escalaFinalCima = 0.1f;

    // Memória para não quebrar a malha original
    private Vector3 posInicialBaixo;
    private Vector3 escalaInicialCima;

    private bool jogadorPerto = false;
    private bool portaoAberto = false;
    private bool fase1Concluida = false; 
    private float progressoGiro = 0f; 
    
    private bool interagindoAgora = false;

    // Referências do jogador pegas automaticamente no Trigger
    private Transform jogadorTransform;
    private PlayerMovement jogadorMovimento;
    private Rigidbody jogadorRb;

    void OnEnable() 
    { 
        if (controleInteracao != null) controleInteracao.action.Enable(); 
    }
    
    void OnDisable() 
    { 
        if (controleInteracao != null) controleInteracao.action.Disable(); 
    }

    void Start()
    {
        if (barrasBaixo != null) posInicialBaixo = barrasBaixo.localPosition;
        if (pivotBarrasCima != null) escalaInicialCima = pivotBarrasCima.localScale;

        // Garante que o tornado comece invisível
        if (efeitoTornadoVFX != null) efeitoTornadoVFX.SetActive(false);
    }

    void Update()
    {
        if (jogadorPerto && !portaoAberto && controleInteracao != null)
        {
            if (controleInteracao.action.IsPressed())
            {
                if (!interagindoAgora)
                {
                    interagindoAgora = true;
                    if (animadorJogador != null) animadorJogador.SetBool(parametroGirando, true);
                    
                    // LIGA O TORNADO!
                    if (efeitoTornadoVFX != null) efeitoTornadoVFX.SetActive(true);

                    // TRAVA A MOVIMENTAÇÃO
                    if (jogadorMovimento != null) jogadorMovimento.enabled = false;
                    if (jogadorRb != null) jogadorRb.linearVelocity = new Vector3(0, jogadorRb.linearVelocity.y, 0);
                }
                
                // FAZ O JOGADOR OLHAR PARA A MANIVELA
                if (jogadorTransform != null)
                {
                    Vector3 direcaoAlvo = transform.position - jogadorTransform.position;
                    direcaoAlvo.y = 0; 

                    if (direcaoAlvo != Vector3.zero)
                    {
                        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcaoAlvo);
                        jogadorTransform.rotation = Quaternion.Slerp(jogadorTransform.rotation, rotacaoAlvo, 10f * Time.deltaTime);
                    }
                }
                
                AbrirPortao();
            }
            else
            {
                if (interagindoAgora) PararInteracao();
            }
        }
        else if (interagindoAgora) 
        {
            PararInteracao();
        }
    }

    private void PararInteracao()
    {
        interagindoAgora = false;
        if (animadorJogador != null) animadorJogador.SetBool(parametroGirando, false);
        
        // DESLIGA O TORNADO
        if (efeitoTornadoVFX != null) efeitoTornadoVFX.SetActive(false);

        // DESTRAVA A MOVIMENTAÇÃO
        if (jogadorMovimento != null) jogadorMovimento.enabled = true;
    }

    private void AbrirPortao()
    {
        progressoGiro += Time.deltaTime / tempoParaAbrirTotal;
        progressoGiro = Mathf.Clamp01(progressoGiro); 

        // 1. MANIVELA
        transform.localRotation = Quaternion.Euler(eixoDeGiro * (progressoGiro * 360f));

        // 2. FASE 1 (0 a 180º)
        if (progressoGiro <= 0.5f)
        {
            float tempoSubida = progressoGiro / 0.5f; 
            
            if (barrasBaixo != null)
            {
                barrasBaixo.localPosition = posInicialBaixo + (eixoDeSubida * (distanciaSubida * tempoSubida));
            }
        }
        // 3. FASE 2 (180º a 360º)
        else
        {
            if (!fase1Concluida && barrasBaixo != null)
            {
                barrasBaixo.gameObject.SetActive(false);
                fase1Concluida = true;
            }

            float tempoEsmagamento = (progressoGiro - 0.5f) / 0.5f;

            if (pivotBarrasCima != null)
            {
                Vector3 escalaAlvo = new Vector3(
                    escalaInicialCima.x * (eixoDeEsmagamento.x != 0 ? escalaFinalCima : 1f),
                    escalaInicialCima.y * (eixoDeEsmagamento.y != 0 ? escalaFinalCima : 1f),
                    escalaInicialCima.z * (eixoDeEsmagamento.z != 0 ? escalaFinalCima : 1f)
                );

                pivotBarrasCima.localScale = Vector3.Lerp(escalaInicialCima, escalaAlvo, tempoEsmagamento);
            }
        }

        // TERMINOU DE ABRIR
        if (progressoGiro >= 1f)
        {
            portaoAberto = true;
            PararInteracao(); 

            if (colisorPortao != null)
            {
                colisorPortao.enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !portaoAberto) 
        {
            jogadorPerto = true;
            
            jogadorTransform = other.transform;
            jogadorMovimento = other.GetComponent<PlayerMovement>();
            jogadorRb = other.GetComponent<Rigidbody>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            jogadorPerto = false;
        }
    }
}