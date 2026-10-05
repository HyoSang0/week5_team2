/// <summary>
/// 게임 일시정지의 원인을 구분하는 값.
/// </summary>
public enum PauseReason
{
    /// <summary>증강 선택 UI가 열려 있는 일시정지.</summary>
    AugmentSelection,

    /// <summary>게임 클리어/패배로 진행을 멈춘 일시정지.</summary>
    GameOver,
    /// <summary>타이틀의 메뉴 또는 조작 안내를 확인하는 일시정지.</summary>
    TitleMenu,
}
