using UnityEngine;
using System.Collections;

public class PilarDeGelo : MonoBehaviour
{
    private Vector3 escalaFinal;
    private float tempoSubida;
    private Vector3 pontoDeAncoragem; // Ponto central colado na parede

    [Header("Configuração 3D")]
    [Tooltip("Se o seu modelo 3D for um cubo normal da Unity, deixe marcado. Se você fez no Blender com o pivot na base, desmarque.")]
    public bool cuboComPivotNoCentro = true; 

    public void IniciarSubida(Vector3 alvoEscala, float tempo)
    {
        escalaFinal = alvoEscala;
        tempoSubida = tempo;
        pontoDeAncoragem = transform.position;
        
        // Achata o gelo usando o eixo local Y (funciona perfeitamente de lado ou teto)
        transform.localScale = new Vector3(alvoEscala.x, 0.05f, alvoEscala.z);
        
        StartCoroutine(AnimarCrescimento());
    }

    private IEnumerator AnimarCrescimento()
    {
        float tempoDecorrido = 0f;
        Vector3 escalaInicial = transform.localScale;

        while (tempoDecorrido < tempoSubida)
        {
            // Cresce
            transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, tempoDecorrido / tempoSubida);
            
            // Empurra a malha para frente baseada na sua própria inclinação para compensar o pivot
            if (cuboComPivotNoCentro)
            {
                transform.position = pontoDeAncoragem + (transform.up * (transform.localScale.y / 2f));
            }

            tempoDecorrido += Time.deltaTime;
            yield return null;
        }

        transform.localScale = escalaFinal;
        
        if (cuboComPivotNoCentro)
        {
            transform.position = pontoDeAncoragem + (transform.up * (escalaFinal.y / 2f));
        }
    }
}