using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWaterHook : MonoBehaviour
{
    [Header("Inputs")]
    public InputActionReference controleMirar;
    public InputActionReference controleAcao;

    [Header("Referências do Jogador")]
    public Animator anim;
    public PlayerMovement scriptMovimento;
    public Transform maoChicote; 
    
    [Header("UI da Mira")]
    public GameObject uiMiraGancho; 

    [Header("Configurações do Chicote (Objeto 3D)")]
    [Tooltip("Arraste aqui o Cilindro que vai servir de chicote d'água")]
    public Transform chicote3D;

    // Estado Interno
    private bool isAiming = false;
    private bool isHooked = false;
    private GanchoModular alvoAtual;

    void Start()
    {
        if (uiMiraGancho != null) uiMiraGancho.SetActive(false);
        
        // Garante que o chicote comece invisível
        if (chicote3D != null) chicote3D.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (controleMirar != null) controleMirar.action.Enable();
        if (controleAcao != null) controleAcao.action.Enable();
    }

    void OnDisable()
    {
        if (controleMirar != null) controleMirar.action.Disable();
        if (controleAcao != null) controleAcao.action.Disable();
    }

    void Update()
    {
        if (controleMirar == null || controleAcao == null) return;

        isAiming = controleMirar.action.IsPressed();

        // ESTADO 1: PRESO NO MINIGAME (PUXANDO O CHICOTE)
        if (isHooked && alvoAtual != null)
        {
            AtualizarPosicaoChicote3D();

            GirarParaAlvo(alvoAtual.transform.position);

            if (controleAcao.action.WasPressedThisFrame())
            {
                alvoAtual.ReceberForca();
            }
            return; 
        }

        // ESTADO 2: MIRANDO PROCURANDO UM GANCHO
        if (isAiming)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            GanchoModular alvoDetectado = null;

            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                if (hit.collider.CompareTag("GanchoTeia"))
                {
                    alvoDetectado = hit.collider.GetComponent<GanchoModular>();
                }
            }

            if (uiMiraGancho != null) uiMiraGancho.SetActive(alvoDetectado != null);

            if (alvoDetectado != null && controleAcao.action.WasPressedThisFrame())
            {
                IniciarMinigame(alvoDetectado);
            }
        }
        else
        {
            if (uiMiraGancho != null) uiMiraGancho.SetActive(false);
        }
    }

    // A MÁGICA MATEMÁTICA DO CILINDRO ACONTECE AQUI
    private void AtualizarPosicaoChicote3D()
    {
        if (chicote3D == null || maoChicote == null || alvoAtual == null) return;

        Vector3 posInicial = maoChicote.position;
        Vector3 posFinal = alvoAtual.pontoDeFixacao.position;
        float distancia = Vector3.Distance(posInicial, posFinal);

        // 1. Posição: Coloca o cilindro exatamente no meio do caminho
        chicote3D.position = (posInicial + posFinal) / 2f;

        // 2. Rotação: Aponta para a caixa (o eixo Y do cilindro é a altura, por isso viramos 90 graus no X)
        Vector3 direcao = posFinal - posInicial;
        if (direcao != Vector3.zero)
        {
            chicote3D.rotation = Quaternion.LookRotation(direcao) * Quaternion.Euler(90f, 0f, 0f);
        }

        // 3. Escala: Estica o Y para o tamanho exato da distância (dividido por 2 porque o cilindro base já tem altura 2)
        chicote3D.localScale = new Vector3(chicote3D.localScale.x, distancia, chicote3D.localScale.z);
    }

    private void IniciarMinigame(GanchoModular alvo)
    {
        isHooked = true;
        alvoAtual = alvo;
        if (uiMiraGancho != null) uiMiraGancho.SetActive(false);

        if (scriptMovimento != null) scriptMovimento.enabled = false;
        
        // Liga o chicote 3D!
        if (chicote3D != null) chicote3D.gameObject.SetActive(true);

        if (anim != null) anim.SetTrigger("WhipCast");

        alvo.ComecarMinigame(this);
    }

    public void VencerMinigame()
    {
        isHooked = false;
        if (chicote3D != null) chicote3D.gameObject.SetActive(false);
        if (scriptMovimento != null) scriptMovimento.enabled = true;
        
        if (anim != null) anim.SetTrigger("WhipSuccess");
        alvoAtual = null;
    }

    public void FalharMinigame()
    {
        isHooked = false;
        if (chicote3D != null) chicote3D.gameObject.SetActive(false);
        if (scriptMovimento != null) scriptMovimento.enabled = true;

        if (anim != null) anim.SetTrigger("WhipFail");
        
        if (alvoAtual != null) alvoAtual.CancelarMinigame();
        alvoAtual = null;
    }

    private void GirarParaAlvo(Vector3 posicaoAlvo)
    {
        Vector3 direcao = posicaoAlvo - transform.position;
        direcao.y = 0;
        if (direcao != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direcao), 15f * Time.deltaTime);
        }
    }
}