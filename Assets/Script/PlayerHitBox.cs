using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [Header("Banco de Dados")]
    public AvatarCombatData data; 

    [Header("Referências")]
    public Transform playerRoot; 

    private void OnTriggerEnter(Collider outro)
    {
        if (data == null) return;

        if ((data.layerInimigos.value & (1 << outro.gameObject.layer)) > 0)
        {
            UniversalEnemy inimigo = outro.GetComponent<UniversalEnemy>();
            if (inimigo != null)
            {
                // AGORA ENVIA O DANO E A FORÇA DO KNOCKBACK!
                inimigo.ReceberDano(data.danoAtaqueBase, data.forcaKnockback, playerRoot);
                
                gameObject.SetActive(false);
            }
        }
    }
}