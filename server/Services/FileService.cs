public class FileService : IOperationService
{
    public async Task<string> ExecuteAsync(string payload)
    {
        var path = Environment.GetEnvironmentVariable("FILE_PATH") ?? "/app/dados.txt";

        await File.AppendAllTextAsync(path, payload + "\n");

        return "Conteúdo salvo no arquivo";
    }
}