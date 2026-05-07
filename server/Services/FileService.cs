public class FileService : IOperationService
{
    public async Task<string> ExecuteAsync(string payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload))
                return "Erro: Conteúdo vazio não pode ser salvo";

            // Arquivo fixo na pasta do servidor (mapeada do host)
            var path = "./data/file.txt";

            // Adiciona timestamp para rastreabilidade
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var conteudoComTimestamp = $"[{timestamp}] {payload}\n";

            await File.AppendAllTextAsync(path, conteudoComTimestamp);

            var fileInfo = new FileInfo(path);
            return $"✓ Conteúdo salvo em 'file.txt'. Tamanho total: {fileInfo.Length} bytes";
        }
        catch (UnauthorizedAccessException)
        {
            return "Erro: Permissão negada para acessar o arquivo";
        }
        catch (IOException ex)
        {
            return $"Erro de I/O: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Erro ao salvar arquivo: {ex.Message}";
        }
    }
}
