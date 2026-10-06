using UnityEngine;

[CreateAssetMenu(fileName = "NewAvatarStatus", menuName = "Avatar/Status Data")]
public class AvatarStatusData : ScriptableObject
{
    [Header("Vida Principal")]
    public int coracoesMaximos = 3; // Corações totais que ele tem agora
    public float vidaAtual;         // Vida no momento (ex: 2.5)

    [Header("Evolução (Pedaços de Coração)")]
    public int pedacosParaNovoCoracao = 2; // Quantos precisa apanhar para ganhar 1
    public int pedacosAtuais = 0;          // Quantos tens na mochila
}