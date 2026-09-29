using UnityEngine;

namespace UI
{
    /// <summary>
    /// A window with several pages (Настройки: Main / Профиль родителя / Debug Menu): every time the window is opened it
    /// starts on the first page, whatever page it was closed on. Put it on the window root; pages switch each other with
    /// plain Button → SetActive calls.
    /// </summary>
    public class WindowPages : MonoBehaviour
    {
        [Tooltip("[0] is shown on open, the rest are hidden.")]
        [SerializeField] private GameObject[] pages = new GameObject[0];

        private void OnEnable()
        {
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                {
                    pages[i].SetActive(i == 0);
                }
            }
        }
    }
}
