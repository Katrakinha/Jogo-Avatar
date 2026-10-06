using UnityEngine;
using UnityEngine.InputSystem;

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
    public PlayerAnimator scriptAnimador; // <-- NOVA REFERÊNCIA! Arrastar no Inspector.
    
    private float comboTimer = 0f;
    private bool inputBuffer = false;      
    private Rigidbody rb;

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

        Vector3 forcaAvanco = transform.forward;
        forcaAvanco.y = 0f;
        float impulso = 0f;
        
        if (comboStep == 1) impulso = data.avancoAtk1;
        else if (comboStep == 2) impulso = data.avancoAtk2;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); 
        rb.AddForce(forcaAvanco.normalized * impulso, ForceMode.Impulse);

        // --- CÓDIGO NOVO: Dispara a animação e o VFX instantaneamente! ---
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

    // --- ANIMATION EVENTS ---
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
        if (hitboxSoco != null) hitboxSoco.SetActive(false); 
        
        if (inputBuffer)
        {
            inputBuffer = false;
            Atacar();
        }
    }
}