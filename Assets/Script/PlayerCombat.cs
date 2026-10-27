using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // <-- ADICIONADO: Necessário para a rotina de suavização!

public class PlayerCombat : MonoBehaviour
{
    [Header("Banco de Dados")]
    public AvatarCombatData data;

    [Header("Controles")]
    public InputActionReference controleAtacar; 

    [Header("Estado do Combate")]
    public int comboStep = 0; 
    public bool isAttacking = false;

    [Header("Hitboxes")]
    public GameObject hitboxSoco; 

    [Header("Referências")]
    public PlayerMovement scriptMovimento; 
    public PlayerAnimator scriptAnimador; 
    
    private float comboTimer = 0f;
    private bool inputBuffer = false;      
    private Rigidbody rb;
    private Coroutine rotinaImpulso; // Guarda o dash atual

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (hitboxSoco != null) hitboxSoco.SetActive(false); 
    }

    void OnEnable() { controleAtacar.action.Enable(); }
    void OnDisable() { controleAtacar.action.Disable(); }

    void Update()
    {
        if (controleAtacar.action.WasPressedThisFrame())
        {
            if (scriptMovimento != null && scriptMovimento.isEarthSlamming) return; 
            inputBuffer = true; 
        }

        if (!isAttacking) 
        {
            if (comboStep > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0) 
                {
                    ZerarCombo(); 
                }
            }

            if (inputBuffer)
            {
                inputBuffer = false; 
                Atacar();
            }
        }
    }

    private void Atacar()
    {
        if (comboStep >= 4) comboStep = 0; 

        comboStep++;
        isAttacking = true; 
        comboTimer = data.tempoJanelaCombo; 

        if (scriptMovimento != null)
        {
            scriptMovimento.isAttacking = true; 
        }

        // Para qualquer dash que ainda esteja a acontecer antes de começar o próximo
        if (rotinaImpulso != null) StopCoroutine(rotinaImpulso);
        
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); 

        if (scriptAnimador != null)
        {
            scriptAnimador.DispararAnimacaoDeAtaque(comboStep);
        }
    }

    private void ZerarCombo()
    {
        comboStep = 0;
        inputBuffer = false;
        if (hitboxSoco != null) hitboxSoco.SetActive(false);
    }

    // ==========================================
    // ANIMATION EVENTS 
    // ==========================================
    
    public void AnimEvent_ImpulsoAtaque()
    {
        // Em vez de dar um tranco instantâneo, inicia o deslize suave!
        if (rotinaImpulso != null) StopCoroutine(rotinaImpulso);
        rotinaImpulso = StartCoroutine(RotinaDashSuave(data.avancoAtaque));
    }

    // --- A MÁGICA DA SUAVIZAÇÃO ACONTECE AQUI ---
    private IEnumerator RotinaDashSuave(float forcaMax)
    {
        float tempoDoDash = 0.15f; // Duração do avanço (150 milissegundos)
        float timer = 0f;
        
        Vector3 direcao = transform.forward;
        direcao.y = 0f;
        direcao.Normalize();

        while (timer < tempoDoDash)
        {
            // Começa rápido (forcaMax) e vai diminuindo até 0 suavemente
            float velocidadeAtual = Mathf.Lerp(forcaMax, 0f, timer / tempoDoDash);
            
            rb.linearVelocity = new Vector3(direcao.x * velocidadeAtual, rb.linearVelocity.y, direcao.z * velocidadeAtual);
            
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate(); // Usa o tempo da física para não engasgar!
        }
    }

    public void AnimEvent_LigarHitbox() 
    { 
        if (hitboxSoco != null) hitboxSoco.SetActive(true); 
    }

    public void AnimEvent_DesligarHitbox() 
    { 
        if (hitboxSoco != null) hitboxSoco.SetActive(false); 
    }

    public void AnimEvent_FimDoAtaque() 
    { 
        isAttacking = false; 
        
        if (scriptMovimento != null)
        {
            scriptMovimento.isAttacking = false; 
        }
        
        if (hitboxSoco != null) hitboxSoco.SetActive(false); 
        
        if (inputBuffer)
        {
            inputBuffer = false;
            Atacar();
        }
    }
}