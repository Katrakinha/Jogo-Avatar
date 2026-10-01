using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Banco de Dados (Scriptable Object)")]
    public AvatarMovementData data; 

    [Header("Referências do Jogador")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public GameObject objetoPlanador; 
    public GameObject objetoAirScooter; 

    [Header("Estado em Tempo de Execução (Mutáveis)")]
    public float currentSpeed;
    public bool isGrounded;
    public bool canDoubleJump;
    public bool didDoubleJump;
    public bool didMountAirScooter = false; 
    public bool isGliding = false;
    public bool isAirScooter = false; 
    public bool isMountingAirScooter = false; 
    public int soprosUsados = 0;
    public bool didSopro = false; 
    public bool isAiming = false; // NOVA VARIÁVEL: Diz ao script que estamos a mirar!

    [Header("Controles (Novo Input System)")]
    public InputActionReference controleMover;
    public InputActionReference controlePular;
    public InputActionReference controleCancelarPlanador;
    public InputActionReference controleSopro;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 glideDirection;
    private Vector3 scooterDirection; 
    private float mountTimer = 0f;
    private float efeitoInclinacaoSopro = 0f; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (objetoPlanador != null) objetoPlanador.SetActive(false);
        if (objetoAirScooter != null) objetoAirScooter.SetActive(false);
    }

    void OnEnable()
    {
        controleMover.action.Enable();
        controlePular.action.Enable();
        controleCancelarPlanador.action.Enable();
        controleSopro.action.Enable();
    }

    void OnDisable()
    {
        controleMover.action.Disable();
        controlePular.action.Disable();
        controleCancelarPlanador.action.Disable();
        controleSopro.action.Disable();
    }

    void Update()
    {
        didDoubleJump = false;
        didMountAirScooter = false; 

        Vector2 inputDir = controleMover.action.ReadValue<Vector2>();
        float moveX = inputDir.x;
        float moveZ = inputDir.y;
        
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;

        moveInput = (camForward.normalized * moveZ + camRight.normalized * moveX).normalized;
        
        // CALCULA A VELOCIDADE VISUAL DO ANIMATOR (Mais devagar se estiver a mirar)
        float targetSpeedCalc = isAiming ? (data.moveSpeed * 0.4f) : data.moveSpeed;
        currentSpeed = moveInput.magnitude * targetSpeedCalc;

        CheckGrounded();

        // Bloqueia pulo e patinete se estiver a mirar
        if (!isAiming)
        {
            if (controleCancelarPlanador.action.WasPressedThisFrame() && !isGliding)
            {
                if (isAirScooter || isMountingAirScooter)
                    DesmontarAirScooterComPulinho();
                else if (isGrounded)
                    AtivarAirScooter();
            }

            if (isGliding && controleSopro.action.WasPressedThisFrame() && soprosUsados < data.maxSopros)
            {
                soprosUsados++;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * data.boostSoproForce, ForceMode.Impulse);
                
                didSopro = true; 
                efeitoInclinacaoSopro = -50f; 
            }

            if (controlePular.action.WasPressedThisFrame())
            {
                if (isGrounded)
                {
                    PararPlanar();
                    if (isAirScooter || isMountingAirScooter)
                        DesativarAirScooterSilencioso();
                    rb.AddForce(Vector3.up * data.jumpForce, ForceMode.Impulse);
                }
                else if (isAirScooter || canDoubleJump)
                {
                    ExecutarPuloDuploComEfeito();
                }
                else if (!isGliding)
                {
                    AtivarPlanador();
                }
            }
        }
    }

    private void ExecutarPuloDuploComEfeito()
    {
        PararPlanar();
        DesativarAirScooterSilencioso();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * data.doubleJumpForce, ForceMode.Impulse);
        canDoubleJump = false;
        didDoubleJump = true; 
    }

    void FixedUpdate()
    {
        if (isGliding)
        {
            Vector2 inputDir = controleMover.action.ReadValue<Vector2>();
            float inputHorizontal = inputDir.x; 
            float inputVertical = inputDir.y;   

            if (Mathf.Abs(inputHorizontal) > 0.1f)
            {
                float turnSpeed = 90f * Time.fixedDeltaTime;
                Quaternion turnOffset = Quaternion.Euler(0f, inputHorizontal * turnSpeed, 0f);
                glideDirection = turnOffset * glideDirection;
            }

            float currentGlideSpeed = data.baseGlideForwardSpeed + (inputVertical * 5f);
            currentGlideSpeed = Mathf.Max(currentGlideSpeed, 3f); 
            
            Vector3 targetHorizVel = glideDirection * currentGlideSpeed;
            rb.linearVelocity = new Vector3(targetHorizVel.x, rb.linearVelocity.y, targetHorizVel.z);

            rb.AddForce(Vector3.down * data.glideGravityForce, ForceMode.Acceleration);

            Quaternion baseRotation = Quaternion.LookRotation(glideDirection);
            
            efeitoInclinacaoSopro = Mathf.Lerp(efeitoInclinacaoSopro, 0f, 5f * Time.fixedDeltaTime);
            
            float pitchAngle = (inputVertical * 30f) + efeitoInclinacaoSopro; 
            float rollAngle = -inputHorizontal * data.maxBankAngle;
            
            Quaternion finalRotation = baseRotation * Quaternion.Euler(pitchAngle, 0f, rollAngle) * Quaternion.Euler(90f, 0f, 0f);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalRotation, 10f * Time.fixedDeltaTime));
        }
        else if (isMountingAirScooter)
        {
            mountTimer -= Time.fixedDeltaTime;

            Vector3 targetVelocity = moveInput * data.moveSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;

            if (mountTimer <= 0f)
            {
                isMountingAirScooter = false;
                isAirScooter = true;
                if (objetoAirScooter != null) objetoAirScooter.SetActive(true);
            }
        }
        else if (isAirScooter)
        {
            Vector2 inputDir = controleMover.action.ReadValue<Vector2>();
            float inputHorizontal = inputDir.x; 
            float inputVertical = inputDir.y;   

            if (Mathf.Abs(inputHorizontal) > 0.1f)
            {
                float turnSpeed = 120f * Time.fixedDeltaTime;
                Quaternion turnOffset = Quaternion.Euler(0f, inputHorizontal * turnSpeed, 0f);
                scooterDirection = turnOffset * scooterDirection;
            }

            if (isGrounded) 
            {
                float currentScooterSpeed = data.airScooterSpeed + (inputVertical * 4f);
                currentScooterSpeed = Mathf.Max(currentScooterSpeed, 3f);

                Vector3 targetScooterVel = scooterDirection * currentScooterSpeed;
                targetScooterVel.y = rb.linearVelocity.y; 
                rb.linearVelocity = targetScooterVel;
            }
            else 
            {
                Vector3 fallVel = rb.linearVelocity;
                fallVel.x = 0f; 
                fallVel.z = 0f;
                fallVel.y += Physics.gravity.y * data.extraFallGravityMultiplier * Time.fixedDeltaTime;
                rb.linearVelocity = fallVel;
            }

            Quaternion baseRotation = Quaternion.LookRotation(scooterDirection);
            float pitchAngle = inputVertical * 20f;                  
            float rollAngle = -inputHorizontal * (data.maxBankAngle * 0.8f); 
            
            Quaternion finalRotation = baseRotation * Quaternion.Euler(pitchAngle, 0f, rollAngle);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalRotation, 12f * Time.fixedDeltaTime));
        }
        else
        {
            // O MOVER NORMAL DO CHÃO: Corta a velocidade real pela metade se mirar
            float velocidadeAtual = isAiming ? (data.moveSpeed * 0.4f) : data.moveSpeed;
            Vector3 targetVelocity = moveInput * velocidadeAtual;
            targetVelocity.y = rb.linearVelocity.y;

            if (!isGrounded)
            {
                targetVelocity.y += Physics.gravity.y * data.extraFallGravityMultiplier * Time.fixedDeltaTime;
            }

            rb.linearVelocity = targetVelocity;

            // BLOQUEIA A ROTAÇÃO DE MOVIMENTO SE MIRAR (O PlayerFireBending já vai rodá-lo)
            if (!isAiming) 
            {
                if (moveInput != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveInput);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
                }
                else
                {
                    Vector3 currentFwd = transform.forward;
                    currentFwd.y = 0f; 
                    if (currentFwd != Vector3.zero)
                    {
                        Quaternion uprightRotation = Quaternion.LookRotation(currentFwd);
                        rb.MoveRotation(Quaternion.Slerp(rb.rotation, uprightRotation, 10f * Time.fixedDeltaTime));
                    }
                }
            }
        }
    }

    private void AtivarAirScooter()
    {
        isMountingAirScooter = true;
        didMountAirScooter = true; 
        mountTimer = 0.15f; 

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        scooterDirection = fwd != Vector3.zero ? fwd.normalized : Vector3.forward;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * data.mountJumpForce, ForceMode.Impulse);
    }

    private void DesmontarAirScooterComPulinho()
    {
        isAirScooter = false;
        isMountingAirScooter = false;
        if (objetoAirScooter != null) objetoAirScooter.SetActive(false);

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * data.mountJumpForce, ForceMode.Impulse);
    }

    private void DesativarAirScooterSilencioso()
    {
        isAirScooter = false;
        isMountingAirScooter = false;
        if (objetoAirScooter != null) objetoAirScooter.SetActive(false);
        
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    private void AtivarPlanador()
    {
        DesativarAirScooterSilencioso(); 
        isGliding = true;
        soprosUsados = 0;
        
        ValueCamFwd();
    }

    private void ValueCamFwd()
    {
        Vector3 camFwd = Camera.main.transform.forward;
        camFwd.y = 0f;
        glideDirection = camFwd != Vector3.zero ? camFwd.normalized : transform.forward;

        if (objetoPlanador != null) objetoPlanador.SetActive(true);
    }

    private void PararPlanar()
    {
        if (isGliding)
        {
            isGliding = false;
            if (objetoPlanador != null) objetoPlanador.SetActive(false);
            
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }
    }

    private void CheckGrounded()
    {
        if (data != null)
        {
            Vector3 boxSize = new Vector3(data.boxWidth, data.boxHeight, data.boxLength);
            isGrounded = Physics.CheckBox(groundCheck.position, boxSize / 2f, Quaternion.identity, groundLayer);
            
            if (isGrounded)
            {
                canDoubleJump = true;
                PararPlanar();
            }
        }
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null && data != null)
        {
            Gizmos.color = Color.red;
            Vector3 boxSize = new Vector3(data.boxWidth, data.boxHeight, data.boxLength);
            Gizmos.DrawWireCube(groundCheck.position, boxSize);
        }
    }
}