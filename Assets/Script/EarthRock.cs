using UnityEngine;
using System.Collections;

public class EarthRock : MonoBehaviour
{
    private float targetY;
    private float startY;
    private AvatarEarthData data;

    public void Inicializar(AvatarEarthData earthData)
    {
        data = earthData;
        targetY = transform.position.y;
        startY = targetY - data.profundidadeEscondida; 
        
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        
        StartCoroutine(CicloDeVidaRocha());
    }

    private IEnumerator CicloDeVidaRocha()
    {
        // 1. SUBIR
        while (transform.position.y < targetY)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetY, transform.position.z), data.velocidadeSubida * Time.deltaTime);
            yield return null;
        }

        // 2. ESPERAR
        yield return new WaitForSeconds(data.tempoNoTopo);

        // 3. DESCER
        while (transform.position.y > startY)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, startY, transform.position.z), data.velocidadeDescida * Time.deltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }
}