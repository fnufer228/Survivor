public interface IHandCarrier
{
    bool IsCarrying(BuildResource resource);
    bool TryTake(BuildResource resource);
    void Give(BuildResource resource);
}
