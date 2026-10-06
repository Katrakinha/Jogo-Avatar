using UnityEngine;
using System.Collections;

public class LegoExplosion : MonoBehaviour
{
    [Header("Configuração")]
    public LegoDeathData deathData;
    public Renderer[] renderersCorpo; 

    private Color[] coresOriginais;

    void Start()
    {
        // Guarda as cores originais da pintura mal o jogo começa
        if (renderersCorpo != null && renderersCorpo.Length > 0)
        {
            coresOriginais = new Color[renderersCorpo.Length];
            for (int i = 0; i < renderersCorpo.Length; i++)
            {
                if (renderersCorpo[i] != null) coresOriginais[i] = renderersCorpo[i].material.color;
            }
        }
    }

    // === SISTEMA DE PISCAR ===
    public void PiscarVermelho()
    {
        if (deathData == null || renderersCorpo == null) return;
        StopAllCoroutines(); 
        StartCoroutine(RotinaPiscar());
    }

    private IEnumerator RotinaPiscar()
    {
        for (int i = 0; i < renderersCorpo.Length; i++)
        {
            if (renderersCorpo[i] != null) renderersCorpo[i].material.color = deathData.corDano;
        }
        
        yield return new WaitForSeconds(deathData.tempoPiscar);
        
        for (int i = 0; i < renderersCorpo.Length; i++)
        {
            if (renderersCorpo[i] != null) renderersCorpo[i].material.color = coresOriginais[i];
        }
    }

    // === SISTEMA DE EXPLOSÃO ===
    public void Explodir()
    {
        if (deathData == null) return;

        Vector3 centroExplosao = transform.position + (Vector3.up * 0.5f);

        for (int i = 0; i < renderersCorpo.Length; i++)
        {
            if (renderersCorpo[i] != null)
            {
                // Devolve a cor original caso ele morra enquanto estava vermelho
                renderersCorpo[i].material.color = coresOriginais[i];

                GameObject peca = renderersCorpo[i].gameObject;
                peca.transform.SetParent(null); 

                Rigidbody rbPeca = peca.AddComponent<Rigidbody>();
                rbPeca.mass = deathData.pesoPecas;

                string nomePeca = peca.name.ToLower();
                if (nomePeca.Contains("cabeça") || nomePeca.Contains("cabeca") || nomePeca.Contains("head"))
                {
                    SphereCollider sc = peca.AddComponent<SphereCollider>();
                    sc.radius = sc.radius * 0.6f; 
                }
                else
                {
                    BoxCollider bc = peca.AddComponent<BoxCollider>();
                    bc.size = bc.size * 0.6f; 
                }

                rbPeca.AddExplosionForce(deathData.forcaExplosao, centroExplosao, deathData.raioExplosao, deathData.impulsoParaCima);
                Destroy(peca, Random.Range(deathData.tempoSumirPecasMin, deathData.tempoSumirPecasMax));
            }
        }
        Destroy(gameObject);
    }
}