using UnityEngine;

[CreateAssetMenu(fileName = "SfxLibrary", menuName = "Strategy/Audio/Sfx Library")]
public class SfxLibrary : ScriptableObject
{
    [Header("공통 UI 사운드")]
    public AudioClip buttonClick;
    public AudioClip popupOpen;
    public AudioClip popupClose;

    [Header("전투 공통 사운드")]
    public AudioClip hitLight;
    public AudioClip hitCritical;
    public AudioClip missWhiff;
    public AudioClip blockClang;

    [Header("결과 사운드")]
    public AudioClip victory;
    public AudioClip defeat;
}