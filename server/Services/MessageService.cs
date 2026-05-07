public class MessageService : IOperationService
{
    public Task<string> ExecuteAsync(string payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload))
                return Task.FromResult("Erro: Mensagem vazia não pode ser processada");

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var response = $"[{timestamp}] Servidor recebeu: \"{payload}\" (Tamanho: {payload.Length} caracteres)";

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Erro ao processar mensagem: {ex.Message}");
        }
    }
}
