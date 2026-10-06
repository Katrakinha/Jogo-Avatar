using UnityEngine;

[CreateAssetMenu(fileName = "NewLegoDeathData", menuName = "Sistema LEGO/Death Data")]
public class LegoDeathData : ScriptableObject
{
    [Header("Efeito de Dano")]
    public Color corDano = new Color(1f, 0f, 0f, 0.5f); // Vermelho padrão
    public float tempoPiscar = 0.15f;

    [Header("Física da Explosão")]
    public float pesoPecas = 1f;
    public float forcaExplosao = 300f;
    public float raioExplosao = 3f;
    public float impulsoParaCima = 1f;

    [Header("Limpeza do Mapa")]
    public float tempoSumirPecasMin = 3f;
    public float tempoSumirPecasMax = 5f;
}