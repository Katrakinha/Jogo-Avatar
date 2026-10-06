using UnityEngine;
using System.Collections;

public class EarthRock : MonoBehaviour
{
    [Header("Dano da Rocha")]
    public float danoDaPedra = 15f;
    public float knockbackDaPedra = 7f; // Terra bate mais pesado!

    private float targetY;
    private float startY;
    private AvatarEarthData data;

    // A Dobra de Terra chama isto quando cria a pedra
    public void Inicializar(AvatarEarthData earthData)
    {
        data = earthData;
        targetY = transform.position.y;
        startY = targetY - data.profundidadeEscondida; 
        
        // Joga a pedra para debaixo do chão instantaneamente
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        
        StartCoroutine(CicloDeVidaRocha());
    }

    private IEnumerator CicloDeVidaRocha()
    {
        // 1. SUBIR COM TUDO
        while (transform.position.y < targetY)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetY, transform.position.z), data.velocidadeSubida * Time.deltaTime);
            yield return null;
        }

        // --- NOVO: HITBOX EM ONDA DE CHOQUE NO AANG ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Transform playerTransform = playerObj.transform;
            
            // O centro da esfera invisível agora é a posição do Aang, com raio de 2.5 metros!
            Collider[] inimigosAcertados = Physics.OverlapSphere(playerTransform.position, 2.5f); 
            
            foreach (Collider inimigo in inimigosAcertados)
            {
                UniversalEnemy scriptInimigo = inimigo.GetComponent<UniversalEnemy>();
                if (scriptInimigo != null)
                {
                    scriptInimigo.ReceberDano(danoDaPedra, knockbackDaPedra, playerTransform); 
                }
            }
        }

        // 2. ESPERAR O AANG TIRAR A MÃO DO CHÃO
        yield return new WaitForSeconds(data.tempoNoTopo);

        // 3. AFUNDAR DEVAGAR
        while (transform.position.y > startY)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, startY, transform.position.z), data.velocidadeDescida * Time.deltaTime);
            yield return null;
        }

        // 4. DESTRUIR PARA NÃO PESAR O JOGO
        Destroy(gameObject);
    }
}