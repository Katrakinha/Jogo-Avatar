using System.Collections.Generic;
using UnityEngine;

public class FisicaCabeloLEGO : MonoBehaviour
{
    public int ossosTravadosNoTopo = 3;
    public float forcaMola = 150f;
    public float amortecimento = 10f;
    public float sensibilidadePulo = 1.2f;
    public float quiqueAndando = 0.025f;
    public float frequenciaPassos = 14f;
    public float limiteSubida = 0.06f;
    public float limiteDescida = 0.015f;

    private class DadosOsso
    {
        public Transform osso;
        public Vector3 posicaoLocalInicial;
        public Quaternion rotacaoLocalInicial;
        public float pesoMovimento;
    }

    private List<DadosOsso> todosOsOssos = new List<DadosOsso>();
    private Vector3 posicaoAnterior;
    private float deslocamentoAtualY;
    private float velocidadeY;
    private float tempoPassos;

    void Start()
    {
        ConfigurarOssos();
        posicaoAnterior = transform.position;
    }

    private void ConfigurarOssos()
    {
        todosOsOssos.Clear();

        Transform raizArmature = transform;
        if (raizArmature.name != "Armature")
        {
            Transform pai = transform.parent;
            while (pai != null)
            {
                if (pai.name == "Armature")
                {
                    raizArmature = pai;
                    break;
                }
                pai = pai.parent;
            }
        }

        Transform raizTranca = raizArmature.Find("Bone.001");
        if (raizTranca == null) raizTranca = raizArmature;

        Transform[] filhos = raizArmature.GetComponentsInChildren<Transform>();
        int maiorProfundidade = 1;

        foreach (Transform t in filhos)
        {
            if (t == raizArmature || t.name.EndsWith("_end")) continue;
            if (t.GetComponent<SkinnedMeshRenderer>() != null) continue;

            if (t == raizTranca || t.IsChildOf(raizTranca))
            {
                int prof = CalcularProfundidade(t, raizTranca);
                if (prof > maiorProfundidade) maiorProfundidade = prof;
            }
        }

        int niveisMoveis = Mathf.Max(1, maiorProfundidade - ossosTravadosNoTopo + 1);

        foreach (Transform t in filhos)
        {
            if (t == raizArmature || t.name.EndsWith("_end")) continue;
            if (t.GetComponent<SkinnedMeshRenderer>() != null) continue;

            float peso = 0f;

            if (t == raizTranca || t.IsChildOf(raizTranca))
            {
                int prof = CalcularProfundidade(t, raizTranca);
                if (prof >= ossosTravadosNoTopo)
                {
                    int indiceMovel = (prof - ossosTravadosNoTopo) + 1;
                    float fatorNivel = (float)indiceMovel / niveisMoveis;
                    peso = fatorNivel / niveisMoveis;
                }
            }

            DadosOsso novo = new DadosOsso();
            novo.osso = t;
            novo.posicaoLocalInicial = t.localPosition;
            novo.rotacaoLocalInicial = t.localRotation;
            novo.pesoMovimento = peso;
            todosOsOssos.Add(novo);
        }
    }

    private int CalcularProfundidade(Transform atual, Transform raiz)
    {
        int profundidade = 0;
        Transform temp = atual;
        while (temp != null && temp != raiz)
        {
            profundidade++;
            temp = temp.parent;
        }
        return profundidade;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0.0001f || todosOsOssos.Count == 0) return;

        for (int i = 0; i < todosOsOssos.Count; i++)
        {
            todosOsOssos[i].osso.localPosition = todosOsOssos[i].posicaoLocalInicial;
            todosOsOssos[i].osso.localRotation = todosOsOssos[i].rotacaoLocalInicial;
        }

        Vector3 deltaMovimento = transform.position - posicaoAnterior;
        posicaoAnterior = transform.position;
        deltaMovimento = Vector3.ClampMagnitude(deltaMovimento, 0.3f);

        deslocamentoAtualY -= deltaMovimento.y * sensibilidadePulo;

        float velocidadeHoriz = new Vector2(deltaMovimento.x, deltaMovimento.z).magnitude / dt;
        float alvoY = 0f;

        if (velocidadeHoriz > 0.2f && Mathf.Abs(deltaMovimento.y) < 0.02f)
        {
            tempoPassos += dt * frequenciaPassos;
            float fatorVel = Mathf.Clamp01(velocidadeHoriz / 6f);
            alvoY = Mathf.Abs(Mathf.Sin(tempoPassos)) * quiqueAndando * fatorVel;
        }
        else
        {
            tempoPassos = 0f;
        }

        float forca = (alvoY - deslocamentoAtualY) * forcaMola;
        velocidadeY += forca * dt;
        velocidadeY *= Mathf.Exp(-amortecimento * dt);
        deslocamentoAtualY += velocidadeY * dt;

        deslocamentoAtualY = Mathf.Clamp(deslocamentoAtualY, -limiteDescida, limiteSubida);

        for (int i = 0; i < todosOsOssos.Count; i++)
        {
            if (todosOsOssos[i].pesoMovimento > 0f)
            {
                todosOsOssos[i].osso.position += Vector3.up * (deslocamentoAtualY * todosOsOssos[i].pesoMovimento);
            }
        }
    }
}