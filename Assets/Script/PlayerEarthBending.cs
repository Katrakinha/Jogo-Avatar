using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Events; // NOVO: Para criarmos eventos no Inspector!

public class PlayerEarthBending : MonoBehaviour
{
    [Header("Referências")]
    public PlayerMovement scriptMovimento;
    public AvatarEarthData earthData; 
    public Animator anim;
    public Rigidbody rb;

    [Header("Input")]
    public InputActionReference controleAtaque;

    [Header("Configurações da Queda")]
    public float forcaQueda = 40f; 

    [Header("Efeitos Visuais e Som")]
    public UnityEvent onTremorDeTerra; // NOVO: Gatilho para o Camera Shake e Sons!
    
    private bool isSlamming = false;
    private bool isWindingUp = false; 

    void OnEnable() { if (controleAtaque != null) controleAtaque.action.Enable(); }
    void OnDisable() { if (controleAtaque != null) controleAtaque.action.Disable(); }

    void Update()
    {
        if (controleAtaque.action.WasPressedThisFrame() && 
            !scriptMovimento.isGrounded && 
            !scriptMovimento.isGliding && 
            !scriptMovimento.isAirScooter &&
            !isSlamming &&
            scriptMovimento.pulosRealizados >= 2) 
        {
            IniciarDobraTerra();
        }

        if (isSlamming && !isWindingUp && scriptMovimento.isGrounded)
        {
            AterrarDobraTerra();
        }

        if (anim != null) 
        {
            anim.SetBool("TerraIsGrounded", scriptMovimento.isGrounded);
        }
    }

    void FixedUpdate()
    {
        if (isSlamming)
        {
            if (isWindingUp)
            {
                rb.linearVelocity = Vector3.zero;
                rb.useGravity = false;
            }
            else
            {
                rb.useGravity = true;
                rb.linearVelocity = new Vector3(0f, -forcaQueda, 0f);
            }
        }
    }

    private void IniciarDobraTerra()
    {
        isSlamming = true;
        isWindingUp = true;
        if (scriptMovimento != null) scriptMovimento.isEarthSlamming = true; 

        if (anim != null) anim.SetTrigger("TerraSlam");
    }

    public void IniciarQuedaTerraEvent()
    {
        isWindingUp = false; 
    }

    private void AterrarDobraTerra()
    {
        isSlamming = false;
        rb.useGravity = true;
    }

    public void PausarAnimacaoTerraEvent()
    {
        if (anim != null) anim.speed = 0f; 
        
        // A MÁGICA ACONTECE AQUI: No frame exato em que a mão bate no chão!
        if (onTremorDeTerra != null) onTremorDeTerra.Invoke();
        
        StartCoroutine(RotinaOndasDeTerra());
    }

    public void FinalizarDobraTerraEvent()
    {
        if (scriptMovimento != null) scriptMovimento.isEarthSlamming = false; 
        if (anim != null) anim.speed = 1f; 
    }

    private IEnumerator RotinaOndasDeTerra()
    {
        if (earthData == null) 
        {
            if (anim != null) anim.speed = 1f; 
            yield break;
        }

        yield return new WaitForSeconds(earthData.atrasoAnel1);
        GerarAnelDeRochas(earthData.qtdRochasAnel1, earthData.raioAnel1);

        yield return new WaitForSeconds(earthData.atrasoAnel2);
        GerarAnelDeRochas(earthData.qtdRochasAnel2, earthData.raioAnel2);

        yield return new WaitForSeconds(earthData.atrasoAnel3);
        GerarAnelDeRochas(earthData.qtdRochasAnel3, earthData.raioAnel3);

        yield return new WaitForSeconds(earthData.tempoNoTopo);

        if (anim != null) anim.speed = 1f;
    }

    private void GerarAnelDeRochas(int quantidade, float raio)
    {
        float passoDoAngulo = 360f / quantidade;

        for (int i = 0; i < quantidade; i++)
        {
            float anguloBase = i * passoDoAngulo;
            float anguloFinal = anguloBase + Random.Range(-passoDoAngulo * 0.3f, passoDoAngulo * 0.3f); 
            
            Vector3 direcao = Quaternion.Euler(0, anguloFinal, 0) * Vector3.forward;
            Vector3 posicaoSpawn = transform.position + (direcao * raio);
            
            if (Physics.Raycast(posicaoSpawn + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, scriptMovimento.groundLayer))
            {
                posicaoSpawn.y = hit.point.y; 
            }
            else 
            {
                continue; 
            }

            GameObject rocha = Instantiate(earthData.rockPrefab, posicaoSpawn, Quaternion.identity);

            rocha.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0, 360f), Random.Range(-15f, 15f));
            float escalaMultiplicador = Random.Range(earthData.variacaoEscalaMin, earthData.variacaoEscalaMax);
            rocha.transform.localScale = earthData.escalaBase * escalaMultiplicador;

            EarthRock scriptRocha = rocha.GetComponent<EarthRock>();
            if (scriptRocha != null) scriptRocha.Inicializar(earthData);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (earthData != null)
        {
            Gizmos.color = Color.yellow;
            DesenharCirculoMagico(transform.position, earthData.raioAnel1);

            Gizmos.color = new Color(1f, 0.5f, 0f); 
            DesenharCirculoMagico(transform.position, earthData.raioAnel2);

            Gizmos.color = Color.red;
            DesenharCirculoMagico(transform.position, earthData.raioAnel3);
        }
    }

    private void DesenharCirculoMagico(Vector3 centro, float raio)
    {
        int segmentos = 36;
        float angulo = 0f;
        Vector3 pontoAnterior = centro + new Vector3(Mathf.Sin(angulo) * raio, 0f, Mathf.Cos(angulo) * raio);

        for (int i = 1; i <= segmentos; i++)
        {
            angulo += (360f / segmentos) * Mathf.Deg2Rad;
            Vector3 proximoPonto = centro + new Vector3(Mathf.Sin(angulo) * raio, 0f, Mathf.Cos(angulo) * raio);
            Gizmos.DrawLine(pontoAnterior, proximoPonto);
            pontoAnterior = proximoPonto;
        }
    }
}