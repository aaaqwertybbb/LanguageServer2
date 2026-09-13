/*
 Do not forget to re-publish when applicable
 */

using JSLSApp;
using JSLSApp.LspTypes;
using System.Text;
using System.Text.Json;

internal class Program
{
    internal const string myPath = "C:\\Users\\hunte\\Repos\\file.txt";

    internal static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    internal static List<StdoutChunkObject> stdoutChunkObjects = new();
    internal static int stdoutChunkFirstEntryMetadataSubstringIndexStart = 0;
    internal static int stdoutChunkFirstEntryMetadataContentLengthNumber = 0;
    private static StringBuilder _stringBuilder = new();

    private static void Main(string[] args)
    {
        ThrowIfAnyoneElseTriesToRunThisProgram();
        MyDebuggingFunction();

        File.WriteAllText(myPath, Environment.ProcessId.ToString() + '\n');

        using StreamReader reader = new StreamReader(Console.OpenStandardInput());
        
        while (true)
        {
            var text = reader.ReadLine();
            if (text is not null)
                MAIN_decodeMessage(text);
        }
    }

    private static object? MAIN_decodeMessage(string json)
    {
        try
        {
            if (stdoutChunkObjects.Count == 0)
            {
                return MAIN_decodeMessage_start(json);
            }
            else
            {
                return MAIN_decodeMessage_continue(json);
            }
        }
        catch (Exception e)
        {
            //Console.WriteLine(e);
            File.AppendAllText(myPath, e.ToString() + '\n');
            return null;
        }
    }

    private static object? MAIN_decodeMessage_start(string json)
    {
        // Parse Content-Length
        var indexOfContentLengthToken = json.IndexOf("Content-Length: ");
        if (indexOfContentLengthToken == -1) return null;
        var substringIndexStart = indexOfContentLengthToken + 16; /* 16 === 'Content-Length: '.length */
        var substringIndexEnd = substringIndexStart;
        for (; substringIndexEnd < json.Length; substringIndexEnd++)
        {
            switch (json[substringIndexEnd])
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    break;
                default:
                    goto afterOuterForLoop;
            }
        }
        afterOuterForLoop:
        if (substringIndexEnd == substringIndexStart) return null;
        var contentLengthString = json.Substring(substringIndexStart, substringIndexEnd - substringIndexStart);
        if (!int.TryParse(contentLengthString, out var contentLengthNumber))
        {
            File.AppendAllText(myPath, $"\n====if (!int.TryParse(contentLengthString, out var contentLengthNumber))====\n");
            return null;
        }

        // Parse Content
        var indexOfSearchTerm = json.IndexOf("\r\n\r\n");
        if (indexOfSearchTerm == -1)
        {
            // TODO: This is a little scuffed because readline is losing the line endings that delimiter header from content...
            // ...
            //File.AppendAllText(myPath, $"\n====indexOfSearchTerm == -1-delaying====\n");
            // delaying
            stdoutChunkObjects.Add(new StdoutChunkObject(json));
            stdoutChunkFirstEntryMetadataSubstringIndexStart = json.Length;
            stdoutChunkFirstEntryMetadataContentLengthNumber = contentLengthNumber;
            return null;
        }
        substringIndexStart = indexOfSearchTerm + 4; /* 4 === "\r\n\r\n".length */

