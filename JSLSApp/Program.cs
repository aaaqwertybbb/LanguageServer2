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
                byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(contentLength);

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

    /// <summary>
    /// The rewrite is intentionally written so that you re-capture each section of the header if it appears a second time right?
    /// It looks very nice, and I think the redundancy is intentional and very good I just wanna make sure,
    /// 
    /// Yes, exactly!...
    /// ...
    /// </summary>
    private static int ReadHeadersAndGetContentLength(Stream stream)
    {
        int contentLength = -1;

        // We track the state of consecutive bytes matching "\r\n\r\n" to find the end of headers
        // 0 = none, 1 = \r, 2 = \r\n, 3 = \r\n\r, 4 = \r\n\r\n
        int lspDelimiterState = 0;

        // To match "Content-Length: ", we can keep an index
        ReadOnlySpan<byte> targetHeader = "Content-Length: "u8; // C# 11 Utf8 literal
        int headerMatchIndex = 0;
        bool parsingDigits = false;

        while (true)
        {
            int b = stream.ReadByte();
            if (b == -1) return -1; // End of stream

            // --- 1. HANDLE CONTENT-LENGTH MATCHING & PARSING ---
            if (!parsingDigits)
            {
                if (b == targetHeader[headerMatchIndex])
                {
                    headerMatchIndex++;
                    if (headerMatchIndex == targetHeader.Length)
                    {
                        parsingDigits = true;
                        contentLength = 0; // Initialize integer compilation
                    }
                }
                else
                {
                    // Fallback reset: Check if the current mismatched byte happens to be 'C' 
                    // to start a new partial match, otherwise reset to 0.
                    headerMatchIndex = (b == targetHeader[0]) ? 1 : 0;
                }
            }
            else // We are currently consuming the digits of the Content-Length
            {
                if (b >= '0' && b <= '9')
                {
                    contentLength = (contentLength * 10) + (b - '0');
                }
                else
                {
                    // Hit a non-digit character (likely '\r' or ' '). Stop digit parsing.
                    parsingDigits = false;
                    headerMatchIndex = (b == targetHeader[0]) ? 1 : 0;
                }
            }

            // --- 2. HANDLE END OF HEADERS DETECTION (\r\n\r\n) ---
            if (b == 13 /* \r */)
            {
                if (lspDelimiterState == 0) lspDelimiterState = 1;
                else if (lspDelimiterState == 2) lspDelimiterState = 3;
                else lspDelimiterState = 1; // Reset to first \r
            }
            else if (b == 10 /* \n */)
            {
                if (lspDelimiterState == 1) lspDelimiterState = 2;
                else if (lspDelimiterState == 3) // Found the second \n -> \r\n\r\n complete!
                {
                    // Ensure we actually found a valid Content-Length before returning
                    if (contentLength > -1)
                    {
                        return contentLength;
                    }

                    // If we hit the end of headers but never found Content-Length, reset and keep looking
                    lspDelimiterState = 0;
                }
                else
                {
                    lspDelimiterState = 0;
                }
            }
            else
            {
                lspDelimiterState = 0; // Any other character breaks the \r\n stream sequence
            }
        }
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
