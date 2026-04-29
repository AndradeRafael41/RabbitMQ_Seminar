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
                Console.WriteLine("\n=== CLIENTE RPC ===");
                Console.WriteLine("1 - Enviar mensagem (RPC)");
                Console.WriteLine("2 - Escrever em arquivo (RPC)");
                Console.WriteLine("3 - Somar números (RPC)");
                Console.WriteLine("4 - Enviar mensagem (ASYNC)");
                Console.WriteLine("0 - Sair");

                Console.Write("Opção: ");
                var op = Console.ReadLine();

                try
                {
                    switch (op)
                    {
                        case "1":
                            Console.Write("Mensagem: ");
                            Console.WriteLine(rpc.Call("msg", Console.ReadLine()));
                            break;

                        case "2":
                            Console.Write("Texto: ");
                            Console.WriteLine(rpc.Call("file", Console.ReadLine()));
                            break;

                        case "3":
                            Console.Write("Ex: 2,3: ");
                            Console.WriteLine(rpc.Call("calc", Console.ReadLine()));
                            break;

                        /*case "4":
                            Console.Write("Mensagem async: ");
                            rpc.SendAsync("msg", Console.ReadLine());
                            break;
                        */
                        case "0":
                            running = false;
                            break;

                        default:
                            Console.WriteLine("Opção inválida!");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERRO] " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FATAL] Não foi possível iniciar o cliente: " + ex.Message);
        }
    }
}