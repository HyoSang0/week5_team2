/// <summary>
/// 증강 선택 카드 하나의 선택/리롤 입력을 받는 처리자 인터페이스.
/// AugmentCardView는 이 인터페이스만으로 입력을 전달하며 구현체의 종류를 알지 않는다.
/// </summary>
public interface IAugmentCardHandler
{
    /// <summary>
    /// index번 카드가 선택됐다는 입력을 처리한다.
    /// </summary>
    void HandleCardSelected(int index);

    /// <summary>
    /// index번 카드의 리롤이 요청됐다는 입력을 처리한다.
    /// </summary>
    void HandleCardRerolled(int index);
}
