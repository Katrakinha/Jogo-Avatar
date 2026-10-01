using UnityEngine;

public class ProjetilFogo : MonoBehaviour
{
    [Header("Configurações")]
    public float tempoDeVida = 4f; 
    public int dano = 10;
    
    [Header("Efeitos Visuais")]
    public GameObject prefabExplosao; // Arrasta o teu efeito de explosão para aqui

    void Start()
    {
        Destroy(gameObject, tempoDeVida);
    }

    void OnTriggerEnter(Collider outro)
    {
        if (outro.CompareTag("Player")) return;

        Alvo alvoAtingido = outro.GetComponent<Alvo>();
        if (alvoAtingido != null)
        {
            alvoAtingido.ReceberDano(dano);
        }

        // Toca a explosão exatamente no ponto onde a bola bateu
        if (prefabExplosao != null)
        {
            Instantiate(prefabExplosao, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}