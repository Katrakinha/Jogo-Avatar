using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [Header("Banco de Dados")]
    public EnemyData data;
    
    [Header("Referências")]
    public Transform rootInimigo; // O corpo principal do inimigo

    private void OnTriggerEnter(Collider outro)
    {
        // Usa a Tag "Player" (a mesma que usaste na Dobra de Terra)
        if (outro.CompareTag("Player"))
        {
            Debug.Log("### O Inimigo acertou um golpe no Aang! ###");
            
            // --- AGORA É OFICIAL: TIRA VIDA DO AANG ---
            PlayerStatus vidaAang = outro.GetComponent<PlayerStatus>();
            if (vidaAang != null) 
            {
                // Aplica o dano e o knockback usando os dados do inimigo
                vidaAang.ReceberDano(data.danoMelee, data.knockbackMelee, rootInimigo);
            }

            // Desliga-se para não dar duplo-hit no mesmo soco
            gameObject.SetActive(false);
        }
    }
}