using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 재바인딩한 키 설정을 PlayerPrefs에 저장/로드하는 공용 유틸리티.
/// GameControls 인스턴스를 새로 만드는 모든 곳(SimpleCameraController, UnitSelectionController,
/// PlayerDeploymentController, KeyRebindController)이 생성 직후 LoadOverrides를 호출해야,
/// 재바인딩한 키가 실제 게임플레이 입력에도 반영된다.
/// </summary>
public static class InputBindingUtility
{
    private const string PrefKey = "InputBindingOverrides";

    public static void LoadOverrides(InputActionAsset asset)
    {
        if (PlayerPrefs.HasKey(PrefKey))
            asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PrefKey));
    }

    public static void SaveOverrides(InputActionAsset asset)
    {
        PlayerPrefs.SetString(PrefKey, asset.SaveBindingOverridesAsJson());
    }
}