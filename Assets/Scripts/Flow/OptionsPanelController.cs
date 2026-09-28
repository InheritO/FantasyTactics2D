using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// 옵션 패널. 마스터 볼륨을 AudioMixer의 노출된 파라미터로 조절한다.
/// 믹서 파라미터는 데시벨(dB) 단위라 슬라이더(0~1 선형값)를 로그 변환해서 넘긴다.
/// </summary>
public class OptionsPanelController : MonoBehaviour
{
    public Slider masterVolumeSlider;

    void OnEnable()
    {
        if (AudioManager.Instance != null)
            masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);

        masterVolumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
    }

    void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
    }

    private void HandleVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }
}