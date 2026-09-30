using UnityEngine;

[CreateAssetMenu(fileName = "Data_AvatarCombat", menuName = "Avatar/Combat Data")]
public class AvatarCombatData : ScriptableObject
{
    [Header("Sistema de Combo")]
    [Tooltip("Tempo máximo que o jogador tem para apertar o botão e continuar o combo")]
    public float tempoJanelaCombo = 0.6f; 
    
    [Header("Ataque 1")]
    public float danoAtk1 = 10f;
    public float avancoAtk1 = 2f; // Forcinha pra frente ao bater

    [Header("Ataque 2")]
    public float danoAtk2 = 15f;
    public float avancoAtk2 = 3f;

    [Header("Ataque 3 (Em Breve)")]
    public float danoAtk3 = 20f;
    public float avancoAtk3 = 4f;

    [Header("Ataque 4 (Em Breve)")]
    public float danoAtk4 = 30f;
    public float avancoAtk4 = 5f;

    [Header("Tempos de Animação (Para não cortar o golpe)")]
    public float tempoAnimAtk1 = 0.5f; 
    public float tempoAnimAtk2 = 0.6f;
    public float tempoAnimAtk3 = 0.5f;
    public float tempoAnimAtk4 = 0.8f;

    [Header("Configurações de Hitbox (O Arco Invisível)")]
    public float raioDoArco = 1.5f; 
    
    [Tooltip("Ajuste X (Lados), Y (Altura) e Z (Frente/Trás)")]
    public Vector3 hitboxOffset = new Vector3(0f, 1f, 1.5f); 
    
    public LayerMask layerInimigos;
}