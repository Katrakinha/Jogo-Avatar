using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    [Header("Ligação com o Cérebro")]
    public PlayerCombat scriptCombate;

    void Awake()
    {
        if (scriptCombate == null)
        {
            scriptCombate = GetComponentInParent<PlayerCombat>();
        }
    }

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

    public void AnimEvent_ImpulsoAtaque()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_ImpulsoAtaque();
    }

    public void AnimEvent_LigarAguaDireita()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_LigarAguaDireita();
    }

    public void AnimEvent_LigarAguaEsquerda()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_LigarAguaEsquerda();
    }

    public void AnimEvent_CriarBolaAgua()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_CriarBolaAgua();
    }

    public void AnimEvent_EstourarBolaAgua()
    {
        if (scriptCombate != null) scriptCombate.AnimEvent_EstourarBolaAgua();
    }
}