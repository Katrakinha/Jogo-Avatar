using UnityEngine;

[CreateAssetMenu(fileName = "Data_Waterbending", menuName = "Avatar/Waterbending Data")]
public class AvatarWaterData : ScriptableObject
{
    [Header("Limites e Distância")]
    public int maxPilaresSimultaneos = 3; // Limite estilo Zelda
    public float alcanceDaMira = 50f;     // Quão longe ele consegue criar gelo

    [Header("Formato do Pilar")]
    public Vector3 escalaDoPilar = new Vector3(2f, 4f, 2f); // Largura, Altura, Profundidade
    public float tempoDeSubida = 0.4f;    // Quão rápido ele "nasce" da água
}