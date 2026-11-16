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
    public int pulosRealizados = 0; 
    public bool didJump = false;
    public bool didDoubleJump = false; 
    public bool didMountAirScooter = false; 
    public bool isGliding = false;
    public bool isAirScooter = false; 
    public bool isMountingAirScooter = false; 
    public int soprosUsados = 0;
    public bool didSopro = false; 
    
    public bool isAiming = false; 
    public bool isEarthSlamming = false; 
    public bool isAttacking = false;

    public IdentidadePersonagem identidade;

    [HideInInspector] public bool inputFakeAtivo = false;
    [HideInInspector] public Vector3 direcaoMovimentoIA = Vector3.zero;
    [HideInInspector] public bool inputFakePulo = false;

    [Header("Controles (Novo Input System)")]
    public InputActionReference controleMover;
    public InputActionReference controlePular;
    public InputActionReference controleCancelarPlanador;
    public InputActionReference controleSopro;
    public InputActionReference controleAirScooter; 
    
    [Header("Controles da Câmera (Para Ligar o Cinemachine)")]
    public InputActionReference controleCameraX; 
    public InputActionReference controleCameraY; 

    private Rigidbody rb;
    private Vector3 moveInput;
    private float mountTimer = 0f;
    private float efeitoInclinacaoSopro = 0f; 
    private float tempoCegoPulo = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (identidade == null) identidade = GetComponent<IdentidadePersonagem>();
        if (objetoPlanador != null) objetoPlanador.SetActive(false);
        if (objetoAirScooter != null) objetoAirScooter.SetActive(false);
    }

    void OnEnable()
    {
        if (controleMover != null) controleMover.action.Enable();
        if (controlePular != null) controlePular.action.Enable();
        if (controleCancelarPlanador != null) controleCancelarPlanador.action.Enable();
        if (controleSopro != null) controleSopro.action.Enable();
        if (controleAirScooter != null) controleAirScooter.action.Enable();
        if (controleCameraX != null) controleCameraX.action.Enable();
        if (controleCameraY != null) controleCameraY.action.Enable();
    }

    void OnDisable()
    {
        if (controleMover != null) controleMover.action.Disable();
        if (controlePular != null) controlePular.action.Disable();
        if (controleCancelarPlanador != null) controleCancelarPlanador.action.Disable();
        if (controleSopro != null) controleSopro.action.Disable();
        if (controleAirScooter != null) controleAirScooter.action.Disable();
        if (controleCameraX != null) controleCameraX.action.Disable();
        if (controleCameraY != null) controleCameraY.action.Disable();
    }

    void Update()
    {
        if (tempoCegoPulo > 0) tempoCegoPulo -= Time.deltaTime;

        didJump = false;
        didMountAirScooter = false; 
        didDoubleJump = false; 

        if (identidade == null || identidade.isPlayerControlled)
        {
            Vector2 inputDir = Vector2.zero;
            if (controleMover != null) inputDir = controleMover.action.ReadValue<Vector2>();

            float moveX = inputDir.x;
            float moveZ = inputDir.y;

            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;

            moveInput = (camForward.normalized * moveZ + camRight.normalized * moveX);
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
        }
        else if (inputFakeAtivo)
        {
            moveInput = Vector3.ClampMagnitude(direcaoMovimentoIA, 1f);
        }
        else
        {
            moveInput = Vector3.zero;
        }
        
        float targetSpeedCalc = isAiming ? (data.moveSpeed * 0.4f) : data.moveSpeed;
        currentSpeed = moveInput.magnitude * targetSpeedCalc;

        CheckGrounded();

        if (!isAiming && !isEarthSlamming && (identidade == null || identidade.isPlayerControlled || inputFakeAtivo))
        {
            if (identidade == null || identidade.isPlayerControlled)
            {
                if (data.podePlanar && controleCancelarPlanador != null && controleCancelarPlanador.action.WasPressedThisFrame())
                {
                    if (isGliding) PararPlanar();
                }

                if (data.podeAirScooter && controleAirScooter != null && controleAirScooter.action.WasPressedThisFrame())
                {
                    if (!isGliding)
                    {
                        if (isAirScooter || isMountingAirScooter) 
                            DesmontarAirScooterComPulinho();
                        else if (isGrounded) 
                            AtivarAirScooter();
                    }
                }

                if (data.podePlanar && isGliding && controleSopro != null && controleSopro.action.WasPressedThisFrame() && soprosUsados < data.maxSopros)
                {
                    soprosUsados++;
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    rb.AddForce(Vector3.up * data.boostSoproForce, ForceMode.Impulse);
                    didSopro = true; 
                    efeitoInclinacaoSopro = -50f; 
                }
            }

            bool apertouPulo = false;
            if (identidade == null || identidade.isPlayerControlled)
            {
                if (controlePular != null && controlePular.action.WasPressedThisFrame()) apertouPulo = true;
            }
            if (inputFakePulo) apertouPulo = true; 

            if (apertouPulo)
            {
                inputFakePulo = false;
                
                if (pulosRealizados == 0 && isGrounded) 
                {
                    pulosRealizados = 1;
                    tempoCegoPulo = 0.15f; 
                    didJump = true;
                    
                    PararPlanar();
                    if (isAirScooter || isMountingAirScooter) DesativarAirScooterSilencioso();
                        
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); 
                    rb.AddForce(Vector3.up * data.jumpForce, ForceMode.Impulse);
                }
                else if (data.podePuloDuplo && (pulosRealizados == 1 || isAirScooter)) 
                {
                    pulosRealizados = 2; 
                    ExecutarPuloDuploComEfeito();
                }
                else if (data.podePlanar && pulosRealizados >= 2 && !isGliding) 
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
        didDoubleJump = true; 
    }

    void FixedUpdate()
    {
        if (isEarthSlamming) return; 
        if (isAttacking)
        {
            if (moveInput != Vector3.zero && !isAiming)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveInput.normalized);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 15f * Time.fixedDeltaTime));
            }
            return; 
        }

        if (isGliding)
        {
            Vector2 inputDir = controleMover != null ? controleMover.action.ReadValue<Vector2>() : Vector2.zero;
            Vector3 direcaoVoo = moveInput.sqrMagnitude > 0.001f ? moveInput.normalized : transform.forward;
            
            float currentGlideSpeed = data.baseGlideForwardSpeed + (inputDir.y * 5f);
            currentGlideSpeed = Mathf.Max(currentGlideSpeed, 3f); 
            
            Vector3 targetHorizVel = direcaoVoo * currentGlideSpeed;
            rb.linearVelocity = new Vector3(targetHorizVel.x, rb.linearVelocity.y, targetHorizVel.z);
            rb.AddForce(Vector3.down * data.glideGravityForce, ForceMode.Acceleration);

            Quaternion baseRotation = Quaternion.LookRotation(direcaoVoo);
            efeitoInclinacaoSopro = Mathf.Lerp(efeitoInclinacaoSopro, 0f, 5f * Time.fixedDeltaTime);
            float pitchAngle = (inputDir.y * 30f) + efeitoInclinacaoSopro; 
            float rollAngle = -inputDir.x * data.maxBankAngle;
            
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
            Vector2 inputDir = controleMover != null ? controleMover.action.ReadValue<Vector2>() : Vector2.zero;
            Vector3 direcaoScooter = moveInput.sqrMagnitude > 0.001f ? moveInput.normalized : transform.forward;

            if (isGrounded) 
            {
                float currentScooterSpeed = data.airScooterSpeed + (inputDir.y * 4f);
                currentScooterSpeed = Mathf.Max(currentScooterSpeed, 3f);

                Vector3 targetScooterVel = direcaoScooter * currentScooterSpeed;
                targetScooterVel.y = rb.linearVelocity.y; 
                rb.linearVelocity = targetScooterVel;
            }
            else 
            {
                Vector3 fallVel = rb.linearVelocity;
                fallVel.x = 0f; fallVel.z = 0f;
                fallVel.y += Physics.gravity.y * data.extraFallGravityMultiplier * Time.fixedDeltaTime;
                rb.linearVelocity = fallVel;
            }

            Quaternion baseRotation = Quaternion.LookRotation(direcaoScooter);
            float pitchAngle = inputDir.y * 20f;                  
            float rollAngle = -inputDir.x * (data.maxBankAngle * 0.8f); 
            Quaternion finalRotation = baseRotation * Quaternion.Euler(pitchAngle, 0f, rollAngle);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalRotation, 12f * Time.fixedDeltaTime));
        }
        else
        {
            float velocidadeAtual = isAiming ? (data.moveSpeed * 0.4f) : data.moveSpeed;
            Vector3 targetVelocity = moveInput * velocidadeAtual;
            targetVelocity.y = rb.linearVelocity.y;

            if (!isGrounded)
            {
                targetVelocity.y += Physics.gravity.y * data.extraFallGravityMultiplier * Time.fixedDeltaTime;
            }

            rb.linearVelocity = targetVelocity;

            if (!isAiming) 
            {
                if (moveInput.sqrMagnitude > 0.005f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveInput.normalized);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 12f * Time.fixedDeltaTime));
                }
            }
        }
    }

    private void AtivarAirScooter()
    {
        isMountingAirScooter = true;
        didMountAirScooter = true; 
        mountTimer = 0.15f; 

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
        if (tempoCegoPulo > 0)
        {
            isGrounded = false;
            return;
        }

        if (data != null && groundCheck != null)
        {
            Vector3 boxSize = new Vector3(data.boxWidth, data.boxHeight, data.boxLength);
            Vector3 boxCenter = groundCheck.position + groundCheck.TransformDirection(data.boxOffset);

            isGrounded = Physics.CheckBox(boxCenter, boxSize / 2f, groundCheck.rotation, groundLayer);
            
            if (!isGrounded && (isAirScooter || isGliding))
            {
                isGrounded = Physics.Raycast(boxCenter, Vector3.down, 10f, groundLayer);
            }
            
            if (isGrounded)
            {
                pulosRealizados = 0; 
                PararPlanar();
            }
        }
    }
}