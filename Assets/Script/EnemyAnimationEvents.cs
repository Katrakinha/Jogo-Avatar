using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    [Header("Ligação com o Cérebro")]
    public UniversalEnemy scriptInimigo; // Arrasta o objeto pai (Root) do inimigo para aqui

    // --- REPASSA OS EVENTOS DE ANIMAÇÃO PARA O CÉREBRO ---

    public void AnimEvent_LigarHitbox()
    {
        if (scriptInimigo != null) scriptInimigo.AnimEvent_LigarHitbox();
    }

    public void AnimEvent_DesligarHitbox()
    {
        if (scriptInimigo != null) scriptInimigo.AnimEvent_DesligarHitbox();
    }

    public void AnimEvent_AtirarProjetil()
    {
        if (scriptInimigo != null) scriptInimigo.AnimEvent_AtirarProjetil();
    }

    public void AnimEvent_FimDoAtaque()
    {
        if (scriptInimigo != null) scriptInimigo.AnimEvent_FimDoAtaque();
    }
}