using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlaySoundOnClick : MonoBehaviour
{
    [SerializeField] private AudioSource sound;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(Play);
    }

    // Same method on both sides — a new lambda in RemoveListener wouldn't match and the listeners would pile up.
    private void OnDisable()
    {
        button.onClick.RemoveListener(Play);
    }

    private void Play()
    {
        sound.Play();
    }
}
