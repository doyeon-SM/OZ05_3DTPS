/// <summary>
/// 상호작용 가능한 오브젝트가 구현해야 하는 인터페이스
/// Layer가 Interactable인 오브젝트에 부착된 컴포넌트에 사용
/// </summary>
public interface IInteraction
{
    /// <summary>
    /// 플레이어가 [E]키를 눌러 상호작용할 때 호출됩니다.
    /// </summary>
    void Interaction();

    /// <summary>
    /// 상호작용 UI에 표시될 설명 텍스트 ([E] 열기 등)
    /// 구현 클래스의 Inspector에서 관리합니다.
    /// </summary>
    string InteractionLabel { get; }
}
