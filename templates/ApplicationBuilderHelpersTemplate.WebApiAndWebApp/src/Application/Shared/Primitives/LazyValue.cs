namespace Application.Shared.Primitives;

public class LazyValue<TValue>(Func<CancellationToken, Task<TValue>> valueFactory)
{
    private TValue? _value;
    private readonly Func<CancellationToken, Task<TValue>> _valueFactory = valueFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<TValue> Get(CancellationToken cancellationToken)
    {
        if (_value == null)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _value ??= await _valueFactory(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        return _value;
    }
}
