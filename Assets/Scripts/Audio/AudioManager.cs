using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private const string VolumePrefKey = "MasterVolume";
    private const string MixerParam = "MasterVolume";

    public static AudioManager Instance { get; private set; }

    public SfxLibrary library;
    public AudioMixerGroup sfxMixerGroup; // 기존 AudioMixer의 그룹에 연결

    public float MasterVolume { get; private set; } = 1f;


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

        MasterVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
    }

    private void Start()
    {
        ApplyToMixer(MasterVolume); // 믹서 값 설정은 Awake가 아니라 Start에서
    }

    public void SetMasterVolume(float linear)
    {
        MasterVolume = Mathf.Clamp01(linear);
        ApplyToMixer(MasterVolume);
        PlayerPrefs.SetFloat(VolumePrefKey, MasterVolume);
    }

    private void ApplyToMixer(float linear)
    {
        if (sfxMixerGroup == null)
            return;

        float dB = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        sfxMixerGroup.audioMixer.SetFloat(MixerParam, dB);
    }


    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        source.PlayOneShot(clip);
    }
}