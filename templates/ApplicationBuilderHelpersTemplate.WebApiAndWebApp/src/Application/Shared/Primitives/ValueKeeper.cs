namespace Application.Shared.Primitives;

public class ValueKeeper<T>
{
    private readonly AsyncManualResetEvent _waiterEvent = new(false);

    public T? Value { get; private set; }

    public void SetValue(T? value)
    {
        Value = value;

        if (value != null)
        {
            _waiterEvent.Set();
        }
        else
        {
            _waiterEvent.Reset();
        }
    }

    public async Task<T?> WaitForValue(CancellationToken cancellationToken)
    {
        try
        {
            await _waiterEvent.WaitAsync(cancellationToken);
            return Value;
        }
        catch
        {
            return default;
        }
    }

    public async Task<T> WaitForValueOrThrow(CancellationToken cancellationToken)
    {
        try
        {
            await _waiterEvent.WaitAsync(cancellationToken);
            if (Value == null)
            {
                throw new OperationCanceledException();
            }
            return Value;
        }
        catch
        {
            throw;
        }
    }
}
