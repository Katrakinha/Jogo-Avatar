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
    public Vector3 escalaAtk1 = Vector3.one; 

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
    }

    // --- NOVA FUNÇÃO PÚBLICA PARA O COMBATE ---
    // Esta função será chamada diretamente pelo PlayerCombat
    public void DispararAnimacaoDeAtaque(int passoDoCombo)
    {
        if (passoDoCombo == 1) 
        {
            anim.SetTrigger("Atk1");
            TocarVFX(vfxCorteNormal, posicaoAtk1, rotacaoAtk1, escalaAtk1);
        }
        else if (passoDoCombo == 2) 
        {
            anim.SetTrigger("Atk2");
            TocarVFX(vfxCorteNormal, posicaoAtk2, rotacaoAtk2, escalaAtk2);
        }
        else if (passoDoCombo == 3) 
        {
            anim.SetTrigger("Atk3");
            TocarVFX(vfxCorteCircular, posicaoAtk3, rotacaoAtk3, escalaAtk3);
        }
        else if (passoDoCombo == 4) 
        {
            anim.SetTrigger("Atk4");
            TocarVFX(vfxCorteCircular, posicaoAtk4, rotacaoAtk4, escalaAtk4);
        }
    }

    private void TocarVFX(ParticleSystem vfx, Vector3 pos, Vector3 rot, Vector3 escala)
    {
        if (vfx != null)
        {
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
            vfx.transform.localPosition = pos;
            vfx.transform.localEulerAngles = rot;
            vfx.transform.localScale = escala; 
            vfx.time = 0f; 
            vfx.Play();
        }
    }
}