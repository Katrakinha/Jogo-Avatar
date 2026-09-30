using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Banco de Dados")]
    public AvatarCombatData data;

    [Header("Controles")]
    public InputActionReference controleAtacar; // Botão de ataque (ex: Clique do Mouse / Quadrado)

    [Header("Estado do Combate (Mutáveis)")]
    public int comboStep = 0; // Vai de 1 a 4
    public bool didAttack = false; // Gatilho para o Animator
    public bool isAttacking = false;

    private float currentAttackTimer = 0f; // Relógio que tranca o personagem
    private bool inputBuffer = false;      // Memória que guarda se você apertou o botão

    private float comboTimer = 0f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        controleAtacar.action.Enable();
    }

    void OnDisable()
    {
        controleAtacar.action.Disable();
    }

    void Update()
    {
        // 1. O INPUT BUFFER (A Memória)
        // Se você apertar o botão, ele guarda a informação, mesmo se estiver atacando!
        if (Input.GetButtonDown("Fire1"))
        {
            inputBuffer = true; 
        }

        // 2. A TRANCA DA ANIMAÇÃO
        if (isAttacking)
        {
            // O tempo da animação vai diminuindo...
            currentAttackTimer -= Time.deltaTime;
            
            // Quando a animação acaba, ele destranca o Aang!
            if (currentAttackTimer <= 0)
            {
                isAttacking = false;
                didAttack = false;
            }
        }
        else // SE NÃO ESTIVER ATACANDO (Livre para agir)
        {
            // Controle da janela do combo (para o combo zerar se demorar muito)
            if (comboStep > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0) 
                {
                    comboStep = 0; // Perdeu o combo
                    inputBuffer = false; // Limpa a memória
                }
            }

            // 3. O DISPARO AUTOMÁTICO
            // Se o Aang está livre E o jogo lembra que você apertou o botão, ele ataca!
            if (inputBuffer)
            {
                inputBuffer = false; // Limpa a memória porque o golpe já vai sair
                Atacar();
            }
        }
    }

    private void Atacar()
    {
        if (comboStep >= 4) comboStep = 0;

        comboStep++;
        isAttacking = true; 
        didAttack = true; 
        comboTimer = data.tempoJanelaCombo; 

        // TRANCA O TEMPO BASEADO NO GOLPE (Aqui você define a duração do bloqueio)
        if (comboStep == 1) currentAttackTimer = data.tempoAnimAtk1;
        else if (comboStep == 2) currentAttackTimer = data.tempoAnimAtk2;
        else if (comboStep == 3) currentAttackTimer = data.tempoAnimAtk3;
        else if (comboStep == 4) currentAttackTimer = data.tempoAnimAtk4;

        // O Impulso para a frente
        Vector3 forcaAvanco = transform.forward;
        forcaAvanco.y = 0f;
        
        float impulso = 0f;
        if (comboStep == 1) impulso = data.avancoAtk1;
        else if (comboStep == 2) impulso = data.avancoAtk2;
        // adicione os impulsos 3 e 4 aqui depois!

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); 
        rb.AddForce(forcaAvanco.normalized * impulso, ForceMode.Impulse);

        // A HITBOX INVISÍVEL
        Vector3 centroDoArco = transform.position 
                             + (transform.right * data.hitboxOffset.x) 
                             + (transform.up * data.hitboxOffset.y) 
                             + (transform.forward * data.hitboxOffset.z);

        Collider[] inimigosAcertados = Physics.OverlapSphere(centroDoArco, data.raioDoArco, data.layerInimigos);

        foreach (Collider inimigo in inimigosAcertados)
        {
            Debug.Log("Acertou o inimigo: " + inimigo.name);
        }
    }

    // DESENHA A HITBOX NO EDITOR
    void OnDrawGizmosSelected()
    {
        if (data == null) return;
        
        Gizmos.color = Color.red;
        Vector3 centroDoArco = transform.position 
                             + (transform.right * data.hitboxOffset.x) 
                             + (transform.up * data.hitboxOffset.y) 
                             + (transform.forward * data.hitboxOffset.z);
                             
        Gizmos.DrawWireSphere(centroDoArco, data.raioDoArco);
    }

    private void ResetarCombo()
    {
        comboStep = 0;
        isAttacking = false;
    }
}