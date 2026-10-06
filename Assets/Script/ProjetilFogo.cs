using UnityEngine;

public class ProjetilFogo : MonoBehaviour
{
    [Header("Configurações")]
    public float tempoDeVida = 4f; 
    public float dano = 10f; // Transformado em float para compatibilidade
    public float forcaKnockback = 4f; // <-- ADICIONADO: Empurrão do impacto da chama
    
    [Header("Efeitos Visuais")]
    public GameObject prefabExplosao; 

    void Start()
    {
        Destroy(gameObject, tempoDeVida);
    }

    void OnTriggerEnter(Collider outro)
    {
        if (outro.CompareTag("Player")) return;

        // --- A BOLA DE FOGO TIRA VIDA AQUI ---
        UniversalEnemy alvoAtingido = outro.GetComponent<UniversalEnemy>();
        if (alvoAtingido != null)
        {
            // Busca o Aang pela Tag "Player" para o inimigo saber de onde veio o tiro
            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            
            // Passa os 3 valores: Dano, Knockback e o Transform do Aang
            alvoAtingido.ReceberDano(dano, forcaKnockback, playerTransform);
        }

        if (prefabExplosao != null)
        {
            Instantiate(prefabExplosao, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}