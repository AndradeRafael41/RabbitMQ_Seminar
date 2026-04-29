public class MessageService : IOperationService
{
    public Task<string> ExecuteAsync(string payload)
    {
        return Task.FromResult($"Servidor respondeu: {payload}");
    }
}