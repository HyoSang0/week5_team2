using UnityEngine;

/// <summary>
/// 증강 등급의 표시명과 표시 색상을 제공하는 공용 유틸리티.
/// View들이 공통된 등급 표기를 위해 사용한다.
/// </summary>
public static class AugmentTierStyle
{
    // 등급별 텍스트 색상(실버 회색 / 프리즘 보라)이다. 골드는 UIPalette.Accent를 사용한다.
    private static readonly Color _silverColor = new Color(0.75f, 0.78f, 0.80f, 1f);
    private static readonly Color _prismaticColor = new Color(0.66f, 0.4f, 1f, 1f);

    /// <summary>
    /// tier의 한국어 등급명을 반환한다.
    /// </summary>
    public static string GetTierName(AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.Silver: return "실버";
            case AugmentTier.Gold: return "골드";
            default: return "프리즘";
        }
    }

    /// <summary>
    /// tier에 대응하는 표시 색상을 반환한다.
    /// </summary>
    public static Color GetTierColor(AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.Silver: return _silverColor;
            case AugmentTier.Gold: return UIPalette.Accent;
            default: return _prismaticColor;
        }
    }
}
