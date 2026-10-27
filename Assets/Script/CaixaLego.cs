using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LegoExplosion))]
public class CaixaLego : MonoBehaviour
{
    [Header("Movimento da Caixa (Física Real)")]
    public float forcaPuxaoFrente = 8f;
    public float forcaPuloCima = 5f;
    
    [Tooltip("Aumente para a caixa cair mais rápido (1 = gravidade normal da Unity)")]
    public float multiplicadorGravidade = 2.5f; 

    [Header("O que tem dentro? (Recompensa)")]
    public GameObject objetoLegoBuilder;

    private bool foiPuxada = false;
    private Rigidbody rb;
    private LegoExplosion scriptExplosao;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        scriptExplosao = GetComponent<LegoExplosion>();

        // Garante que a caixa começa quieta
        rb.isKinematic = true; 

        if (objetoLegoBuilder != null) objetoLegoBuilder.SetActive(false);
    }

    // A mágica do peso extra acontece aqui! (Roda junto com o motor de física)
    void FixedUpdate()
    {
        if (foiPuxada && rb != null && !rb.isKinematic)
        {
            // A Unity já aplica 1x a gravidade padrão (-9.81). Nós injetamos a força extra.
            rb.AddForce(Physics.gravity * (multiplicadorGravidade - 1f), ForceMode.Acceleration);
        }
    }

    // Esta é a função que o seu GanchoModular chama!
    public void IniciarPuxaoEExplosao()
    {
        foiPuxada = true;
        Transform jogadorPos = GameObject.FindGameObjectWithTag("Player").transform;

        Vector3 direcaoFrente = (jogadorPos.position - transform.position).normalized;
        direcaoFrente.y = 0; 

        // Liga a física real da caixa
        rb.isKinematic = false;
        
        // Empurra a caixa num arco
        rb.AddForce((direcaoFrente * forcaPuxaoFrente) + (Vector3.up * forcaPuloCima), ForceMode.Impulse);
        
        // Adiciona um giro aleatório no ar
        rb.AddTorque(new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f)), ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Se ela já foi arrancada pelo chicote E bateu num objeto com a tag "Chao"
        if (foiPuxada && collision.gameObject.CompareTag("Chao"))
        {
            foiPuxada = false; // Desliga a trava para não explodir duas vezes

            // 1. Liberta as peças do construtor rente ao chão!
            if (objetoLegoBuilder != null)
            {
                float alturaDoChao = collision.contacts[0].point.y;
                Vector3 posicaoNoChao = new Vector3(transform.position.x, alturaDoChao, transform.position.z);

                objetoLegoBuilder.transform.position = posicaoNoChao;
                objetoLegoBuilder.SetActive(true);
            }

            // 2. Chama o script LegoExplosion para fazer as peças voarem!
            if (scriptExplosao != null)
            {
                scriptExplosao.Explodir();
            }
            else
            {
                Destroy(gameObject); 
            }
        }
    }
}