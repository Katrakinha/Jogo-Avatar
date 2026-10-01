using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFireBending : MonoBehaviour
{
    [Header("Referências Entre Scripts")]
    public PlayerMovement scriptMovimento; 
    public PlayerCombat scriptCombate;     

    [Header("Inputs")]
    public InputActionReference controleMirar;
    public InputActionReference controleAtirar;

    [Header("Câmera e UI (Mira)")]
    public GameObject camMiraObj; 
    public GameObject camNormalObj; // <--- NOVA VARIÁVEL: A tua câmera de exploração
    public GameObject miraUI;     

    [Header("Tiro e Mãos")]
    public GameObject prefabBolaDeFogo;
    public Transform maoDireita;
    public Transform maoEsquerda;
    public float forcaDoTiro = 60f; // Aumentei um bocadinho para a bola ser rápida!

    [Header("Cadência de Tiro (Recoil)")]
    public float tempoEntreTiros = 0.4f; 
    private float tiroTimer = 0f;        
    
    [Header("Referências")]
    public Animator anim;

    private bool isAiming = false;
    private bool atirarComDireita = true; 
    private Transform mainCam;

    void Start()
    {
        mainCam = Camera.main.transform;
        
        if(camMiraObj) camMiraObj.SetActive(false);
        if(miraUI) miraUI.SetActive(false);
    }

    void OnEnable()
    {
        controleMirar.action.Enable();
        controleAtirar.action.Enable();
    }

    void OnDisable()
    {
        controleMirar.action.Disable();
        controleAtirar.action.Disable();
    }

    void Update()
    {
        isAiming = controleMirar.action.IsPressed();

        if (scriptMovimento != null) scriptMovimento.isAiming = isAiming;
        if (scriptCombate != null) scriptCombate.enabled = !isAiming;

        if (tiroTimer > 0) tiroTimer -= Time.deltaTime;

        if (isAiming)
        {
            if(camMiraObj && !camMiraObj.activeSelf) camMiraObj.SetActive(true);
            if(miraUI && !miraUI.activeSelf) miraUI.SetActive(true);
            
            Vector3 camForward = mainCam.forward;
            camForward.y = 0f; 
            
            if (camForward != Vector3.zero)
            {
                Transform objetoParaRodar = transform.parent != null ? transform.parent : transform;
                objetoParaRodar.rotation = Quaternion.Slerp(
                    objetoParaRodar.rotation, 
                    Quaternion.LookRotation(camForward), 
                    25f * Time.deltaTime 
                );
            }

            if (controleAtirar.action.WasPressedThisFrame() && tiroTimer <= 0f)
            {
                if (anim != null) 
                {
                    anim.SetBool("TiroDireita", atirarComDireita); 
                    anim.SetTrigger("TiroFogo");
                }
                tiroTimer = tempoEntreTiros; 
            }
        }
        else
        {
            if(camMiraObj && camMiraObj.activeSelf) camMiraObj.SetActive(false);
            if(miraUI && miraUI.activeSelf) miraUI.SetActive(false);
        }
        
        // A MAGIA ACONTECE AQUI: No exato milissegundo em que largas o L2
        if (controleMirar.action.WasReleasedThisFrame())
        {
            if (camNormalObj != null)
            {
                // Desliga e liga a câmera para resetar o eixo dela para as costas do Aang
                camNormalObj.SetActive(false);
                camNormalObj.SetActive(true);
            }
        }

        if (anim != null) anim.SetBool("IsAiming", isAiming);
    }

    public void DispararBolaDeFogoEvent()
    {
        Transform maoAtual = atirarComDireita ? maoDireita : maoEsquerda;
        atirarComDireita = !atirarComDireita; 

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); 
        Vector3 pontoAlvo;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            pontoAlvo = hit.point;
        else
            pontoAlvo = ray.GetPoint(100f); 

        if (prefabBolaDeFogo != null && maoAtual != null)
        {
            GameObject bolaDeFogo = Instantiate(prefabBolaDeFogo, maoAtual.position, Quaternion.identity);
            
            Vector3 direcaoTiro = (pontoAlvo - maoAtual.position).normalized;
            bolaDeFogo.transform.forward = direcaoTiro; 

            Rigidbody rbFogo = bolaDeFogo.GetComponent<Rigidbody>();
            if (rbFogo != null) rbFogo.AddForce(direcaoTiro * forcaDoTiro, ForceMode.Impulse);
        }
    }
}