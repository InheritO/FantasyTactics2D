using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// MonoBehaviour의 public 필드 중 UnityEngine.Object 계열(버튼, 텍스트, GameObject 등)이
/// 인스펙터에서 연결되지 않았는지 리플렉션으로 자동 검사한다.
/// 필드를 추가/삭제/이름변경 해도 이 유틸리티나 호출부는 수정할 필요가 없다.
/// </summary>
public static class InspectorFieldValidator
{
    public static bool ValidateAllFieldsAssigned(MonoBehaviour target)
    {
        bool ok = true;
        Type type = target.GetType();

        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // GameObject, Button, TMP_Text 등 UnityEngine.Object를 상속하는 필드만 검사
            if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                continue;

            UnityEngine.Object value = field.GetValue(target) as UnityEngine.Object;

            if (value == null)
            {
                Debug.LogError($"[{target.name}] {field.Name}이(가) 연결되지 않았습니다.", target);
                ok = false;
            }
        }

        return ok;
    }
}