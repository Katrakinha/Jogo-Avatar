using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    [Header("Ligação com o Cérebro")]
    public PlayerCombat scriptCombate; // Arrasta o objeto principal do Aang para aqui

    // O Animator chama estas funções, e a Ponte passa a mensagem ao PlayerCombat
    public void AnimEvent_LigarHitbox()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_LigarHitbox();
    }

    public void AnimEvent_DesligarHitbox()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_DesligarHitbox();
    }

    public void AnimEvent_FimDoAtaque()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_FimDoAtaque();
    }
}