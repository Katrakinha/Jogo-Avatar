using UnityEngine;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    [Header("Banco de Dados")]
    public AvatarStatusData dataAvatar;

    [Header("Configuração da HUD")]
    public Transform containerCoracoes; 
    public GameObject prefabCoracaoUI;  

    private List<HeartSlot> listaCoracoes = new List<HeartSlot>();

    void Start()
    {
        AtualizarHUD();
    }

    void Update()
    {
        AtualizarHUD();
    }

    public void AtualizarHUD()
    {
        if (dataAvatar == null) 
        {
            Debug.LogWarning("⚠️ ALARME HUD: Esqueceste-te de arrastar o AvatarStatusData para o HUDManager!");
            return;
        }

        if (dataAvatar.coracoesMaximos <= 0)
        {
            Debug.LogWarning("⚠️ ALARME HUD: Os Corações Máximos no AvatarStatusData estão a ZERO! O Aang precisa ter pelo menos 1 coração para aparecer na tela.");
            return;
        }

        // Cria os corações
        while (listaCoracoes.Count < dataAvatar.coracoesMaximos)
        {
            GameObject novoCoracao = Instantiate(prefabCoracaoUI, containerCoracoes);
            HeartSlot slot = novoCoracao.GetComponent<HeartSlot>();
            
            if (slot == null)
            {
                Debug.LogError("🚨 ERRO HUD: O teu Prefab 'CoraçãoSlot' não tem o script 'HeartSlot' anexado!");
                return;
            }

            listaCoracoes.Add(slot);
            Debug.Log("✅ HUD: Criei um coração novo na tela com sucesso!");
        }

        // Liga os modelos
        for (int i = 0; i < listaCoracoes.Count; i++)
        {
            if (i < Mathf.FloorToInt(dataAvatar.vidaAtual))
            {
                listaCoracoes[i].MudarEstado(2); 
            }
            else if (i == Mathf.FloorToInt(dataAvatar.vidaAtual) && (dataAvatar.vidaAtual % 1) != 0)
            {
                listaCoracoes[i].MudarEstado(1); 
            }
            else
            {
                listaCoracoes[i].MudarEstado(0); 
            }
        }
    }
}