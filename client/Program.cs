class Program
{
    static void Main()
    {
        try
        {
            using var rpc = new RpcClient();

            bool running = true;

            while (running)
            {
                Console.WriteLine("\n╔══════════════════════════════════════╗");
                Console.WriteLine("║      CLIENTE RPC - RabbitMQ         ║");
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.WriteLine("\n[OPERAÇÕES RPC - Com Resposta]");
                Console.WriteLine("  1 - Enviar mensagem de texto");
                Console.WriteLine("  2 - Escrever em arquivo no servidor");
                Console.WriteLine("  3 - Operações matemáticas");
                Console.WriteLine("\n[OPERAÇÃO ASSÍNCRONA - Sem Resposta]");
                Console.WriteLine("  4 - Enviar mensagem async (Fire-and-forget)");
                Console.WriteLine("\n[SISTEMA]");
                Console.WriteLine("  0 - Sair");
                Console.WriteLine("─────────────────────────────────────");

                Console.Write("\nOpção: ");
                var op = Console.ReadLine();

                try
                {
                    switch (op)
                    {
                        case "1":
                            Console.Write("\n→ Digite a mensagem: ");
                            var msg = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(msg))
                            {
                                Console.WriteLine("\n[RESPOSTA]");
                                Console.WriteLine(rpc.Call("msg", msg));
                            }
                            else
                            {
                                Console.WriteLine("[ERRO] Mensagem não pode ser vazia");
                            }
                            break;

                        case "2":
                            Console.Write("\n→ Digite o texto para salvar: ");
                            var texto = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(texto))
                            {
                                Console.WriteLine("\n[RESPOSTA]");
                                Console.WriteLine(rpc.Call("file", texto));
                            }
                            else
                            {
                                Console.WriteLine("[ERRO] Texto não pode ser vazio");
                            }
                            break;

                        case "3":
                            MostrarMenuCalculadora(rpc);
                            break;

                        case "4":
                            Console.Write("\n→ Mensagem para envio assíncrono: ");
                            var msgAsync = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(msgAsync))
                            {
                                rpc.SendAsync("msg", msgAsync);
                            }
                            else
                            {
                                Console.WriteLine("[ERRO] Mensagem não pode ser vazia");
                            }
                            break;

                        case "0":
                            running = false;
                            Console.WriteLine("\n[INFO] Encerrando cliente...");
                            break;

                        default:
                            Console.WriteLine("\n[ERRO] Opção inválida!");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ERRO] {ex.Message}");
                }

                if (running && op != "3")
                {
                    Console.WriteLine("\nPressione ENTER para continuar...");
                    Console.ReadLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] Não foi possível iniciar o cliente: {ex.Message}");
        }
    }

    static void MostrarMenuCalculadora(RpcClient rpc)
    {
        Console.WriteLine("\n╔═══════════════════════════════════╗");
        Console.WriteLine("║     OPERAÇÕES MATEMÁTICAS        ║");
        Console.WriteLine("╚═══════════════════════════════════╝");
        Console.WriteLine("  1 - Soma");
        Console.WriteLine("  2 - Subtração");
        Console.WriteLine("  3 - Multiplicação");
        Console.WriteLine("  4 - Divisão");
        Console.WriteLine("  5 - Potência");
        Console.WriteLine("  6 - Módulo (resto)");
        Console.WriteLine("  7 - Raiz n-ésima");
        Console.WriteLine("  0 - Voltar");
        Console.WriteLine("───────────────────────────────────");

        Console.Write("\nOperação: ");
        var opcao = Console.ReadLine();

        if (opcao == "0") return;

        Console.Write("Primeiro número: ");
        var num1 = Console.ReadLine();

        Console.Write("Segundo número: ");
        var num2 = Console.ReadLine();

        string operacao = opcao switch
        {
            "1" => "soma",
            "2" => "sub",
            "3" => "mult",
            "4" => "div",
            "5" => "pot",
            "6" => "mod",
            "7" => "raiz",
            _ => ""
        };

        if (!string.IsNullOrEmpty(operacao))
        {
            var payload = $"{operacao},{num1},{num2}";
            Console.WriteLine("\n[RESPOSTA]");
            Console.WriteLine(rpc.Call("calc", payload));
        }
        else
        {
            Console.WriteLine("[ERRO] Operação inválida!");
        }

        Console.WriteLine("\nPressione ENTER para continuar...");
        Console.ReadLine();
    }
}
