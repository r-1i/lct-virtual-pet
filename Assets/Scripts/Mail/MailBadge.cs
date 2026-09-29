using Core;
using TMPro;
using UnityEngine;

namespace Mail
{
    /// <summary>Red counter of unread letters on the Home "Почта" button. Hidden when everything is read.</summary>
    public class MailBadge : MonoBehaviour
    {
        [SerializeField] private GameObject badge;
        [SerializeField] private TMP_Text countLabel;

        private MailService _mail;

        // First Get/Subscribe in Start — see StatBarView for why not OnEnable.
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
            Draw();
        }

        private void OnMailChanged(MailChangedEvent e) => Draw();

        private void Draw()
        {
            int unread = _mail.UnreadCount;
            badge.SetActive(unread > 0);
            countLabel.text = unread > 9 ? "9+" : unread.ToString();
        }
    }
}
