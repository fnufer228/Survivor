public interface IDamageable
{
    void TakeHit(int damage);
}

public interface IInteractable
{
    string Prompt { get; }
    bool CanInteract { get; }
    void Interact();
}
