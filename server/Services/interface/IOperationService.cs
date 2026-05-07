public interface IOperationService
{
    Task<string> ExecuteAsync(string payload);
}