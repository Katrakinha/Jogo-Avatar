using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class GanchoModular : MonoBehaviour
{
    [Header("Configuração do Minigame")]
    public float forcaPorClique = 0.15f;
    public float velocidadeDecaimento = 0.2f;
    
    [Header("Referências Visuais")]
    [Tooltip("Onde o chicote gruda no modelo 3D do gancho")]
    public Transform pontoDeFixacao;
    public Slider barraProgressoUI;
    
    [Header("O Que Acontece ao Vencer?")]
    [Tooltip("Aqui você conecta a Caixa, a Ponte, a Porta... Funciona igual a um Botão de UI!")]
    public UnityEvent AoVencerMinigame;

    private bool minigameAtivo = false;
    private float progressoAtual = 0f;
    private PlayerWaterHook jogadorConectado;

    void Start()
    {
        if (barraProgressoUI != null) barraProgressoUI.gameObject.SetActive(false);
    }

    void Update()
    {
        if (minigameAtivo)
        {
            progressoAtual -= velocidadeDecaimento * Time.deltaTime;
            progressoAtual = Mathf.Clamp01(progressoAtual);

            if (barraProgressoUI != null) barraProgressoUI.value = progressoAtual;

            if (progressoAtual >= 1f)
            {
                ExecutarVitoria();
            }
        }
    }

    public void ComecarMinigame(PlayerWaterHook jogador)
    {
        jogadorConectado = jogador;
        minigameAtivo = true;
        progressoAtual = 0f;
        
        if (barraProgressoUI != null)
        {
            barraProgressoUI.gameObject.SetActive(true);
            barraProgressoUI.value = 0f;
        }
    }

    public void ReceberForca()
    {
        if (minigameAtivo) progressoAtual += forcaPorClique;
    }

    public void CancelarMinigame()
    {
        minigameAtivo = false;
        if (barraProgressoUI != null) barraProgressoUI.gameObject.SetActive(false);
        progressoAtual = 0f;
        jogadorConectado = null;
    }

    private void ExecutarVitoria()
    {
        minigameAtivo = false;
        if (barraProgressoUI != null) barraProgressoUI.gameObject.SetActive(false);

        if (jogadorConectado != null) jogadorConectado.VencerMinigame();

        // A MÁGICA: O Gancho avisa a quem estiver conectado para fazer a sua ação!
        AoVencerMinigame.Invoke();

        gameObject.SetActive(false);
    }
}