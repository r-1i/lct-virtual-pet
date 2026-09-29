using Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// "Удалить аккаунт" in Настройки → Профиль родителя: after a confirmation deletes the save (save.json, daily
    /// snapshots, mail), resets the debug day offset and reloads the scene — the game starts over from the intro.
    /// Volume and animation settings stay.
    /// </summary>
    public class AccountReset : MonoBehaviour
    {
        [SerializeField] private Button deleteButton;
        [SerializeField] private ConfirmDialog confirmDialog;
        [SerializeField, TextArea] private string confirmText =
            "Удалить аккаунт? Питомец, монеты, покупки, мечты и письма пропадут навсегда, игра начнётся заново.";

        private void Awake()
        {
            deleteButton.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (confirmDialog != null)
            {
                confirmDialog.Show(confirmText, DeleteAndRestart);
            }
            else
            {
                DeleteAndRestart();
            }
        }

        private static void DeleteAndRestart()
        {
            if (!ServiceLocator.Get<SaveService>().DeleteAll())
            {
                Feedback.Show("Не получилось удалить сохранение");
                return;
            }

            GameDay.ResetDebugOffset();
            DOTween.KillAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
