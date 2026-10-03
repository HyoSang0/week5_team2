using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 전역에서 공용으로 쓰는 색상 값을 모아 둔 정적 팔레트.
/// 읽기 전용 프로퍼티로 색상을 제공하고 버튼 공용 ColorBlock 생성기도 제공한다.
/// </summary>
public static class UIPalette
{
    /// <summary>어두운 바탕면 색.</summary>
    public static Color SurfaceDark => new Color(0.106f, 0.110f, 0.133f, 1f);

    /// <summary>버튼과 면에 쓰는 남색 면 색.</summary>
    public static Color SurfaceNavy => new Color(0.141f, 0.196f, 0.337f, 1f);

    /// <summary>선택 강조, 마우스 오버, 승리 문구, 골드 등급에 쓰는 강조색.</summary>
    public static Color Accent => new Color(1f, 0.765f, 0.365f, 1f);

    /// <summary>Accent를 0.8배 어둡게 한 pressed 상태색.</summary>
    public static Color AccentPressed => new Color(0.8f, 0.612f, 0.292f, 1f);

    /// <summary>게임 오버 문구와 HP 채움에 쓰는 위험색.</summary>
    public static Color Danger => new Color(0.945f, 0.208f, 0.184f, 1f);

    /// <summary>기본 글자색.</summary>
    public static Color TextPrimary => new Color(1f, 1f, 1f, 1f);

    /// <summary>Accent 강조 바탕 위에 쓰는 글자색.</summary>
    public static Color TextOnAccent => SurfaceDark;

    /// <summary>버튼 바탕 Image 색.</summary>
    public static Color ButtonBackground => SurfaceNavy;

    /// <summary>버튼 highlighted 상태색. 선택 강조와 같은 색.</summary>
    public static Color ButtonHighlight => Accent;

    /// <summary>버튼 pressed 상태색.</summary>
    public static Color ButtonPressed => AccentPressed;

    /// <summary>버튼 disabled 상태색.</summary>
    public static Color ButtonDisabled => new Color(0.141f, 0.196f, 0.337f, 0.4f);

    /// <summary>HUD 패널 바탕색.</summary>
    public static Color PanelBackground => new Color(0.106f, 0.110f, 0.133f, 0.6f);

    /// <summary>모달 오버레이 바탕색.</summary>
    public static Color ModalBackground => new Color(0.106f, 0.110f, 0.133f, 0.85f);

    /// <summary>
    /// 모든 Selectable이 공유할 공용 ColorBlock을 만들어 반환한다.
    /// Image 색은 흰색으로 두고 상태색만으로 버튼 색을 결정한다:
    /// normal=ButtonBackground, highlighted=ButtonHighlight, pressed=ButtonPressed,
    /// selected=Accent, disabled=ButtonDisabled.
    /// </summary>
    public static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = ButtonBackground;
        colors.highlightedColor = ButtonHighlight;
        colors.pressedColor = ButtonPressed;
        colors.selectedColor = Accent;
        colors.disabledColor = ButtonDisabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        return colors;
    }
}
