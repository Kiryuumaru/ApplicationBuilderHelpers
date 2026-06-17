namespace Application.Shared.Primitives;

public class GateKeeper
{
    private readonly AsyncManualResetEvent _waiterEvent = new(false);

    public bool IsOpen { get; private set; }

    public GateKeeper(bool initialOpenState = false)
    {
        SetOpen(initialOpenState);
    }

    public void SetOpen(bool isOpen = true)
    {
        IsOpen = isOpen;

        if (isOpen)
        {
            _waiterEvent.Set();
        }
        else
        {
            _waiterEvent.Reset();
        }
    }

    public ValueTask<bool> WaitForOpen(CancellationToken cancellationToken)
        => _waiterEvent.WaitAsync(cancellationToken);
}
