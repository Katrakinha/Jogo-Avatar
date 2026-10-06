using UnityEngine;

[CreateAssetMenu(fileName = "NewAvatarCombatData", menuName = "Avatar/Combat Data")]
public class AvatarCombatData : ScriptableObject
{
    [Header("Timers de Combate")]
    public float tempoJanelaCombo = 1.0f;

    [Header("Impulsos de Ataque")]
    public float avancoAtk1 = 4f;
    public float avancoAtk2 = 5f;

    [Header("Configurações da Hitbox")]
    public float danoAtaqueBase = 10f; 
    public float forcaKnockback = 3f; // <-- ADICIONADO: Força do empurrão
    public LayerMask layerInimigos; 
}