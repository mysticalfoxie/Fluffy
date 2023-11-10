namespace Fluffy.Handlers;

public interface IHandler
{
    public int Order { get; }
    public void Register();
    public Task Initialize();
}