using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Collections; // Necessário para a Coroutine de viagem da Navi

public class GerenciadorDeGrupo : MonoBehaviour
{
    [Header("Atores do Palco")]
    public List<IdentidadePersonagem> personagensNaParty;

    [Header("Direção de Câmera")]
    public CinemachineCamera cameraDoJogo; 

    [Header("Controle")]
    public InputActionReference controleTrocarPersonagem;

    [Header("Efeitos Visuais de Troca")]
    [Tooltip("Arraste a partícula voadora (Efeito_Navi) da cena para cá")]
    public ParticleSystem particulaNavi;
    [Tooltip("Tempo em segundos que a Navi demora para voar de um boneco ao outro")]
    public float tempoDeVoo = 0.5f;

    private int indiceAtual = 0;
    private bool trocando = false; // Trava para não bugar se apertar Triângulo muito rápido

    void Start()
    {
        if (particulaNavi != null) particulaNavi.Stop(); // Garante que a Navi comece invisível
        AtivarPersonagem(indiceAtual, false); // O false significa: não toque efeitos visuais no começo do jogo
    }

    void OnEnable() { if (controleTrocarPersonagem != null) controleTrocarPersonagem.action.Enable(); }
    void OnDisable() { if (controleTrocarPersonagem != null) controleTrocarPersonagem.action.Disable(); }

    void Update()
    {
        if (controleTrocarPersonagem != null && controleTrocarPersonagem.action.WasPressedThisFrame())
        {
            if (!trocando) 
            {
                TrocarParaProximo();
            }
        }
    }

    private void TrocarParaProximo()
    {
        int indiceAntigo = indiceAtual;
        
        indiceAtual++;
        if (indiceAtual >= personagensNaParty.Count)
        {
            indiceAtual = 0;
        }

        IdentidadePersonagem liderAntigo = personagensNaParty[indiceAntigo];
        IdentidadePersonagem novoLider = personagensNaParty[indiceAtual];

        // Se a Navi existir, inicia o voo mágico!
        if (particulaNavi != null && liderAntigo != null && novoLider != null)
        {
            StartCoroutine(VooDaNavi(liderAntigo.pontoDeCamera.position, novoLider));
        }

        AtivarPersonagem(indiceAtual, true);
    }

    private void AtivarPersonagem(int indiceParaAtivar, bool tocarEfeitoVisual)
    {
        if (personagensNaParty.Count == 0) return;

        for (int i = 0; i < personagensNaParty.Count; i++)
        {
            if (personagensNaParty[i] != null)
            {
                personagensNaParty[i].isPlayerControlled = (i == indiceParaAtivar);
            }
        }

        IdentidadePersonagem liderAtivo = personagensNaParty[indiceParaAtivar];

        if (cameraDoJogo != null && liderAtivo != null)
        {
            cameraDoJogo.Follow = liderAtivo.pontoDeCamera;
            cameraDoJogo.LookAt = liderAtivo.pontoDeCamera;
        }

        // Se não tiver a Navi (ou se for só a troca na mesma hora), acende o chão direto
        if (tocarEfeitoVisual && particulaNavi == null)
        {
            if (liderAtivo.particulaSimboloChao != null) liderAtivo.particulaSimboloChao.Play();
        }
    }

    // A viagem cinematográfica da bolinha
    private IEnumerator VooDaNavi(Vector3 posicaoInicial, IdentidadePersonagem destino)
    {
        trocando = true;
        
        particulaNavi.transform.position = posicaoInicial;
        particulaNavi.Play();

        float tempoDecorrido = 0f;

        while (tempoDecorrido < tempoDeVoo)
        {
            tempoDecorrido += Time.deltaTime;
            float porcentagem = tempoDecorrido / tempoDeVoo;

            // Faz a curva de velocidade da Navi (começa rápido, freia no final)
            float suavizacao = Mathf.SmoothStep(0f, 1f, porcentagem);

            particulaNavi.transform.position = Vector3.Lerp(posicaoInicial, destino.pontoDeCamera.position, suavizacao);
            
            yield return null; // Espera o próximo frame
        }

        particulaNavi.Stop(); // Para de soltar as bolinhas (o rastro some naturalmente)
        
        // Quando a Navi chega no peito do novo boneco, o símbolo bate no chão!
        if (destino.particulaSimboloChao != null) destino.particulaSimboloChao.Play();
        
        trocando = false;
    }
}