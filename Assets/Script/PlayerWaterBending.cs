using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Cinemachine;
using System.Collections; // Necessário para a Corrotina de queda

public class PlayerWaterBending : MonoBehaviour
{
    [Header("Banco de Dados")]
    public AvatarWaterData data;

    [Header("Referências Entre Scripts")]
    public PlayerMovement scriptMovimento; 
    public PlayerCombat scriptCombate;     

    [Header("Inputs")]
    public InputActionReference controleMirarAgua;
    public InputActionReference controleCriarGelo;

    [Header("Câmera e UI")]
    public GameObject camMiraObj; 
    public GameObject camNormalObj; 
    public GameObject miraUI;     

    [Header("Prefabs do Gelo")]
    public GameObject prefabPilarGelo; 
    public GameObject prefabHolograma; 
    
    [Header("Configurações do Holograma")]
    public float velocidadeAnimacaoHolo = 1.5f; 
    public bool cuboComPivotNoCentro = true;    

    [Header("Esfera de Água Visual (NOVO)")]
    [Tooltip("O Prefab da Esfera com o material de água liso")]
    public GameObject prefabEsferaAgua;
    [Tooltip("O ponto (Empty) na mão ou perto do Aang onde a água vai flutuar")]
    public Transform pontoEsferaAgua;
    [Tooltip("De quão alto a água vem caindo ao começar a mirar (ex: 2 metros acima)")]
    public float alturaOrigemEsfera = 2f;
    [Tooltip("Velocidade que a esfera desce até a mão")]
    public float velocidadePuxaoEsfera = 8f;

    [Header("Referências")]
    public Animator anim;

    private bool isAimingWater = false;
    private Transform mainCam;
    private CinemachinePanTilt panTiltMira;
    private Queue<GameObject> pilaresAtivos = new Queue<GameObject>();
    
    private GameObject hologramaInstancia; 
    private GameObject esferaInstancia; // A bolota de água atual
    private bool usouHabilidade = false; // Controle para saber se atirou
    
    private bool mirandoNaAgua = false;
    private Vector3 pontoAlvoAgua;
    private Vector3 normalAlvoAgua;

    void Start()
    {
        mainCam = Camera.main.transform;
        if (camMiraObj != null) panTiltMira = camMiraObj.GetComponent<CinemachinePanTilt>();
    }

    void OnEnable()
    {
        if (controleMirarAgua != null) controleMirarAgua.action.Enable();
        if (controleCriarGelo != null) controleCriarGelo.action.Enable();
    }

    void OnDisable()
    {
        if (controleMirarAgua != null) controleMirarAgua.action.Disable();
        if (controleCriarGelo != null) controleCriarGelo.action.Disable();
    }

