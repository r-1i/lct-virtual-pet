using System;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mail
{
    /// <summary>One fixed inbox slot. MailScreen fills it (Bind) or hides the whole object when there are fewer letters.</summary>
    public class MailRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text senderLabel;
        [SerializeField] private TMP_Text subjectLabel;
        [SerializeField] private TMP_Text previewLabel;
        [SerializeField] private TMP_Text dateLabel;
        [Tooltip("Dot shown while the letter is unread.")]
        [SerializeField] private GameObject unreadMark;
        [Tooltip("Optional: sender/subject get bold while unread.")]
        [SerializeField] private bool boldWhenUnread = true;

        private MailLetter _letter;
        private Action<MailLetter> _onClick;

        private void Awake()
        {
            button.onClick.AddListener(() => _onClick?.Invoke(_letter));
        }

        public void Bind(MailLetter letter, Action<MailLetter> onClick)
        {
            _letter = letter;
            _onClick = onClick;

            senderLabel.text = MailService.Sender;
            subjectLabel.text = MailFormat.Subject(letter);
            previewLabel.text = MailFormat.Preview(letter);
            dateLabel.text = MailFormat.ShortDate(letter);
            unreadMark.SetActive(!letter.read);

            if (boldWhenUnread)
            {
                FontStyles style = letter.read ? FontStyles.Normal : FontStyles.Bold;
                senderLabel.fontStyle = style;
                subjectLabel.fontStyle = style;
            }
        }
    }
}
