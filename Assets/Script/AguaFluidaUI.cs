using UnityEngine;

public class AguaFluidaUI : MonoBehaviour
{
    [Header("Alvo a Seguir")]
    [Tooltip("Arraste o objeto da MiraUI para cá, ou o script vai procurá-lo sozinho pela Tag 'MiraAgua'")]
    public RectTransform alvoMiraUI;
    
    [Header("Configurações de Movimento")]
    [Tooltip("Quão rápido a água alcança a mira (valores menores = mais 'atraso')")]
    public float suavidadeMovimento = 0.1f;
    [Tooltip("O 'tamanho' do rastro que ela faz (distância Z da câmera)")]
    public float profundidadeNaCamera = 2f;
    
    [Header("Configurações de Distorção (Esticar)")]
    [Tooltip("Quão elástica a água é (valores maiores esticam mais)")]
    public float multiplicadorDistorcao = 0.5f;
    [Tooltip("Tamanho original da bolha de água (escala Y/Z/X base)")]
    public float tamanhoBase = 1f;
    [Tooltip("Qual o limite máximo que ela pode esticar para não virar um espaguete infinito")]
    public float esticamentoMaximo = 2.5f;

    private Camera camPrincipal;
    private Vector3 velocidadeAtualMovimento; // Usado pelo SmoothDamp
    private Vector3 ultimaPosicao;

    void Start()
    {
        camPrincipal = Camera.main;
        
        // Tenta achar a mira se você esquecer de colocar no Inspector
        if (alvoMiraUI == null)
        {
            GameObject miraEncontrada = GameObject.FindGameObjectWithTag("MiraAgua");
            if (miraEncontrada != null)
            {
                alvoMiraUI = miraEncontrada.GetComponent<RectTransform>();
            }
        }
        
        ultimaPosicao = transform.position;
    }

    void LateUpdate()
    {
        if (alvoMiraUI == null || camPrincipal == null) return;

        // 1. ONDE A MIRA ESTÁ NO MUNDO 3D?
        // Transforma a posição 2D do Canvas (miraUI) num ponto 3D na frente da câmera
        Vector3 posicaoMiraNaTela = alvoMiraUI.position;
        posicaoMiraNaTela.z = profundidadeNaCamera; 
        Vector3 posicaoAlvo3D = camPrincipal.ScreenToWorldPoint(posicaoMiraNaTela);

        // Deixa a bolha "um pouquinho mais embaixo" como você pediu
        posicaoAlvo3D += camPrincipal.transform.up * -0.2f;

        // 2. MOVIMENTO SUAVE (SmoothDamp)
        transform.position = Vector3.SmoothDamp(transform.position, posicaoAlvo3D, ref velocidadeAtualMovimento, suavidadeMovimento);

        // 3. EFEITO DE DISTORÇÃO (ESTICAR COMO GOTA)
        CalcularEsticamentoVisual();
    }

    private void CalcularEsticamentoVisual()
    {
        // Pega a velocidade real do movimento da bolha neste frame
        Vector3 direcaoMovimento = transform.position - ultimaPosicao;
        float velocidadeMovimento = direcaoMovimento.magnitude / Time.deltaTime;

        // Se estiver se mexendo o suficiente, começa a distorcer
        if (velocidadeMovimento > 0.1f)
        {
            // Aponta a ponta fina da "gota" para onde ela está indo
            transform.rotation = Quaternion.LookRotation(direcaoMovimento, camPrincipal.transform.up);

            // Calcula o quanto vai esticar (eixo Z) e achatar (eixos X e Y)
            float distorcaoZ = tamanhoBase + (velocidadeMovimento * multiplicadorDistorcao * 0.01f);
            distorcaoZ = Mathf.Clamp(distorcaoZ, tamanhoBase, esticamentoMaximo);
            
            // Para não ganhar volume do nada, se esticou no Z, ela encolhe um pouco nas laterais
            float distorcaoLateral = tamanhoBase - ((distorcaoZ - tamanhoBase) * 0.3f);
            
            transform.localScale = new Vector3(distorcaoLateral, distorcaoLateral, distorcaoZ);
        }
        else
        {
            // Parou de mexer? Volta ao tamanho normal de bolha gordinha e aponta pra câmera
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * tamanhoBase, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, camPrincipal.transform.rotation, Time.deltaTime * 10f);
        }

        ultimaPosicao = transform.position;
    }
}