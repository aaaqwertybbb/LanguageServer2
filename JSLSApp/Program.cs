/*
 Do not forget to re-publish when applicable
 */

using JSLSApp.LspTypes;
using System.Buffers;
using System.Text;
using System.Text.Json;
using WatsonWebsocket;

namespace JSLSApp;

public class Program
{
    internal static WatsonWsServer server;
    internal static JavaScriptParser? _activeParser = null;
    /// <summary>Contains the bytes casted as ints from the stream</summary>

    // Goal is to use the same wording as LSP where possible for clarity of communication.
    const byte MessageKind_none = 48 /* 0 */;
    const byte MessageKind_string = 49 /* 1 */;
    const byte MessageKind_textDocument_didOpen = 2;
    const byte MessageKind_textDocument_definition = 3;
    const byte MessageKind_custom_editorGetNodesAdjacentToCursor = 4;
    const byte MessageKind_custom_saveFile = 5;

    const byte MessageKind_field_separator = 95 /* _ */;

    static byte current_message_kind = MessageKind_none;
    static int current_message_contentLength = 0;

    static int message_contentLength_int = 0;
    static int messageKindInt = 0;

    static bool seen_separator_messageKind_to_contentLength = false;
    static bool seen_separator_contentLength_to_aaa = false;

    static byte[]? rentedBuffer;
    static int readLength;

    //public static UTF8Encoding noBomMark_UTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    // UTF-8 without BOM (your default)
    public static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // UTF-8 with BOM
    public static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static void Main(string[] args)
    {
        // TODO: This didn't close correctly I had to kill the previous app with task manager (maybe an exception is bad state or etc... i.e.:TODO)

        // Start server on localhost port 8080
        server = new WatsonWsServer("127.0.0.1", 8080, false);

        server.ClientConnected += (sender, args) => Console.WriteLine($"JS Client connected: {args.Client.IpPort}");
        server.ClientDisconnected += (sender, args) => Console.WriteLine("JS Client disconnected");

        // Handle incoming transaction buffers (Byte Arrays)
        server.MessageReceived += (sender, args) =>
        {
            ReadOnlySpan<byte> transactionBuffer = args.Data;
            
            //Console.WriteLine($"Received {transactionBuffer.Length} bytes of transaction data.");

            Guid clientGuid = args.Client.Guid;

            var index = 0;
            var str = "";

            if (current_message_kind == MessageKind_none)
            {
                while (index < transactionBuffer.Length && transactionBuffer[index] != MessageKind_field_separator)
                {
                    var b = transactionBuffer[index++];
                    if (b >= '0' && b <= '9')
                    {
                        messageKindInt = (messageKindInt * 10) + (b - '0');
                    }
                    // TODO: bad input
                }

                if (index >= transactionBuffer.Length)
                {
                    throw new NotImplementedException();
                }

                if (transactionBuffer[index] == MessageKind_field_separator)
                {
                    index++;
                    seen_separator_messageKind_to_contentLength = true;
                    current_message_kind = (byte)messageKindInt;
                    if (current_message_kind == MessageKind_custom_saveFile)
                    {
                        goto noParameters;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            
            if (!seen_separator_contentLength_to_aaa)
            {
                while (index < transactionBuffer.Length && transactionBuffer[index] != MessageKind_field_separator)
                {
                    var b = transactionBuffer[index++];
                    if (b >= '0' && b <= '9')
                    {
                        message_contentLength_int = (message_contentLength_int * 10) + (b - '0');
                    }
                    else
                    {
                        throw new NotImplementedException(); // TODO: bad input
                    }
                }

                if (index >= transactionBuffer.Length)
                {
                    throw new NotImplementedException();
                }

                if (transactionBuffer[index] == MessageKind_field_separator)
                {
                    index++;
                    current_message_contentLength = message_contentLength_int;
                    rentedBuffer = ArrayPool<byte>.Shared.Rent(current_message_contentLength);
                    seen_separator_contentLength_to_aaa = true;
                }
            }

            if (rentedBuffer is not null)
            {
                for (var i = readLength; i < current_message_contentLength; i++)
                {
                    rentedBuffer[i] = transactionBuffer[index++];
                    readLength++;
                }
            }

            str = Encoding.UTF8.GetString(rentedBuffer, 0, readLength);
            //Console.WriteLine(str);

            noParameters:

            if (current_message_kind == MessageKind_textDocument_didOpen)
            {
                Lsp.DeserializeContent_DidOpen(str);
                var base64String = Lsp.DeserializeContent_CustomFullFileLexRequest(str);
                server.SendAsync(clientGuid, base64String);
            }
            else if (current_message_kind == MessageKind_textDocument_definition)
            {
                if (int.TryParse(str, out var positionIndex))
                {
                    var positionIndexOfDefinition = Lsp.DeserializeContent_Definition(positionIndex);
                    if (positionIndexOfDefinition != -1)
                    {
                        server.SendAsync(clientGuid, positionIndexOfDefinition.ToString());
                    }
                }
            }
            else if (current_message_kind == MessageKind_custom_editorGetNodesAdjacentToCursor)
            {
                if (int.TryParse(str, out var positionIndex))
                {
                    var customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult = Lsp.DeserializeContent_CustomEditorGetNodesAdjacentToCursorFromLspRequest(positionIndex);
                    server.SendAsync(clientGuid, JsonSerializer.Serialize(customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult));
                }
            }
            else if (current_message_kind == MessageKind_custom_saveFile)
            {
                //Lsp.DeserializeContent_CustomSaveFile();
            }

            current_message_kind = MessageKind_none;
            current_message_contentLength = 0;

            message_contentLength_int = 0;
            messageKindInt = 0;

            seen_separator_messageKind_to_contentLength = false;
            seen_separator_contentLength_to_aaa = false;

            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                rentedBuffer = null;
            }
            
            readLength = 0;
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
