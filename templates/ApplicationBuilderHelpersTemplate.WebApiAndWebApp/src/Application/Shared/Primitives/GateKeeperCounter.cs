namespace Application.Shared.Primitives;

public class GateKeeperCounter : GateKeeper
{
    private readonly Lock _lock = new();

    public int Count { get; private set; }

    public void Increment()
    {
        using var _ = _lock.EnterScope();
        Count++;
        if (Count != 0)
        {
            SetOpen(false);
        }
    }

    public void Decrement()
    {
        using var _ = _lock.EnterScope();
        Count--;
        if (Count == 0)
        {
            SetOpen();
        }
    }
}
