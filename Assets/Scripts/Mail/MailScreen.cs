using Core;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mail
{
    /// <summary>
    /// "Почта" window: inbox (fixed MailRowView slots, newest first) and the opened letter with
    /// "Вернуться к сохранению". Opened by the Почта button on Home (persistent SetActive calls, like
    /// Settings); closing here turns Home back on. Restoring reloads the scene so every screen reads the
    /// restored save from scratch.
    /// </summary>
    public class MailScreen : MonoBehaviour
    {
        [SerializeField] private GameObject homeWindow;
        [SerializeField] private Button closeButton;

        [Header("Inbox")]
        [SerializeField] private GameObject inboxPage;
        [Tooltip("Fixed slots, top to bottom. Unused ones are hidden.")]
        [SerializeField] private MailRowView[] rows;
        [Tooltip("Shown when there are no letters.")]
        [SerializeField] private GameObject emptyState;
        [Tooltip("\"2 новых\" next to the title. Optional.")]
        [SerializeField] private TMP_Text unreadLabel;

        [Header("Letter")]
        [SerializeField] private GameObject letterPage;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text letterSenderLabel;
        [SerializeField] private TMP_Text letterSubjectLabel;
        [SerializeField] private TMP_Text letterDateLabel;
        [SerializeField] private TMP_Text letterBodyLabel;
        [SerializeField] private Button restoreButton;
        [SerializeField] private ConfirmDialog confirmDialog;

        private MailService _mail;
        private MailLetter _openLetter;

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            backButton.onClick.AddListener(ShowInbox);
            restoreButton.onClick.AddListener(AskRestore);
        }

        private void Start()
        {
            _mail = ServiceLocator.Get<MailService>();
            Activate();
        }

        private void OnEnable()
        {
            if (_mail != null)
            {
                Activate();
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MailChangedEvent>(OnMailChanged);
        }

        private void Activate()
        {
            EventBus.Unsubscribe<MailChangedEvent>(OnMailChanged);
            EventBus.Subscribe<MailChangedEvent>(OnMailChanged);
            confirmDialog.gameObject.SetActive(false);
            ShowInbox();
        }

        private void OnMailChanged(MailChangedEvent e)
        {
            if (inboxPage.activeSelf)
            {
                DrawInbox();
            }
        }

        private void ShowInbox()
        {
            _openLetter = null;
            letterPage.SetActive(false);
            inboxPage.SetActive(true);
            DrawInbox();
        }

        private void DrawInbox()
        {
            var letters = _mail.Letters;
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < letters.Count;
                rows[i].gameObject.SetActive(used);
                if (used)
                {
                    rows[i].Bind(letters[i], OpenLetter);
                }
            }

            if (emptyState != null)
            {
                emptyState.SetActive(letters.Count == 0);
            }

            if (unreadLabel != null)
            {
                int unread = _mail.UnreadCount;
                unreadLabel.text = unread > 0 ? $"{unread} {RuPlural.Pick(unread, "новое", "новых", "новых")}" : "";
            }
        }

        private void OpenLetter(MailLetter letter)
        {
            _openLetter = letter;
            _mail.MarkRead(letter.day);

            letterSenderLabel.text = MailService.Sender;
            letterSubjectLabel.text = MailFormat.Subject(letter);
            letterDateLabel.text = MailFormat.ShortDate(letter);
            letterBodyLabel.text = MailFormat.Body(letter);

            inboxPage.SetActive(false);
            letterPage.SetActive(true);
        }

        private void AskRestore()
        {
            if (_openLetter == null)
            {
                return;
            }

            MailLetter letter = _openLetter;
            confirmDialog.Show(MailFormat.ConfirmRestore(letter), () => Restore(letter));
        }

        private void Restore(MailLetter letter)
        {
            if (!_mail.TryRestore(letter.day))
            {
                return;
            }

            DOTween.KillAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Close()
        {
            gameObject.SetActive(false);
            homeWindow.SetActive(true);
        }
    }
}
