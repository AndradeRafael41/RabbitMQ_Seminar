public class FileService : IOperationService
{
    public async Task<string> ExecuteAsync(string payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload))
                return "Erro: Conteúdo vazio não pode ser salvo";

            // Caminho configurável via env var.
            // - Em Docker (docker-compose.yml com volume ./server:/data) use /data/file.txt
            // - Rodando nativo (dotnet run) cai no fallback ./data/file.txt
            var path = Environment.GetEnvironmentVariable("FILE_PATH")
                       ?? (Directory.Exists("/data") ? "/data/file.txt" : "./data/file.txt");

            // Garante que o diretório exista (cria se preciso)
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Adiciona timestamp para rastreabilidade
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var conteudoComTimestamp = $"[{timestamp}] {payload}\n";

            await File.AppendAllTextAsync(path, conteudoComTimestamp);

            var fileInfo = new FileInfo(path);
            return $"✓ Conteúdo salvo em '{path}'. Tamanho total: {fileInfo.Length} bytes";
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
