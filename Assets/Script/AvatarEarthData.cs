using UnityEngine;

[CreateAssetMenu(fileName = "Data_AvatarEarth", menuName = "Avatar/Earth Data")]
public class AvatarEarthData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject rockPrefab;

    [Header("Anel 1 (Mais Próximo)")]
    public int qtdRochasAnel1 = 6;
    public float raioAnel1 = 2f;
    public float atrasoAnel1 = 0f; // Tempo para sair do chão após a mão bater

    [Header("Anel 2 (Médio)")]
    public int qtdRochasAnel2 = 12;
    public float raioAnel2 = 4.5f;
    public float atrasoAnel2 = 0.15f; 

    [Header("Anel 3 (Mais Longe)")]
    public int qtdRochasAnel3 = 18;
    public float raioAnel3 = 7f;
    public float atrasoAnel3 = 0.15f; 

    [Header("Visual e Variação (Orgânico)")]
    public Vector3 escalaBase = Vector3.one;
    public float variacaoEscalaMin = 0.8f; // Pode encolher até 80%
    public float variacaoEscalaMax = 1.4f; // Pode crescer até 140%
    public float profundidadeEscondida = 3.5f; // Quão fundo a pedra nasce antes de subir

    [Header("Física das Pedras")]
    public float velocidadeSubida = 15f;
    public float tempoNoTopo = 1.2f; // Tempo que o Aang fica com a mão colada no chão
    public float velocidadeDescida = 8f;
}