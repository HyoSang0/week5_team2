using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 전역에서 공용으로 쓰는 색상 값을 모아 둔 정적 팔레트.
/// 읽기 전용 프로퍼티로 색상을 제공하고 버튼 공용 ColorBlock 생성기도 제공한다.
/// </summary>
public static class UIPalette
{
    /// <summary>기본 글자색.</summary>
    public static Color TextPrimary => new Color(1f, 1f, 1f, 1f);

    /// <summary>선택 강조와 승리 문구에 쓰는 강조색.</summary>
    public static Color Accent => new Color(1f, 0.9f, 0.4f, 1f);

    /// <summary>패배 문구에 쓰는 위험색.</summary>
    public static Color Danger => new Color(1f, 0.27f, 0.27f, 1f);

    /// <summary>버튼 바탕 Image 색.</summary>
    public static Color ButtonBackground => new Color(0.25f, 0.35f, 0.6f, 1f);

    /// <summary>버튼 highlighted 상태색.</summary>
    public static Color ButtonHighlight => new Color(0.35f, 0.47f, 0.78f, 1f);

    /// <summary>버튼 pressed 상태색.</summary>
    public static Color ButtonPressed => new Color(0.18f, 0.25f, 0.45f, 1f);

    /// <summary>선택 강조색(Accent) 바탕 위에 쓰는 글자색.</summary>
    public static Color TextOnAccent => new Color(0.1f, 0.1f, 0.1f, 1f);

    /// <summary>HUD 패널 바탕색.</summary>
    public static Color PanelBackground => new Color(0.1f, 0.1f, 0.1f, 0.392f);

    /// <summary>모달 오버레이 바탕색.</summary>
    public static Color ModalBackground => new Color(0f, 0f, 0f, 0.72f);

    /// <summary>
    /// 모든 Selectable이 공유할 공용 ColorBlock을 만들어 반환한다.
    /// Image 색은 흰색으로 두고 상태색만으로 버튼 색을 결정한다:
    /// normal=ButtonBackground, highlighted=ButtonHighlight, pressed=ButtonPressed,
    /// selected=Accent, disabled=회색 반투명.
    /// </summary>
    public static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = ButtonBackground;
        colors.highlightedColor = ButtonHighlight;
        colors.pressedColor = ButtonPressed;
        colors.selectedColor = Accent;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        return colors;
    }
}
