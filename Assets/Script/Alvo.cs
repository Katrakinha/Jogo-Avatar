using UnityEngine;

public class Alvo : MonoBehaviour
{
    [Header("Status")]
    public int vidaMaxima = 30;
    private int vidaAtual;

    [Header("Respawn")]
    public float tempoRespawn = 3f;

    [Header("Movimento (Opcional)")]
    public bool seMexe = false;
    public float distancia = 5f;
    public float velocidade = 2f;
    
    private Vector3 posicaoInicial;
    private Collider col;
    private MeshRenderer render;

    void Start()
    {
        vidaAtual = vidaMaxima;
        posicaoInicial = transform.position;
        col = GetComponent<Collider>();
        render = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        // Se a caixa estiver marcada e o alvo estiver vivo, ele faz um vai-e-vem lateral
        if (seMexe && vidaAtual > 0)
        {
            float deslocamento = Mathf.PingPong(Time.time * velocidade, distancia) - (distancia / 2f);
            
            // O movimento está a ser feito no eixo X. Se quiseres que ele ande para a frente/trás, muda o deslocamento para o Z.
            transform.position = posicaoInicial + new Vector3(deslocamento, 0, 0); 
        }
    }

    public void ReceberDano(int quantidade)
    {
        if (vidaAtual <= 0) return; // Impede que receba dano enquanto está invisível a aguardar o respawn

        vidaAtual -= quantidade;
        
        if(render != null) render.material.color = Color.red;
        Invoke("ResetarCor", 0.1f); // Pisca vermelho e volta ao normal quase instantaneamente

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    private void ResetarCor()
    {
        if(render != null) render.material.color = Color.white; 
    }

    private void Morrer()
    {
        // Fica invisível e intangível em vez de ser destruído
        if(render != null) render.enabled = false;
        if(col != null) col.enabled = false;

        // Chama a função de renascer após o tempo definido
        Invoke("Respawn", tempoRespawn);
    }

    private void Respawn()
    {
        vidaAtual = vidaMaxima;
        transform.position = posicaoInicial; // Volta para o ponto central da patrulha
        
        if(render != null) render.enabled = true;
        if(col != null) col.enabled = true;
        ResetarCor();
    }
}