        // Payload
        if (substringIndexStart + contentLengthNumber <= json.Length)
        {
            var content = json.Substring(substringIndexStart, substringIndexStart + contentLengthNumber - substringIndexStart);
            return DeserializeContent(content);
        }
        else
        {
            // delaying
            stdoutChunkObjects.Add(new StdoutChunkObject(json));
            stdoutChunkFirstEntryMetadataSubstringIndexStart = substringIndexStart;
            stdoutChunkFirstEntryMetadataContentLengthNumber = contentLengthNumber;
            return null;
        }
    }

    /// <summary>
    /// // TODO: You could determine the necessary length of the NEXT chunk that will cause the necessary length requirement to be met then avoid an 'n complexity' and just have 'constant'.
    /// // TODO: Further commenting about determining the necessary length of the NEXT chunk, that is what the original 'if' block is doing on the first message. Perhaps these two conditional branches are equivalent when following a "necessary length" implementation.
    /// </summary>
    private static object? MAIN_decodeMessage_continue(string json)
    {
        // Parse Content
        // 0th
        var sumUnreadStdout = stdoutChunkObjects[0].BytesDecoded.Length - stdoutChunkFirstEntryMetadataSubstringIndexStart; // initialize to the remaining length that was in the first message of the batch

        // >first && <last
        for (var i = 1; i < stdoutChunkObjects.Count; i++)
        {
            sumUnreadStdout += stdoutChunkObjects[i].BytesDecoded.Length;
        }

        // current
        sumUnreadStdout += json.Length;

        // Payload
        if (stdoutChunkFirstEntryMetadataContentLengthNumber <= sumUnreadStdout)
        {
            // TODO: Preferably only clear after you're done to empty out the string builder...
            // ...if all that 'Clear' does is change internal state that doesn't result in any collectable objects then it doesn't matter at all what I'm saying...
            // ...but I'm just presuming that there is internal state which becomes collectable when I invoke 'Clear'...
            // ...and thus I want to clear at the end so that I immediately have that internal state collectable...
            // ...but if I have an exception thrown then the '_stringBuilder' never will be cleared...
            // ...all in all invoking this extra 'Clear' versus the possibility of consistently hitting this conditional branch
            // ... and eating either a single large allocation from the sum of unread...
            // ...or eating a new without initialCapacity and eating the resizing cost...
            // ...this 'clear' is nothing compared to those things is what I'm thinking.
            // 
            _stringBuilder.Clear();

            var lenZeroth = stdoutChunkObjects[0].BytesDecoded.Length - stdoutChunkFirstEntryMetadataSubstringIndexStart;
            if (lenZeroth != 0)
            {
                var zerothSubstring = stdoutChunkObjects[0].BytesDecoded.Substring(stdoutChunkFirstEntryMetadataSubstringIndexStart, stdoutChunkObjects[0].BytesDecoded.Length);
                _stringBuilder.Append(zerothSubstring);
            }

            // >first && <last
            for (var i = 1; i < stdoutChunkObjects.Count; i++)
            {
                _stringBuilder.Append(stdoutChunkObjects[i].BytesDecoded);
            }

            // current
            _stringBuilder.Append(json);

            var joinedJson = _stringBuilder.ToString();
            _stringBuilder.Clear();

            stdoutChunkObjects.Clear(); // TODO: clear the array entries to permit garbage collection (since stdoutChunkObjects is always in the app's scope any entries would as well never be collected)

            string content;

            if (joinedJson.Length == stdoutChunkFirstEntryMetadataContentLengthNumber)
            {
                content = joinedJson;
            }
            else
            {
                content = joinedJson.Substring(0, stdoutChunkFirstEntryMetadataContentLengthNumber - 0);
                // I can't decide on what to put here, at the end of the day just make sure this case has something instrusive so its incompleteness isn't swept under the rug
                // maybe I should throw an error I can't describe how "confused" I am at the moment I am just pushing to make progress with every last bit of energy I have
                // and all the anxiety and decisions i.e.: you get a message box idk
                throw new NotImplementedException();
            }

            return DeserializeContent(content);

        }
        else
        {
            // ... continue delaying
            stdoutChunkObjects.Add(new StdoutChunkObject(json));
            return null;
        }
    }

    private static object? DeserializeContent(string content)
    {
        var request = JsonSerializer.Deserialize<Message>(content, _jsonSerializerOptions);
        if (request is null)
        {
            File.AppendAllText(myPath, $"\n====request is null====\n");
        }
        return LspDispatcher.GiveMessage(content, request);
    }

    public static string MAIN_encodeMessageObject(object messageObject)
    {
        var content = JsonSerializer.Serialize(messageObject);
        var spacing = "\r\n\r\n";
        return $"Content-Length: {content.Length}{spacing}{content}";
    }

    /// <summary>
    /// The code is too egregiously bad at the moment I wanna make certain that nobody runs this.
    /// </summary>
    private static void ThrowIfAnyoneElseTriesToRunThisProgram()
    {
        string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (homePath != "C:\\Users\\hunte")
        {
            Console.WriteLine(@"if (homePath != ""C:\\Users\\hunte"")");
            throw new Exception();
        }
    }

    private static void MyDebuggingFunction()
    {
//        var str = @"
//public class Foo {
//    Bar() {
//    }
//}
//";

        var uriPath = "file:///C:/Users/hunte/Repos/New folder (6)/Editor/src/RendererFiles/dialogGlobal.js";
        var localPath = new Uri(uriPath).LocalPath;
        var str = File.ReadAllText(localPath);

        var javaScriptDocument = new JavaScriptDocument(localPath, str.ToList());
        var javaScriptParser = new JavaScriptParser(javaScriptDocument, JavaScriptWorkspace.Empty);
        javaScriptDocument.CompilationUnit = javaScriptParser.Parse();
        var aaa = javaScriptDocument.CompilationUnit.FunctionDefinitionStartPositionList;
        var bbb = javaScriptDocument.CompilationUnit.GetString();
    }
}
