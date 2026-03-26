using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Главное меню — выбор режима игры.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button pvpButton;

    private void Awake()
    {
        singlePlayerButton.onClick.AddListener(SceneLoader.LoadSinglePlayer);
        pvpButton.onClick.AddListener(() => SceneManager.LoadScene("PvPLobby"));
    }
}
