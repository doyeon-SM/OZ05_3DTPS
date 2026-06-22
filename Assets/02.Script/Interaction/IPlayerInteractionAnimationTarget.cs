/// <summary>
/// 상호작용 성공 시 플레이어의 Interact 애니메이션을 재생할 수 있는 대상입니다.
/// Door, SceneMove처럼 즉시 처리되는 상호작용은 이 인터페이스를 구현하지 않습니다.
/// </summary>
public interface IPlayerInteractionAnimationTarget
{
    bool CanPlayPlayerInteractionAnimation { get; }
}
