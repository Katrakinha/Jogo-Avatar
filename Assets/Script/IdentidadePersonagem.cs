using UnityEngine;

public enum PersonagemID
{
    QualquerUm,
    Aang,
    Katara
}

public class IdentidadePersonagem : MonoBehaviour
{
    [Header("Crachá de Identidade")]
    public PersonagemID idDoPersonagem = PersonagemID.Aang;
    public bool isPlayerControlled = false;

    [Header("Câmera e Feedback")]
    public Transform pontoDeCamera;
    public ParticleSystem particulaSimboloChao;
}