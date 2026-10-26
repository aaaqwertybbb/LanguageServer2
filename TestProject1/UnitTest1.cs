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
        JSLSApp.Program.MAIN_decodeMessage(DefinitionRequest);
    }

    public const string InitializeJson = "Content-Length: 331\r\n\r\n{\"method\":\"initialize\",\"id\":0,\"params\":{\"processId\":18260,\"clientInfo\":{\"name\":\"TextEditor123\",\"version\":\"0.0.1\"},\"rootUri\":null,\"capabilities\":{\"workspace\":{}},\"workspaceFolders\":[{\"uri\":\"C:\\\\Users\\\\hunte\\\\Repos\\\\New folder (7)\\\\Editor2\",\"name\":\"2\"},{\"uri\":\"C:\\\\Users\\\\hunte\\\\Repos\\\\New folder (7)\\\\LanguageServer2\",\"name\":\"3\"}]}}\\n";
    // I'm gonna put "      " at the end of this string cuz the length is wrong and it won't read it
    public const string didOpenTextDocumentNotification = "Content-Length: 433\r\n\r\n{\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"languageId\":\"javascript\",\"version\":0,\"text\":\"aaa\\n        /**\\n         * @type {HTMLDivElement}\\n         * Consider the existence of such methods as 'state_cursor_setIndex' before mutating state directly\\n         */xxx/*\\n         */\\n        this.cursorElement = document.createElement('div');\"}}}\\n      ";
    public const string customFullFileLexRequest = "Content-Length: 157\r\n\r\n{\"id\":1,\"method\":\"textDocument/CustomFullFileLexRequest\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"}}}\\n";

    public const string FileText = "function Aaa(bbb, ccc) {\\n    return bbb; return ccc;\\n}";
    public const string CompletionRequest = "Content-Length: 179\r\n\r\n{\"id\":2,\"method\":\"textDocument/completion\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":0,\"character\":0}}}\\n";

    public const string DefinitionRequest = "Content-Length: 180\r\n\r\n{\"id\":3,\"method\":\"textDocument/definition\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\"},\"position\":{\"line\":1,\"character\":23}}}\\n";

    public string Format_didOpenTextDocumentNotification(string fileText)
    {
        return "Content-Length: " + (181 + fileText.Length) + "\r\n\r\n{\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":\"file:///C:/Users/hunte/Repos/New folder (7)/Editor2/testFile.js\",\"languageId\":\"javascript\",\"version\":0,\"text\":\"" + fileText + "\"}}}\\n      ";
    }
}

//public class ParserTests
//{
//    [Theory]
//    [InlineData("if (true) { return 1; }", "IfStatement(Condition: true, Body: [ReturnStatement(1)])")]
//    [InlineData("1 + 2 * 3", "BinaryExpression(+, 1, BinaryExpression(*, 2, 3))")] // Tests operator precedence!
//    public void Parser_ShouldBuild_CorrectAst(string source, string expectedAstString)
//    {
//        var lexer = new Lexer(source);
//        var parser = new Parser(lexer);

//        var program = parser.ParseProgram();

//        Assert.False(parser.HasErrors(), $"Parser had errors: {string.Join(", ", parser.Errors)}");
//        Assert.Equal(expectedAstString, program.ToDebugString());
//    }
//}
