/*
 Do not forget to re-publish when applicable
 */

using JSLSApp.LspTypes;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace JSLSApp;

public class Program
{
    internal static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    internal static JavaScriptParser? _activeParser = null;
    /// <summary>Contains the bytes casted as ints from the stream</summary>
    private static List<byte> _contentLengthNumberBuffer = new();

    private static void Main(string[] args)
    {
        try
        {
            using Stream stdin = Console.OpenStandardInput();

            while (true)
            {
                int contentLength = ReadHeadersAndGetContentLength(stdin);
                if (contentLength == -1) break; // Stream closed

                // NOTE: The pool may return an array LARGER than contentLength.
                byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(contentLength);

                try
                {
                    int bytesRead = 0;

                    while (bytesRead < contentLength)
                    {
                        int read = stdin.Read(rentedBuffer, bytesRead, contentLength - bytesRead);
                        if (read == 0) break; // End of stream
                        bytesRead += read;
                    }

                    MAIN_decodeMessage(new ReadOnlyMemory<byte>(rentedBuffer, 0, contentLength));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
                
            }
        }
        catch (Exception e)
        {
            var windowShowMessageNotification = new WindowShowMessageNotification(new WindowShowMessageNotificationParams(2, e.ToString()));
            if (_activeParser is not null)
            {
                var localActiveParser = _activeParser;
                _activeParser = null;
                windowShowMessageNotification.@params.message = localActiveParser.DebugString + windowShowMessageNotification.@params.message;
            }
            Console.Out.WriteLine(MAIN_encodeMessageObject(windowShowMessageNotification));
            return;
        }
    }

    private static int ReadHeadersAndGetContentLength(Stream stream)
    {
        _contentLengthNumberBuffer.Clear();

        var contentLength = -1;
        var matchIndex = 0;

        while (true)
        {
            int b = stream.ReadByte();
            if (b == -1) return -1; // End of stream

            refreshCauseParsedNumber:

            switch (matchIndex)
            {
                case 0:
                    if (b == 67 /*C*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 1:
                    if (b == 111 /*o*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 2:
                    if (b == 110 /*n*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 3:
                    if (b == 116 /*t*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 4:
                    if (b == 101 /*e*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 5:
                    if (b == 110 /*n*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 6:
                    if (b == 116 /*t*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 7:
                    if (b == 45 /*-*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 8:
                    if (b == 76 /*L*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 9:
                    if (b == 101 /*e*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 10:
                    if (b == 110 /*n*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 11:
                    if (b == 103 /*g*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 12:
                    if (b == 116 /*t*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 13:
                    if (b == 104 /*h*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 14:
                    if (b == 58 /*:*/)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 15:
                    if (b == 32 /* */)
                        matchIndex++;
                    else
                        matchIndex = 0;
                    break;
                case 16:
                    // Read "Content-Length: ", now read the number.
                    if (char.IsAsciiDigit((char)b))
                    {
                        _contentLengthNumberBuffer.Add((byte)b);
                    }
                    else
                    {
                        matchIndex++;
                        ReadOnlySpan<byte> aaa = CollectionsMarshal.AsSpan(_contentLengthNumberBuffer); // TODO: I don't like the idea of doing this
                        if (!int.TryParse(aaa, out contentLength))
                        {
                            throw new Exception(contentLength.ToString());
                        }
                        goto refreshCauseParsedNumber;
                    }
                    break;
                case 17: // index 0 "\r\n\r\n"
                    if (b == 13 /*\r*/)
                        matchIndex++;
                    else
                        matchIndex = 17;
                    break;
                case 18: // index 1 "\r\n\r\n"
                    if (b == 10 /*\n*/)
                        matchIndex++;
                    else
                        matchIndex = 17;
                    break;
                case 19: // index 2 "\r\n\r\n"
                    if (b == 13 /*\r*/)
                        matchIndex++;
                    else
                        matchIndex = 17;
                    break;
                case 20: // index 3 "\r\n\r\n"
                    if (b == 10 /*\n*/)
                    {
                        matchIndex++;
                        goto exitWhileLoop;
                    }
                    else
                        matchIndex = 17;
                    break;
                case 21: // TODO: This case does nothing because case '20' already has 'goto exitWhileLoop;'
                    goto exitWhileLoop;
            }
        }

        exitWhileLoop:
        return contentLength;
    }

    public static object? MAIN_decodeMessage(ReadOnlyMemory<byte> RENTED_NO_ASYNC_jsonBytes)
    {
        // TODO: Option 2: Keep ReadOnlySpan<byte> and use Utf8JsonReader

        // 1. Parse the byte span ONCE into a lightweight DOM
        using (JsonDocument doc = JsonDocument.Parse(RENTED_NO_ASYNC_jsonBytes))
        {
            JsonElement root = doc.RootElement;

            // 2. Extract the method string to route the message
            if (!root.TryGetProperty("method", out JsonElement methodElement))
            {
                return null; // Not a standard LSP request/notification
            }

            string? method = methodElement.GetString();

            switch (method)
            {
                case "initialize":
                    return Lsp.DeserializeContent_Initialize(JsonSerializer.Deserialize<InitializeRequest>(root, _jsonSerializerOptions));
                case "textDocument/didOpen":
                    return Lsp.DeserializeContent_DidOpen(JsonSerializer.Deserialize<DidOpenTextDocumentNotification>(root, _jsonSerializerOptions));
                case "textDocument/didClose":
                    return Lsp.DeserializeContent_DidClose(JsonSerializer.Deserialize<DidCloseTextDocumentNotification>(root, _jsonSerializerOptions));
                case "textDocument/didChange":
                    return Lsp.DeserializeContent_DidChange(JsonSerializer.Deserialize<DidChangeTextDocumentNotification>(root, _jsonSerializerOptions));
                case "textDocument/documentSymbol":
                    return Lsp.DeserializeContent_DocumentSymbol(JsonSerializer.Deserialize<TextDocumentDocumentSymbolRequest>(root, _jsonSerializerOptions));
                case "textDocument/hover":
                    return Lsp.DeserializeContent_Hover(JsonSerializer.Deserialize<TextDocumentHoverRequest>(root, _jsonSerializerOptions));
                case "textDocument/definition":
                    return Lsp.DeserializeContent_Definition(JsonSerializer.Deserialize<TextDocumentDefinitionRequest>(root, _jsonSerializerOptions));
                case "textDocument/completion":
                    return Lsp.DeserializeContent_Completion(JsonSerializer.Deserialize<TextDocumentCompletionRequest>(root, _jsonSerializerOptions));
                case "textDocument/completion_slice":
                    return Lsp.DeserializeContent_CompletionSlice(JsonSerializer.Deserialize<TextDocumentCompletionRequest_slice>(root, _jsonSerializerOptions));
                case "textDocument/CustomFullFileLexRequest":
                    return Lsp.DeserializeContent_CustomFullFileLexRequest(JsonSerializer.Deserialize<CustomFullFileLexRequest>(root, _jsonSerializerOptions));
                case "textDocument/CustomEditorGetNodesAdjacentToCursorFromLspRequest":
                    return Lsp.DeserializeContent_CustomEditorGetNodesAdjacentToCursorFromLspRequest(JsonSerializer.Deserialize<CustomEditorGetNodesAdjacentToCursorFromLspRequest>(root, _jsonSerializerOptions));
                case "textDocument/CustomEditorVerifyLspTextAgainstEditorTextRequest":
                    return Lsp.DeserializeContent_CustomEditorVerifyLspTextAgainstEditorTextRequest(JsonSerializer.Deserialize<CustomEditorVerifyLspTextAgainstEditorTextRequest>(root, _jsonSerializerOptions));
                default:
                    return null;
            }
        }
    }

    public static string MAIN_encodeMessageObject(object messageObject)
    {
        var content = JsonSerializer.Serialize(messageObject);
        var spacing = "\r\n\r\n";
        return $"Content-Length: {content.Length}{spacing}{content}";
    }
}
