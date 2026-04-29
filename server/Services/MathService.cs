public class MathService : IOperationService
{
    public Task<string> ExecuteAsync(string payload)
    {
        try
        {
            // Formato esperado: "operacao,num1,num2"
            // Exemplos: "soma,5,3" ou "div,10,2" ou "potencia,2,3"
            var parts = payload.Split(',');

            if (parts.Length < 3)
                return Task.FromResult("Erro: Formato inválido. Use: operacao,num1,num2");

            var operacao = parts[0].ToLower().Trim();

            if (!double.TryParse(parts[1], out double a))
                return Task.FromResult("Erro: Primeiro número inválido");

            if (!double.TryParse(parts[2], out double b))
                return Task.FromResult("Erro: Segundo número inválido");

            double resultado;
            string operacaoNome;

            switch (operacao)
            {
                case "soma":
                case "+":
                    resultado = a + b;
                    operacaoNome = "Soma";
                    break;

                case "sub":
                case "subtracao":
                case "-":
                    resultado = a - b;
                    operacaoNome = "Subtração";
                    break;

                case "mult":
                case "multiplicacao":
                case "*":
                    resultado = a * b;
                    operacaoNome = "Multiplicação";
                    break;

                case "div":
                case "divisao":
                case "/":
                    if (b == 0)
                        return Task.FromResult("Erro: Divisão por zero não permitida");
                    resultado = a / b;
                    operacaoNome = "Divisão";
                    break;

                case "pot":
                case "potencia":
                case "^":
                    resultado = Math.Pow(a, b);
                    operacaoNome = "Potência";
                    break;

                case "mod":
                case "resto":
                case "%":
                    if (b == 0)
                        return Task.FromResult("Erro: Módulo por zero não permitido");
                    resultado = a % b;
                    operacaoNome = "Módulo";
                    break;

                case "raiz":
                    // Neste caso, 'a' é o número e 'b' é o índice da raiz
                    if (b == 0)
                        return Task.FromResult("Erro: Índice da raiz não pode ser zero");
                    resultado = Math.Pow(a, 1.0 / b);
                    operacaoNome = "Raiz";
                    break;

                default:
                    return Task.FromResult($"Erro: Operação '{operacao}' não reconhecida. " +
                        "Use: soma, sub, mult, div, pot, mod, raiz");
            }

            return Task.FromResult($"{operacaoNome}: {a} e {b} = {resultado:F2}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Erro ao processar cálculo: {ex.Message}");
        }
    }
}
