using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    int coins;
    int totalCoins;
    bool won;
    GUIStyle hudStyle;
    GUIStyle bigStyle;

    void Awake() => Instance = this;

    void Start() => totalCoins = Coin.Active.Count;

    public void AddCoin() => coins++;

    public void ReachGoal() => won = true;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = Color.white;
            bigStyle = new GUIStyle(hudStyle) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            bigStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        }

        GUI.Label(new Rect(20, 15, 400, 30), $"Moedas: {coins}/{totalCoins}", hudStyle);
        GUI.Label(new Rect(20, 45, 700, 30), "A/D ou ←/→ mover  ·  Espaço pular  ·  R reiniciar", new GUIStyle(hudStyle) { fontSize = 16, fontStyle = FontStyle.Normal });

        if (won)
        {
            string msg = coins == totalCoins ? "Perfeito! Todas as moedas!" : "Você chegou!";
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), msg, bigStyle);
        }
    }
}
