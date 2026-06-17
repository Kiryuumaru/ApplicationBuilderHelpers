namespace Domain.Authorization.Models;

/// <summary>
/// Represents a permission check request with path and optional parameters.
/// </summary>
public readonly struct PermissionRequest
{
    private readonly string _path;
    private readonly string? _params;

    internal PermissionRequest(string path)
    {
        _path = path;
        _params = null;
    }

    internal PermissionRequest(string path, string key, string value)
    {
        _path = path;
        _params = $"{key}={value}";
    }

    private PermissionRequest(string path, string existingParams, string key, string value)
    {
        _path = path;
        _params = $"{existingParams};{key}={value}";
    }

    public string Path => _path;

    public PermissionRequest WithParameter(string key, string value) =>
        string.IsNullOrEmpty(_params)
            ? new PermissionRequest(_path, key, value)
            : new PermissionRequest(_path, _params, key, value);

    public override string ToString() =>
        string.IsNullOrEmpty(_params) ? _path : $"{_path};{_params}";

    public static implicit operator string(PermissionRequest request) => request.ToString();
}
