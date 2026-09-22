using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 폴더 하나(idle.png/walk.png/combat.png/slash.png/thrust.png/shoot.png가 들어있는, 이미 슬라이스된)를
/// 골라서 CharacterAnimationSet 에셋을 자동으로 채워 생성한다.
/// BatchGridSlicer로 먼저 슬라이싱을 끝낸 폴더에 대해서만 동작한다.
/// </summary>
public static class AnimationSetBuilder
{
    private static readonly string[] AnimationFileNames = { "idle", "walk", "combat", "slash", "thrust", "shoot" };

    [MenuItem("Tools/LPC/Build CharacterAnimationSet From Folder")]
    private static void BuildFromSelectedFolder()
    {
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning("Project 창에서 idle.png 등이 들어있는 폴더를 선택하세요.");
            return;
        }

        CharacterAnimationSet set = ScriptableObject.CreateInstance<CharacterAnimationSet>();
        int foundCount = 0;

        foreach (string animationName in AnimationFileNames)
        {
            string texturePath = $"{folderPath}/{animationName}.png";
            Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().ToArray();

            if (allSprites.Length == 0)
                continue; // 이 애니메이션 파일이 없거나, 아직 슬라이싱 안 됨

            AssignField(set, animationName, BuildDirectionalFrames(allSprites));
            foundCount++;
        }

        if (foundCount == 0)
        {
            Debug.LogWarning($"{folderPath} 안에서 idle/walk/combat/slash/thrust/shoot.png를 하나도 못 찾았어요. 슬라이싱이 먼저 끝나야 해요.");
            Object.DestroyImmediate(set);
            return;
        }

        string folderName = new DirectoryInfo(folderPath).Name;
        string assetPath = $"{folderPath}/{folderName}_CharacterAnimationSet.asset";
        AssetDatabase.CreateAsset(set, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"생성됨: {assetPath} ({foundCount}개 애니메이션 채움)");
        Selection.activeObject = set;
    }

    private static CharacterAnimationSet.DirectionalFrames BuildDirectionalFrames(Sprite[] sprites)
    {
        var byRow = new Dictionary<int, List<(int col, Sprite sprite)>>();

        foreach (var sprite in sprites)
        {
            // 이름 형식: {파일이름}_{행}_{열} (BatchGridSlicer가 붙인 이름)
            string[] parts = sprite.name.Split('_');
            if (parts.Length < 3)
                continue;

            if (!int.TryParse(parts[parts.Length - 2], out int row))
                continue;
            if (!int.TryParse(parts[parts.Length - 1], out int col))
                continue;

            if (!byRow.ContainsKey(row))
                byRow[row] = new List<(int, Sprite)>();

            byRow[row].Add((col, sprite));
        }

        Sprite[] GetRow(int row) =>
            byRow.TryGetValue(row, out var list)
                ? list.OrderBy(e => e.col).Select(e => e.sprite).ToArray()
                : new Sprite[0];

        return new CharacterAnimationSet.DirectionalFrames
        {
            up = GetRow(0),
            left = GetRow(1),
            down = GetRow(2),
            right = GetRow(3)
        };
    }

    private static void AssignField(CharacterAnimationSet set, string animationName, CharacterAnimationSet.DirectionalFrames frames)
    {
        switch (animationName)
        {
            case "idle": set.idle = frames; break;
            case "walk": set.walk = frames; break;
            case "combat": set.combatIdle = frames; break;
            case "slash": set.slash = frames; break;
            case "thrust": set.thrust = frames; break;
            case "shoot": set.shoot = frames; break;
        }
    }
}