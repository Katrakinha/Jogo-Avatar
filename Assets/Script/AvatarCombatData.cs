using UnityEngine;

[CreateAssetMenu(fileName = "NewAvatarCombatData", menuName = "Avatar/Combat Data")]
public class AvatarCombatData : ScriptableObject
{
    [Header("Timers de Combate")]
    public float tempoJanelaCombo = 1.0f;

    [Header("Impulso de Ataque")]
    public float avancoAtaque = 5f; // <-- APENAS UM VALOR PARA TODOS OS GOLPES!

    [Header("Configurações da Hitbox")]
    public float danoAtaqueBase = 10f; 
    public float forcaKnockback = 3f; 
    public LayerMask layerInimigos; 
}