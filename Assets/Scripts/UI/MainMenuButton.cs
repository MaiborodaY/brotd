using UnityEngine;

/// <summary>
/// Вешается на кнопку "Main Menu" — возвращает в главное меню.
/// </summary>
public class MainMenuButton : MonoBehaviour
{
    public void GoToMainMenu()
    {
        SceneLoader.LoadMainMenu();
    }
}
