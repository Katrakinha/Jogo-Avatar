using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Referências de Ligação (Glue Code)")]
    public PlayerMovement movementSimulation;
    public PlayerCombat combatSimulation;
    public Animator anim;
    
    [Header("VFX - Movimento")]
    public ParticleSystem vfxRedemoinho; 
    public ParticleSystem vfxSopro;

    [Header("VFX - Cortes do Bastão (Novos)")]
    public ParticleSystem vfxCorteNormal;   
    public ParticleSystem vfxCorteCircular; 

    [Header("Ajustes do Ataque 1")]
    public Vector3 posicaoAtk1;
    public Vector3 rotacaoAtk1;
    public Vector3 escalaAtk1 = Vector3.one; // Adicionado: Controlo de Tamanho/Espelho

    [Header("Ajustes do Ataque 2")]
    public Vector3 posicaoAtk2;
    public Vector3 rotacaoAtk2;
    public Vector3 escalaAtk2 = Vector3.one; 

    [Header("Ajustes do Ataque 3")]
    public Vector3 posicaoAtk3;
    public Vector3 rotacaoAtk3;
    public Vector3 escalaAtk3 = Vector3.one; 

    [Header("Ajustes do Ataque 4")]
    public Vector3 posicaoAtk4;
    public Vector3 rotacaoAtk4;
    public Vector3 escalaAtk4 = Vector3.one; 

    void Update()
    {
        // --- ANIMAÇÕES DE MOVIMENTO ---
        anim.SetFloat("Speed", movementSimulation.currentSpeed);
        anim.SetBool("NoChao", movementSimulation.isGrounded);
        anim.SetBool("IsGliding", movementSimulation.isGliding);
        anim.SetBool("IsAirScooter", movementSimulation.isAirScooter); 
        
        if(movementSimulation.controlePular.action.WasPressedThisFrame() && movementSimulation.isGrounded)
        {
            anim.SetTrigger("Pulo");
        }

        if(movementSimulation.didDoubleJump || movementSimulation.didMountAirScooter)
        {
            anim.SetTrigger("PuloDuplo");
            if (vfxRedemoinho != null && movementSimulation.didDoubleJump)
            {
                vfxRedemoinho.Stop(); 
                vfxRedemoinho.Play(); 
            }
        }
        
        if (movementSimulation.didSopro)
        {
            if (vfxSopro != null)
            {
                vfxSopro.Stop();
                vfxSopro.Play();
            }
            movementSimulation.didSopro = false; 
        }

        // --- ANIMAÇÕES DE COMBATE ---
        if (combatSimulation != null && combatSimulation.didAttack)
        {
            combatSimulation.didAttack = false;

            if (combatSimulation.comboStep == 1) 
            {
                anim.SetTrigger("Atk1");
                TocarVFX(vfxCorteNormal, posicaoAtk1, rotacaoAtk1, escalaAtk1);
            }
            else if (combatSimulation.comboStep == 2) 
            {
                anim.SetTrigger("Atk2");
                TocarVFX(vfxCorteNormal, posicaoAtk2, rotacaoAtk2, escalaAtk2);
            }
            else if (combatSimulation.comboStep == 3) 
            {
                anim.SetTrigger("Atk3");
                TocarVFX(vfxCorteCircular, posicaoAtk3, rotacaoAtk3, escalaAtk3);
            }
            else if (combatSimulation.comboStep == 4) 
            {
                anim.SetTrigger("Atk4");
                TocarVFX(vfxCorteCircular, posicaoAtk4, rotacaoAtk4, escalaAtk4);
            }
        }
    }

    // A MÁGICA: Agora também aplica a Escala!
    // A MÁGICA ATUALIZADA: Força a limpeza e o reinício da partícula!
    private void TocarVFX(ParticleSystem vfx, Vector3 pos, Vector3 rot, Vector3 escala)
    {
        if (vfx != null)
        {
            // 1. Pára o efeito imediatamente e limpa qualquer rastro da tela
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
            
            // 2. Reposiciona, gira e espelha
            vfx.transform.localPosition = pos;
            vfx.transform.localEulerAngles = rot;
            vfx.transform.localScale = escala; 
            
            // 3. Zera o relógio interno da Unity para esta partícula
            vfx.time = 0f; 
            
            // 4. Dispara com força total como se fosse a primeira vez
            vfx.Play();
        }
    }
}