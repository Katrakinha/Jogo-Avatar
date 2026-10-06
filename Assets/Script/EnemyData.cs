using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Inimigo/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Status")]
    public float vidaMaxima = 50f;
    public float tempoStunDano = 0.5f;

    [Header("Movimento da Patrulha")]
    public float velocidadePatrulha = 2f;
    public float tempoDeEsperaNoPonto = 2f; 
    public bool olharAoRedorNoPonto = true; 

    [Header("Sistema de Visão")]
    public float raioSuspeita = 12f;
    [Range(0, 360)] public float anguloSuspeita = 120f;
    public float raioVisao = 6f;
    [Range(0, 360)] public float anguloVisao = 75f;
    public float tempoParaDescobrir = 2f; 
    public float raioFuga = 15f; 

    [Header("Combate Corpo a Corpo")]
    public float danoMelee = 0.5f;
    public float knockbackMelee = 4f;
    public float tempoEntreAtaques = 2f; // Cooldown para ele não "metralhar" socos
    public float distanciaAtaqueMelee = 1.5f; // Distância para começar a bater

    [Header("Ataque à Distância (Fogo)")]
    public bool atiradorDeFogo = false; // Se marcar isto, ele vira ranged!
    public GameObject projetilFogoPrefab;
    public float distanciaAtaqueFogo = 8f; // Distância de onde ele começa a atirar

    [Header("Perseguição")]
    public float velocidadePerseguicao = 5f;

}