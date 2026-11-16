using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ResetarCenaSegurando : MonoBehaviour
{
    [Header("Configurações de Tempo")]
    public float tempoSegurandoParaResetar = 1.5f;

    [Header("Input Action (Opcional)")]
    public InputActionReference acaoResetarOpcional;

    [Header("Leitura Direta do Controle / Teclado")]
    public bool lerSelectEOptionsAutomatico = true;
    public bool permitirTeclaRNoTeclado = true;

    [Header("Estado (Apenas Leitura)")]
    [Range(0f, 1f)]
    public float progressoAtual = 0f;

    private float timerSegurando = 0f;
    private bool jaResetou = false;

    void OnEnable()
    {
        if (acaoResetarOpcional != null && acaoResetarOpcional.action != null)
        {
            acaoResetarOpcional.action.Enable();
        }
    }

    void OnDisable()
    {
        if (acaoResetarOpcional != null && acaoResetarOpcional.action != null)
        {
            acaoResetarOpcional.action.Disable();
        }
    }

    void Update()
    {
        if (jaResetou) return;

        if (EstaSegurandoBotaoReset())
        {
            timerSegurando += Time.unscaledDeltaTime;
            progressoAtual = Mathf.Clamp01(timerSegurando / Mathf.Max(0.01f, tempoSegurandoParaResetar));

            if (timerSegurando >= tempoSegurandoParaResetar)
            {
                jaResetou = true;
                ReiniciarCenaAtual();
            }
        }
        else
        {
            timerSegurando = 0f;
            progressoAtual = 0f;
        }
    }

    private bool EstaSegurandoBotaoReset()
    {
        if (acaoResetarOpcional != null && acaoResetarOpcional.action != null && acaoResetarOpcional.action.IsPressed())
        {
            return true;
        }

        if (lerSelectEOptionsAutomatico && Gamepad.current != null)
        {
            if (Gamepad.current.selectButton.isPressed || Gamepad.current.startButton.isPressed)
            {
                return true;
            }
        }

        if (permitirTeclaRNoTeclado && Keyboard.current != null)
        {
            if (Keyboard.current.rKey.isPressed || Keyboard.current.backspaceKey.isPressed)
            {
                return true;
            }
        }

        return false;
    }

    public void ReiniciarCenaAtual()
    {
        Time.timeScale = 1f;
        Scene cenaAtiva = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtiva.buildIndex);
    }
}