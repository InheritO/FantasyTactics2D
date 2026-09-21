using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// 옵션 패널. 마스터 볼륨을 AudioMixer의 노출된 파라미터로 조절한다.
/// 믹서 파라미터는 데시벨(dB) 단위라 슬라이더(0~1 선형값)를 로그 변환해서 넘긴다.
/// </summary>
public class OptionsPanelController : MonoBehaviour
{
    private const string VolumePrefKey = "MasterVolume";
    private const string MixerParam = "MasterVolume"; // 믹서에서 노출한 파라미터 이름과 정확히 일치해야 함

    public AudioMixer audioMixer;
    public Slider masterVolumeSlider;

    void OnEnable()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
        ApplyVolume(savedVolume);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(savedVolume);
            masterVolumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }
    }

    void OnDisable()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
    }

    private void HandleVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VolumePrefKey, value);
    }

    private void ApplyVolume(float linearValue)
    {
        float dB = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f; // 0이면 사실상 무음(-80dB)
        if (audioMixer != null)
            audioMixer.SetFloat(MixerParam, dB);
    }
}