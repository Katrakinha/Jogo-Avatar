using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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
    public GameObject camNormalObj; 
    public GameObject miraUI;     

    [Header("Tiro e Mãos")]
    public GameObject prefabBolaDeFogo;
    public Transform maoDireita;
    public Transform maoEsquerda;
    public float forcaDoTiro = 60f; 

    [Header("Cadência de Tiro (Recoil)")]
    public float tempoEntreTiros = 0.4f; 
    private float tiroTimer = 0f;        
    
    [Header("Referências")]
    public Animator anim;

    private bool isAiming = false;
    private bool atirarComDireita = true; 
    private Transform mainCam;
    private CinemachinePanTilt panTiltMira;

    void Start()
    {
        mainCam = Camera.main.transform;
        
        if (camMiraObj != null)
        {
            panTiltMira = camMiraObj.GetComponent<CinemachinePanTilt>();
            if (panTiltMira != null) panTiltMira.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
            camMiraObj.SetActive(false);
        }
        
        if (miraUI) miraUI.SetActive(false);
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

        // ACORDO DE PAZ: Verifica se a Água está a usar a mira
        PlayerWaterBending scriptAgua = GetComponent<PlayerWaterBending>();
        bool aguaMirando = scriptAgua != null && scriptAgua.controleMirarAgua.action.IsPressed();

        if (tiroTimer > 0) tiroTimer -= Time.deltaTime;

        if (isAiming)
        {
            Transform playerRoot = scriptMovimento != null ? scriptMovimento.transform : transform.root;
            if (scriptMovimento != null) scriptMovimento.isAiming = true;
            if (scriptCombate != null) scriptCombate.enabled = false;

            if (camMiraObj && !camMiraObj.activeSelf)
            {
                camMiraObj.SetActive(true);

                if (panTiltMira == null) panTiltMira = camMiraObj.GetComponent<CinemachinePanTilt>();
                if (panTiltMira != null)
                {
                    panTiltMira.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
                    
                    // A MÁGICA: Copia o ângulo exato em que a câmera do Zelda estava olhando!
                    float cameraPan = mainCam.eulerAngles.y;
                    float cameraTilt = mainCam.eulerAngles.x;
                    
                    // Converte o ângulo vertical (Tilt) para o padrão do Cinemachine (negativo/positivo)
                    if (cameraTilt > 180f) cameraTilt -= 360f;

                    panTiltMira.PanAxis.Value = cameraPan;
                    panTiltMira.TiltAxis.Value = cameraTilt; 
                }
            }

            if (miraUI && !miraUI.activeSelf) miraUI.SetActive(true);
            
            if (panTiltMira != null)
            {
                Quaternion rotacaoAlvo = Quaternion.Euler(0f, panTiltMira.PanAxis.Value, 0f);
                playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, rotacaoAlvo, 25f * Time.deltaTime);
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
        else if (!aguaMirando) // SÓ DESLIGA SE A ÁGUA TAMBÉM NÃO ESTIVER A USAR
        {
            if (scriptMovimento != null) scriptMovimento.isAiming = false;
            if (scriptCombate != null) scriptCombate.enabled = true;
            if (camMiraObj && camMiraObj.activeSelf) camMiraObj.SetActive(false);
            if (miraUI && miraUI.activeSelf) miraUI.SetActive(false);
        }
        
        if (controleMirar.action.WasReleasedThisFrame() && !aguaMirando)
        {
            if (camNormalObj != null)
            {
                camNormalObj.SetActive(false);
                camNormalObj.SetActive(true);
            }
        }

        if (anim != null) anim.SetBool("IsAiming", isAiming);
    }

    public void DispararBolaDeFogoEvent()
    {
        // A lógica de disparo mantém-se igual
        Transform maoAtual = atirarComDireita ? maoDireita : maoEsquerda;
        atirarComDireita = !atirarComDireita; 

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); 
        Vector3 pontoAlvo;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) pontoAlvo = hit.point;
        else pontoAlvo = ray.GetPoint(100f); 

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