using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Простой менеджер переходов между сценами.
/// </summary>
public static class SceneLoader
{
    public const string MainMenu    = "MainMenu";
    public const string SinglePlayer = "Main";
    public const string PvP         = "PvP";

    public static void LoadMainMenu()     => SceneManager.LoadScene(MainMenu);
    public static void LoadSinglePlayer() => SceneManager.LoadScene(SinglePlayer);
    public static void LoadPvP()          => SceneManager.LoadScene(PvP);
}
