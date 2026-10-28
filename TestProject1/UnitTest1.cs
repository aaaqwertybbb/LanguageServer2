namespace TestProject1;

public class LexerTests
{
    /// <summary>
    /// Everything in this file is extremely inaccurate I messed with the numbers and the text and the json length so it all kinda works for at least a single case.
    /// It'll hit the Parse function if it doesn't throw an exception.
    /// 
    /// got it `{"An item with the same key has already been added. Key: createElement"}`
    /// </summary>
    [Fact]
    public void ParseFile()
    {
        var program = new JSLSApp.Program();
        JSLSApp.Program.MAIN_decodeMessage(InitializeJson);
        var aaa = Format_didOpenTextDocumentNotification(FileText);
        JSLSApp.Program.MAIN_decodeMessage(aaa);
        JSLSApp.Program.MAIN_decodeMessage(customFullFileLexRequest);
        JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestOne); // ensure the goto definition of the original 'x'
        JSLSApp.Program.MAIN_decodeMessage(DidChangeNotification); // paste "function Aaa() { return 'x'; }"
        JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestTwo); // ensure the original goto definition of the 'x' no longer works
        JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestThree); // try another goto definition but with the updated position for 'x'
        JSLSApp.Program.MAIN_decodeMessage(DefinitionRequestFour); // try a final goto definition but for the 'x' that is inside of the "function Aaa() { return 'x'; }" text.
    }

    // TODO: Try to resolve unknown symbols after parsing the file rather than doing what you did last time which was to parse the entire scope and then re-visit the child scopes one by one...
    // ...whichever is the correct answer I don't know but you tried one so you should try the other one now.

    public const string InitializeJson = "Content-Length: 331\r\n\r\n{\"method\":\"initialize\",\"id\":0,\"params\":{\"processId\":18260,\"clientInfo\":{\"name\":\"TextEditor123\",\"version\":\"0.0.1\"},\"rootUri\":null,\"capabilities\":{\"workspace\":{}},\"workspaceFolders\":[{\"uri\":\"C:\\\\Users\\\\hunte\\\\Repos\\\\New folder (7)\\\\Editor2\",\"name\":\"2\"},{\"uri\":\"C:\\\\Users\\\\hunte\\\\Repos\\\\New folder (7)\\\\LanguageServer2\",\"name\":\"3\"}]}}\\n";
    // I'm gonna put "      " at the end of this string cuz the length is wrong and it won't read it
    public const string didOpenTextDocumentNotification = "Content-Length: 433\r\n\r\n{\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"languageId\":\"javascript\",\"version\":0,\"text\":\"aaa\\n        /**\\n         * @type {HTMLDivElement}\\n         * Consider the existence of such methods as 'state_cursor_setIndex' before mutating state directly\\n         */xxx/*\\n         */\\n        this.cursorElement = document.createElement('div');\"}}}\\n      ";
    public const string customFullFileLexRequest = "Content-Length: 157\r\n\r\n{\"id\":1,\"method\":\"textDocument/CustomFullFileLexRequest\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"}}}\\n";

    public const string FileText = "let x = 2;\\n\\nreturn x;";
    

    public const string DefinitionRequestOne = "Content-Length: 179\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":2,\"character\":7}}}\n";
    public const string DefinitionRequestTwo = "Content-Length: 179\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":2,\"character\":7}}}\n";
    public const string DefinitionRequestThree = "Content-Length: 179\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":5,\"character\":7}}}\n";
    public const string DefinitionRequestFour = "Content-Length: 180\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":3,\"character\":11}}}\n";

    public const string DidChangeNotification = "Content-Length: 288\r\n\r\n{\"method\":\"textDocument/didChange\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"version\":1},\"contentChanges\":[{\"range\":{\"start\":{\"line\":1,\"character\":0},\"end\":{\"line\":1,\"character\":0}},\"text\":\"function Aaa() {\\\\n    return x;\\\\n}\"}]}}\\n";


    public const string CompletionRequest = "Content-Length: 179\r\n\r\n{\"id\":2,\"method\":\"textDocument/completion\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":0,\"character\":0}}}\\n";

/*
There are 4 steps:
- [x] Get the code to even run (now that you've changed all the properties tons of build errors, and make sure nothing is bugging out)
- [ ] Replicate the current goto definition logic with the changed properties (that are now relative positions).
    - [x] find the reference node
    - [ ] find the declaration node
        - [ ] Algorithm "first thing that comes to mind regardless of what it is"
            - [ ] Start at the definition node, and calculate the total "negative" distance required to reach the GlobalNode.
            - [ ] Absolute value of that is the position but I need the line and character too.
                - [ ] Line and character would be the absolute value of
                    - [ ] Upon finding a new line you remember the "negative character index" that you have stored.
                    - [ ] When you're done your line number is the absolute value of the "negative line index".
        - [ ] Algorithm "???"
- [ ] When you insert text you update the relative positions of the "node on the left and right of where you are inserting".
- [ ] When you insert text you parse the text to create a new node and modify the AST with the node inserted at the correct position.
*/

    public string Format_didOpenTextDocumentNotification(string fileText)
    {
        return "Content-Length: " + (181 + fileText.Length) + "\r\n\r\n{\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"languageId\":\"javascript\",\"version\":0,\"text\":\"" + fileText + "\"}}}\\n      ";
    }
}
