using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    private Camera camPrincipal;

    void Start()
    {
        camPrincipal = Camera.main;
    }

    void LateUpdate()
    {
        // Força o objeto a ficar virado exatamente para a mesma direção da câmara
        if (camPrincipal != null)
        {
            transform.rotation = camPrincipal.transform.rotation;
        }
    }
}