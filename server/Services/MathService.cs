public class MathService : IOperationService
{
    public Task<string> ExecuteAsync(string payload)
    {
        var parts = payload.Split(',');
        int a = int.Parse(parts[0]);
        int b = int.Parse(parts[1]);

        return Task.FromResult($"Resultado: {a + b}");
    }
}