using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public SfxLibrary library;
    public AudioMixerGroup sfxMixerGroup; // 기존 AudioMixer의 그룹에 연결

    private AudioSource source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.playOnAwake = false;
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        source.PlayOneShot(clip);
    }
}