namespace TestProject1;

public class LexerTests
{
    [Theory]
    // Test simple variable declarations
    [InlineData("let x = 42;", new[] { "LET", "IDENT(x)", "ASSIGN", "INT(42)", "SEMICOLON" })]
    // Test operators
    [InlineData("a + b", new[] { "IDENT(a)", "PLUS", "IDENT(b)" })]
    // Test invalid syntax (lexer should handle errors gracefully)
    [InlineData("@", new[] { "ILLEGAL" })]
    public void Tokenizer_ShouldReturn_ExpectedTokenTypes(string source, string[] expectedTokens)
    {
        // 1. Arrange & Act
        var lexer = new Lexer(source);
        var actualTokens = new List<string>();

        Token token;
        do
        {
            token = lexer.NextToken();
            actualTokens.Add(token.ToTestString(source)); // e.g., "LET" or "IDENT(x)"
        } while (token.Type != TokenType.EOF);

        // Remove EOF for easier assertions if desired, then assert
        actualTokens.RemoveAt(actualTokens.Count - 1);

        // 2. Assert
        Assert.Equal(expectedTokens, actualTokens);
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
