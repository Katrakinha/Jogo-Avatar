using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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
    private Coroutine rotinaImpulso;

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

    public void AnimEvent_ImpulsoAtaque()
    {
        if (rotinaImpulso != null) StopCoroutine(rotinaImpulso);
        rotinaImpulso = StartCoroutine(RotinaDashSuave(data.avancoAtaque));
    }

    private IEnumerator RotinaDashSuave(float forcaMax)
    {
        float tempoDoDash = 0.15f;
        float timer = 0f;
        
        Vector3 direcao = transform.forward;
        direcao.y = 0f;
        direcao.Normalize();

        while (timer < tempoDoDash)
        {
            float velocidadeAtual = Mathf.Lerp(forcaMax, 0f, timer / tempoDoDash);
            
            rb.linearVelocity = new Vector3(direcao.x * velocidadeAtual, rb.linearVelocity.y, direcao.z * velocidadeAtual);
            
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
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