using UnityEngine;

[CreateAssetMenu(fileName = "Data_AvatarMovement", menuName = "Avatar/Movement Data")]
public class AvatarMovementData : ScriptableObject
{
    [Header("Movimentação Base")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float doubleJumpForce = 6f;
    
    [Header("Sensor de Chão Retangular")]
    public float boxWidth = 0.6f;   
    public float boxHeight = 0.15f; 
    public float boxLength = 0.6f;  
    
    [Header("Configurações Gerais de Queda")]
    public float extraFallGravityMultiplier = 2.0f; 

    [Header("Configurações do Planador")]
    public float glideGravityForce = 25f; 
    public float baseGlideForwardSpeed = 7f;
    public float boostSoproForce = 14f;   
    public int maxSopros = 3;
    public float maxBankAngle = 40f; 

    [Header("Configurações do Patinete Aéreo")]
    public float airScooterSpeed = 9f;  
    public float mountJumpForce = 5f;   
}