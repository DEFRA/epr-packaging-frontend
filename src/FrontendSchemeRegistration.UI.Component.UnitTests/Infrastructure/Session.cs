namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

public class Session : ISession
{
    private readonly Dictionary<string, byte[]> _data = new();
    private readonly Guid _id = Guid.NewGuid();
        
    public Task LoadAsync(CancellationToken cancellationToken = new()) => Task.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = new()) => Task.CompletedTask;

    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _data.TryGetValue(key, out value);

    public void Set(string key, byte[] value)
    {
        _data[key] = value;
    }

    public void Remove(string key)
    {
        _data.Remove(key);
    }

    public void Clear()
    {
        _data.Clear();
    }

    public bool IsAvailable => true;
    public string Id => _id.ToString();
    public IEnumerable<string> Keys => _data.Keys;
}