    void Update()
    {
        if (controleMirarAgua == null) return; 
        
        isAimingWater = controleMirarAgua.action.IsPressed();

        PlayerFireBending scriptFogo = GetComponent<PlayerFireBending>();
        bool fogoMirando = scriptFogo != null && scriptFogo.controleMirar != null && scriptFogo.controleMirar.action.IsPressed();

        if (isAimingWater)
        {
            if (scriptMovimento != null) scriptMovimento.isAiming = true;
            if (scriptCombate != null) scriptCombate.enabled = false;

            Transform playerRoot = scriptMovimento != null ? scriptMovimento.transform : transform.root;

            if (camMiraObj && !camMiraObj.activeSelf)
            {
                camMiraObj.SetActive(true);

                if (panTiltMira == null) panTiltMira = camMiraObj.GetComponent<CinemachinePanTilt>();
                if (panTiltMira != null)
                {
                    panTiltMira.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
                    float cameraPan = mainCam.eulerAngles.y;
                    float cameraTilt = mainCam.eulerAngles.x;
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

            AtualizarHolograma();

            // --- LÓGICA DA ESFERA DE ÁGUA (Puxando) ---
            if (esferaInstancia == null && prefabEsferaAgua != null && pontoEsferaAgua != null)
            {
                // Instancia a esfera mais alta para dar o efeito de puxar de cima
                Vector3 posicaoAlta = pontoEsferaAgua.position + (Vector3.up * alturaOrigemEsfera);
                esferaInstancia = Instantiate(prefabEsferaAgua, posicaoAlta, Quaternion.identity);
                usouHabilidade = false; // Reset da ação
            }

            // Move a esfera suavemente para a mão se ainda não atirou
            if (esferaInstancia != null && !usouHabilidade)
            {
                esferaInstancia.SetActive(true);
                esferaInstancia.transform.position = Vector3.Lerp(esferaInstancia.transform.position, pontoEsferaAgua.position, Time.deltaTime * velocidadePuxaoEsfera);
            }

            // LÊ O R2 COM SEGURANÇA
            if (controleCriarGelo != null && controleCriarGelo.action.WasPressedThisFrame())
            {
                Debug.Log("+++ [ÁGUA] Botão R2 pressionado! +++");
                if (anim != null) anim.SetTrigger("CastWater"); 
                
                // O Jogador atirou! Esconde a esfera imediatamente para abrir espaço pro VFX
                usouHabilidade = true;
                if (esferaInstancia != null) esferaInstancia.SetActive(false);

                TentarCriarPilarDeGelo();
            }
        }
        else if (!fogoMirando) 
        {
            if (hologramaInstancia != null) hologramaInstancia.SetActive(false);
            mirandoNaAgua = false;

            if (scriptMovimento != null) scriptMovimento.isAiming = false;
            if (scriptCombate != null) scriptCombate.enabled = true;
            if (camMiraObj && camMiraObj.activeSelf) camMiraObj.SetActive(false);
            if (miraUI && miraUI.activeSelf) miraUI.SetActive(false);
        }

        // SOLTOU O BOTÃO DE MIRAR
        if (controleMirarAgua.action.WasReleasedThisFrame() && !fogoMirando)
        {
            if (camNormalObj != null)
            {
                camNormalObj.SetActive(false);
                camNormalObj.SetActive(true);
            }

            // --- LÓGICA DA ESFERA DE ÁGUA (Caindo ou Limpando) ---
            if (esferaInstancia != null)
            {
                // Se soltou a mira sem fazer nada, deixa a água cair no chão
                if (!usouHabilidade && esferaInstancia.activeSelf)
                {
                    StartCoroutine(RotinaGotaCaindo(esferaInstancia));
                }
                else
                {
                    // Se já tinha atirado (e ela estava invisível), só destrói pra criar outra depois
                    Destroy(esferaInstancia); 
                }
                esferaInstancia = null; // Desvincula para o script criar uma nova na próxima mira
            }
        }

        if (anim != null) anim.SetBool("IsAimingWater", isAimingWater); 
    }

    private void AtualizarHolograma()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        if (Physics.Raycast(ray, out RaycastHit hit, data.alcanceDaMira))
        {
            if (hit.collider.CompareTag("Agua"))
            {
                mirandoNaAgua = true;
                pontoAlvoAgua = hit.point;
                normalAlvoAgua = hit.normal;

                if (hologramaInstancia == null && prefabHolograma != null)
                {
                    hologramaInstancia = Instantiate(prefabHolograma);
                }

                if (hologramaInstancia != null)
                {
                    hologramaInstancia.SetActive(true);
                    
                    float progressoLoop = Mathf.Repeat(Time.time * velocidadeAnimacaoHolo, 1f);
                    float alturaAnimada = Mathf.Max(0.05f, data.escalaDoPilar.y * progressoLoop);

                    hologramaInstancia.transform.rotation = Quaternion.FromToRotation(Vector3.up, normalAlvoAgua);
                    hologramaInstancia.transform.localScale = new Vector3(data.escalaDoPilar.x, alturaAnimada, data.escalaDoPilar.z);
                    
                    if (cuboComPivotNoCentro)
                    {
                        hologramaInstancia.transform.position = pontoAlvoAgua + (normalAlvoAgua * (alturaAnimada / 2f));
                    }
                    else
                    {
                        hologramaInstancia.transform.position = pontoAlvoAgua;
                    }
                }
                return; 
            }
        }
        
        mirandoNaAgua = false;
        if (hologramaInstancia != null) hologramaInstancia.SetActive(false);
    }

    private void TentarCriarPilarDeGelo()
    {
        Debug.Log("+++ [ÁGUA] Tentando criar bloco. Está na água? " + mirandoNaAgua + " +++");
        
        if (mirandoNaAgua)
        {
            if (prefabPilarGelo == null)
            {
                Debug.LogWarning("+++ [ÁGUA] ERRO: Prefab do Pilar sumiu do Inspector! +++");
                return;
            }

            if (pilaresAtivos.Count >= data.maxPilaresSimultaneos)
            {
                GameObject pilarAntigo = pilaresAtivos.Dequeue();
                if (pilarAntigo != null) Destroy(pilarAntigo);
            }

            Quaternion rotacaoBase = Quaternion.FromToRotation(Vector3.up, normalAlvoAgua);
            GameObject novoPilar = Instantiate(prefabPilarGelo, pontoAlvoAgua, rotacaoBase);
            
            PilarDeGelo scriptPilar = novoPilar.GetComponent<PilarDeGelo>();
            if (scriptPilar != null) 
            {
                scriptPilar.IniciarSubida(data.escalaDoPilar, data.tempoDeSubida);
                Debug.Log("+++ [ÁGUA] Pilar criado e subindo perfeitamente! +++");
            }
            else
            {
                Debug.LogWarning("+++ [ÁGUA] O bloco nasceu, mas o script 'PilarDeGelo' sumiu dele! +++");
            }

            pilaresAtivos.Enqueue(novoPilar);
        }
    }

    // --- CORROTINA DA QUEDA DA ÁGUA ---
    private IEnumerator RotinaGotaCaindo(GameObject gota)
    {
        Vector3 posInicial = gota.transform.position;
        Vector3 posChao = posInicial + (Vector3.down * 1.5f); // Cai um metro e meio
        Vector3 escalaInicial = gota.transform.localScale;

        float timer = 0f;
        float duracaoQueda = 0.3f; // Cai bem rápido!

        while (timer < duracaoQueda)
        {
            timer += Time.deltaTime;
            float t = timer / duracaoQueda;

            if (gota != null)
            {
                // A água cai e vai encolhendo ao mesmo tempo
                gota.transform.position = Vector3.Lerp(posInicial, posChao, t);
                gota.transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t);
            }
            yield return null;
        }

        if (gota != null) Destroy(gota); // Limpa a memória
    }
}