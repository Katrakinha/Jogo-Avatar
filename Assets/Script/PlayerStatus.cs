using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Banco de Dados")]
    public AvatarStatusData data;

    void Start()
    {
        if (data != null)
        {
            data.vidaAtual = data.coracoesMaximos;
        }
    }

    public void ReceberDano(float dano, float forcaKnockback, Transform atacante)
    {
        if (data == null || data.vidaAtual <= 0) return;

        data.vidaAtual -= dano;
        Debug.Log("Aang sofreu Dano! Vida restante: " + data.vidaAtual + " Corações.");

        // Chama o efeito universal
        LegoExplosion efeitosLego = GetComponent<LegoExplosion>();
        if (efeitosLego != null) efeitosLego.PiscarVermelho();

        if (data.vidaAtual <= 0)
        {
            Morrer();
        }
    }

    public void ColetarPedacoDeCoracao()
    {
        if (data == null) return;

        data.pedacosAtuais++;
        Debug.Log("Pegou um pedaço de coração! (" + data.pedacosAtuais + "/" + data.pedacosParaNovoCoracao + ")");

        if (data.pedacosAtuais >= data.pedacosParaNovoCoracao)
        {
            data.pedacosAtuais = 0;
            data.coracoesMaximos++;
            data.vidaAtual = data.coracoesMaximos; 
            Debug.Log("### UPGRADE! Novo limite: " + data.coracoesMaximos + " Corações ###");
        }
    }

    private void Morrer()
    {
        Debug.Log("GAME OVER: Aang caiu! EXPLOSÃO DE LEGO!");
        
        LegoExplosion explosao = GetComponent<LegoExplosion>();
        if (explosao != null)
        {
            explosao.Explodir();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}