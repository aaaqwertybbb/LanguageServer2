/*
 Do not forget to re-publish when applicable
 */

using JSLSApp.LspTypes;
using System.Text.Json;
using WatsonWebsocket;

namespace JSLSApp;

public class Program
{
    internal static WatsonWsServer server;
    internal static JavaScriptParser? _activeParser = null;
    /// <summary>Contains the bytes casted as ints from the stream</summary>

    private static void Main(string[] args)
    {
        // Start server on localhost port 8080
        server = new WatsonWsServer("127.0.0.1", 8080, false);

        server.ClientConnected += (sender, args) => Console.WriteLine($"JS Client connected: {args.Client.IpPort}");
        server.ClientDisconnected += (sender, args) => Console.WriteLine("JS Client disconnected");

        // Handle incoming transaction buffers (Byte Arrays)
        server.MessageReceived += (sender, args) =>
        {
            ReadOnlySpan<byte> transactionBuffer = args.Data;
            // args.Data[0] => 104 => 'h'
            // args.Data[1] => 105 => 'i'

            // Process your text editor sync logic here
            Console.WriteLine($"Received {transactionBuffer.Length} bytes of transaction data.");

            // Look up the exact GUID assigned to this JS client
            Guid clientGuid = args.Client.Guid;

            // Echo or broadcast back if necessary
            var test = "hello"u8;
            //byte[] replyBytes = new byte[5];
            //replyBytes[0] = 'h';
            //replyBytes[1] = 'e';
            //replyBytes[2] = 'l';
            //replyBytes[3] = 'l';
            //replyBytes[4] = 'o';

            server.SendAsync(clientGuid, test.ToArray());
        };

        server.Start();
        Console.WriteLine("WebSocket Server started on ws://127.0.0.1:8080");
        Console.ReadLine(); // Keep console alive
    }

    public static object? MAIN_decodeMessage(ReadOnlyMemory<byte> RENTED_NO_ASYNC_jsonBytes)
    {
        return null;
    }

    public static string MAIN_encodeMessageObject(object messageObject)
    {
        var content = JsonSerializer.Serialize(messageObject);
        var spacing = "\r\n\r\n";
        return $"Content-Length: {content.Length}{spacing}{content}";
    }
}
