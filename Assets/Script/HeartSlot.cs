using UnityEngine;

public class HeartSlot : MonoBehaviour
{
    [Header("Os 3 Modelos 3D Filhos")]
    public GameObject modeloCheio;
    public GameObject modeloMetade;
    public GameObject modeloVazio;

    void Start()
    {
        // Força os modelos a ficarem no centro do slot (X=0, Y=0) e puxados para a frente da tela (Z=-50)
        // Isso impede o Layout Group de empurrá-los para trás da câmara!
        if (modeloCheio != null) modeloCheio.transform.localPosition = new Vector3(0, 0, -50f);
        if (modeloMetade != null) modeloMetade.transform.localPosition = new Vector3(0, 0, -50f);
        if (modeloVazio != null) modeloVazio.transform.localPosition = new Vector3(0, 0, -50f);
    }

    public void MudarEstado(int estado)
    {
        if (modeloVazio != null) modeloVazio.SetActive(estado == 0);
        if (modeloMetade != null) modeloMetade.SetActive(estado == 1);
        if (modeloCheio != null) modeloCheio.SetActive(estado == 2);
    }
}