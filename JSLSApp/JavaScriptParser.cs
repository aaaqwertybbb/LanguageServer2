using JSLSApp.LspTypes;
using System.Text;

namespace JSLSApp;

/// <summary>
/// Current intention is 1 parser instance per document.
/// This is possibly GC heavy design.
/// But I'm thinking I'll only have an instance for an "open" file.
/// And that the parser would understand the edits made to the document to re-parse the document quickly.
/// And if needed, a CompilationUnit would be the representation of the document semantically,
/// and this CompilationUnit would exist independent of a file being "open".
/// </summary>
public class JavaScriptParser
{
    private JavaScriptDocument _doc;
    private readonly JavaScriptWorkspace _javaScriptWorkspace;
    private int _pos = 0;
    private int _indexLine = 0;
    private int _indexChar = 0;

    public byte[] SyntaxHighlightByteList { get => _syntaxHighlightByteList; set => _syntaxHighlightByteList = value; }

    private List<FunctionDefinitionSyntax> _functionDefinitionStartPositionList = new();
    private List<SyntaxNode> _bodyList = new();
    private byte[] _syntaxHighlightByteList = new byte[0];
    /// <summary>
    /// TODO: Perhaps storing the _indexLine prior to invoking Lex and then checking if it changed is equivalent functionality with less overhead.
    /// </summary>
    private bool _seenLineEnd_flagForStringsAndComments;

    /// <summary>
    /// _peekToken is also used for temporary storage, when the boolean '_peekTokenExists' is false.
    /// </summary>
    private SyntaxToken _peekToken;
    private bool _peekTokenExists;

    private Scope _globalScope;

    public Scope _currentScope = new(parent:null, body:null);

    /// <summary>
    /// Resets the count when a new known scope is open,
    /// i.e.: this still doesn't fully solve the problem but it could delay corrupt state
    /// when the file is 100% correct maybe?
    /// 
    /// As well, having to remember to reset this when you open a scope is very unfortunate...
    /// </summary>
    private int _unknownOpenBracesThatNeedMatched;

    public JavaScriptParser(JavaScriptDocument doc, JavaScriptWorkspace javaScriptWorkspace)
    {
        _doc = doc;
        _javaScriptWorkspace = javaScriptWorkspace;
        _currentScope = new(parent: null, body: null);
        _globalScope = _currentScope;
    }

    public SyntaxToken PeekToken()
    {
        if (_peekTokenExists)
        {
            return _peekToken;
        }
        while ((_peekToken = Lex()).SyntaxKind == SyntaxKind.WhitespaceToken);
        _peekTokenExists = true;
        return _peekToken;
    }

    public SyntaxToken ConsumePeekToken()
    {
        _peekTokenExists = false;
        return _peekToken;
    }

    public SyntaxToken NextToken()
    {
        _peekTokenExists = false;
        // Using _peekToken for temporary storage, the boolean '_peekTokenExists' is still false.
        while ((_peekToken = Lex()).SyntaxKind == SyntaxKind.WhitespaceToken);
        return _peekToken;
    }

    public SyntaxToken Defensive_SkipUntil_LexFor_OrEof(SyntaxKind syntaxKind)
    {
        _peekTokenExists = false;
        // Using _peekToken for temporary storage, the boolean '_peekTokenExists' is still false.
        while ((_peekToken = Lex()).SyntaxKind != syntaxKind)
        {
            if (_peekToken.SyntaxKind == SyntaxKind.EndOfFileToken)
                break;
        }
        return _peekToken;
    }

    public JavaScriptCompilationUnit Parse()
    {
        var stringBuilder = new StringBuilder(capacity: 64);
        _syntaxHighlightByteList = new byte[_doc.Chars.Count];

        _doc.HasBeenParsedAtLeastOnce = true;

        while (_pos < _doc.Chars.Count)
        {
            SyntaxToken token;
            if (_peekTokenExists)
            {
                token = ConsumePeekToken();
            }
            else
            {
                token = NextToken();
            }
            switch (token.SyntaxKind)
            {
                case SyntaxKind.EndOfFileToken:
                    goto exitOuterWhileLoop;
                case SyntaxKind.FunctionKeywordToken:
                    ParseFunctionDefinitionNode(stringBuilder, default, false);
                    break;
                case SyntaxKind.ClassKeywordToken:
                    ParseClassDefinitionNode(stringBuilder);
                    break;
                case SyntaxKind.ImportKeywordToken:
                    ParseImportNode(stringBuilder);
                    break;
                case SyntaxKind.IdentifierToken:
                    if (_currentScope.GetBodyKind() == BodyKind.ClassBody &&
                        PeekToken().SyntaxKind == SyntaxKind.OpenParenthesisToken)
                    {
                        ParseFunctionDefinitionNode(stringBuilder, token, true);
                    }
                    else
                    {
                        ParseAssumedReference(stringBuilder, token);
                    }
                    // TODO: 'this' keyword.
                    break;
                case SyntaxKind.StringToken:
                    if (_seenLineEnd_flagForStringsAndComments)
                    {
                    }
                    break;
                case SyntaxKind.MultiLineCommentToken:
                    SyntaxHighlightByteList.SetSyntaxHighlighting(token.Start, token.Length, TrackedSyntaxKind.Comment);
                    if (_seenLineEnd_flagForStringsAndComments)
                    {
                    }
                    break;
                case SyntaxKind.CloseBraceToken:
                    if (_unknownOpenBracesThatNeedMatched > 0)
                    {
                        _unknownOpenBracesThatNeedMatched--;
                    }
                    else if (_currentScope.AttemptEndScope(token.Position, token.SyntaxKind) && _currentScope.Parent is not null)
                    {
                        _currentScope = _currentScope.Parent;
                    }
                    break;
                case SyntaxKind.OpenBraceToken:
                    _unknownOpenBracesThatNeedMatched++;
                    break;
                case SyntaxKind.LetKeywordContextualToken:
                case SyntaxKind.ConstKeywordToken:
                case SyntaxKind.VarKeywordToken:
                    ParseVariableDeclaration(token, stringBuilder);
                    break;
                case SyntaxKind.WhitespaceToken:
                    break;
            }
        }

        exitOuterWhileLoop:
        return new JavaScriptCompilationUnit(_functionDefinitionStartPositionList, _bodyList, _globalScope.LexicalScope);
    }

    private void ParseAssumedReference(StringBuilder stringBuilder, SyntaxToken token)
    {
        // TODO: Constructing a string here is likely to be extremely GC expensive
        // TODO: Presuming that the entry was added then just taking the most recent function definition perhaps is a bit hacky; I'm not sure
        stringBuilder.Clear();
        for (int k = 0; k < token.Length; k++)
        {
            stringBuilder.Append(_doc.Chars[(_pos - token.Length) + k]);
        }

        var identifierText = stringBuilder.ToString();

        var ccc = 2;

        // I'm not lexing every keyword so this has a lot of keywords showing up.
        // As well yes this probably is gonna be a massive GC spike, but I'm thinking one thing at a time for the moment.

        var scope = _currentScope;

        while (true)
        {
            if (scope.LexicalScope.TryGetValue(identifierText, out var definition))
            {
                var variableReferenceNode = new VariableReferenceNode(identifierText, token.Position.line, token.Position.character, _indexLine, _indexChar, definition.Start.line);
                _currentScope.GetBodyList(_bodyList).Add(variableReferenceNode);
                break;
            }

            if (scope.Parent is null)
            {
                break;
            }
            else
            {
                scope = scope.Parent;
            }
        }
        

        // in editorGlobal.js
        // if I hover the first variable I see in the global scope it doesn't work.
        // etc...
        //
        // but then at the end of the file. I can start hovering some within a function and it does work...
    }

    private void ParseVariableDeclaration(SyntaxToken token, StringBuilder stringBuilder)
    {
        /*
        TODO: How do you say it... "implicit global variable declaration"? Lacking let,var,const when defining a variable that isn't defined.
        */
        var aaa = 2;
        var identifierToken = PeekToken();
        if (identifierToken.SyntaxKind == SyntaxKind.IdentifierToken)
        {
            ConsumePeekToken();
            // TODO: Constructing a string here is likely to be extremely GC expensive
            // TODO: Presuming that the entry was added then just taking the most recent function definition perhaps is a bit hacky; I'm not sure
            stringBuilder.Clear();
            for (int k = 0; k < identifierToken.Length; k++)
            {
                stringBuilder.Append(_doc.Chars[(_pos - identifierToken.Length) + k]);
            }

            var bbb = stringBuilder.ToString();
            var ccc = 2;
            var variableDeclarationNode = new VariableDeclarationNode(bbb, identifierToken.Position.line, identifierToken.Position.character, _indexLine, _indexChar);

            _currentScope.GetBodyList(_bodyList).Add(variableDeclarationNode);
            _currentScope.LexicalScope.TryAdd(variableDeclarationNode.Id_name, variableDeclarationNode);
        }

        // in editorGlobal.js
        // if I hover the first variable I see in the global scope it doesn't work.
        // etc...
        //
        // but then at the end of the file. I can start hovering some within a function and it does work...
        
    }

    private void ParseImportNode(StringBuilder stringBuilder)
    {
        return;
    }

    public void ParseFunctionDefinitionNode(StringBuilder stringBuilder, SyntaxToken identifierToken, bool identifierTokenExists)
    {
        // TODO: if (!identifierTokenExists) then you have a 'function' keyword defined function...
        // ...otherwise you have a function defined within a class that doesn't have the 'function' keyword...
        // ...TODO: Why is the function defined within a class logic 1 character off (the ' - 1' whereas the 'function' keyword defined function doesn't need this?
        // TODO: You probably lex'd the braces and parentheses wrong.

        if (!identifierTokenExists)
        {
            identifierToken = PeekToken();
            if (identifierToken.SyntaxKind != SyntaxKind.IdentifierToken)
                return;
            _ = ConsumePeekToken();
        }

        if (!identifierTokenExists)
        {
            // TODO: Constructing a string here is likely to be extremely GC expensive
            // TODO: Presuming that the entry was added then just taking the most recent function definition perhaps is a bit hacky; I'm not sure
            stringBuilder.Clear();
            for (int k = 0; k < identifierToken.Length; k++)
            {
                stringBuilder.Append(_doc.Chars[(_pos - identifierToken.Length) + k]);
            }
        }
        else
        {
            // TODO: Constructing a string here is likely to be extremely GC expensive
            // TODO: Presuming that the entry was added then just taking the most recent function definition perhaps is a bit hacky; I'm not sure
            stringBuilder.Clear();
            for (int k = 0; k < identifierToken.Length; k++)
            {
                stringBuilder.Append(_doc.Chars[(_pos - identifierToken.Length - 1) + k]);
            }
        }
        
        var str = stringBuilder.ToString();

        if (!identifierTokenExists)
        {
            _functionDefinitionStartPositionList[^1].Name = str;
        }
        
        var functionDeclarationNode = new FunctionDeclarationNode(str, identifierToken.Position.line, identifierToken.Position.character, _indexLine, _indexChar);
        _currentScope.GetBodyList(_bodyList).Add(functionDeclarationNode);

        _currentScope.LexicalScope.Add(functionDeclarationNode.Id_name, functionDeclarationNode);

        ParseFunctionArguments();
        ParseFunctionBody(functionDeclarationNode);
    }

    public void ParseFunctionArguments()
    {
        var token = PeekToken();
        if (token.SyntaxKind != SyntaxKind.OpenParenthesisToken)
            return;
        _ = ConsumePeekToken();
        // ----
        token = Defensive_SkipUntil_LexFor_OrEof(SyntaxKind.CloseParenthesisToken);
        if (token.SyntaxKind != SyntaxKind.CloseParenthesisToken)
            return;
    }

    public void ParseFunctionBody(FunctionDeclarationNode functionDeclarationNode)
    {
        var token = PeekToken();
        if (token.SyntaxKind != SyntaxKind.OpenBraceToken)
            return;
        _ = ConsumePeekToken();
        var openBraceToken = token;
        // ----
        //token = Defensive_SkipUntil_LexFor_OrEof(SyntaxKind.CloseBraceToken);
        //if (token.SyntaxKind != SyntaxKind.CloseBraceToken)
        //    return;
        //var closeBraceToken = token;

        functionDeclarationNode.Body = new Body(_currentScope, BodyKind.FunctionBody, ref _unknownOpenBracesThatNeedMatched);
        functionDeclarationNode.Body.SetStart(openBraceToken.Position);
        //functionDeclarationNode.Body.SetEnd(closeBraceToken.Position);

        _currentScope = functionDeclarationNode.Body.Scope;
    }


    public void ParseClassDefinitionNode(StringBuilder stringBuilder)
    {
        var token = PeekToken();
        if (token.SyntaxKind != SyntaxKind.IdentifierToken)
            return;
        _ = ConsumePeekToken();

        // TODO: Constructing a string here is likely to be extremely GC expensive
        // TODO: Presuming that the entry was added then just taking the most recent function definition perhaps is a bit hacky; I'm not sure
        stringBuilder.Clear();
        for (int k = 0; k < token.Length; k++)
        {
            stringBuilder.Append(_doc.Chars[(_pos - token.Length) + k]);
        }
        var classDeclarationNode = new ClassDeclarationNode(stringBuilder.ToString(), token.Position.line, token.Position.character, _indexLine, _indexChar);
        _currentScope.GetBodyList(_bodyList).Add(classDeclarationNode);

        _currentScope.LexicalScope.Add(classDeclarationNode.Id_name, classDeclarationNode);

        ParseClassBody(classDeclarationNode);
    }

    public void ParseClassBody(ClassDeclarationNode classDeclarationNode)
    {
        var token = PeekToken();
        if (token.SyntaxKind != SyntaxKind.OpenBraceToken)
            return;
        _ = ConsumePeekToken();
        var openBraceToken = token;
        //// ----
        //token = Defensive_SkipUntil_LexFor_OrEof(SyntaxKind.CloseBraceToken);
        //if (token.SyntaxKind != SyntaxKind.CloseBraceToken)
        //    return;
        //var closeBraceToken = token;

        classDeclarationNode.Body = new Body(_currentScope, BodyKind.ClassBody, ref _unknownOpenBracesThatNeedMatched);
        classDeclarationNode.Body.SetStart(openBraceToken.Position);
        //classDeclarationNode.Body.SetEnd(closeBraceToken.Position);

        _currentScope = classDeclarationNode.Body.Scope;
    }

    public SyntaxToken Lex()
    {
        while (_pos < _doc.Chars.Count)
        {
            switch (_doc.Chars[_pos])
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '_':
                    return Lex_IdentifierOrKeyword();
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
                    return Lex_Number();
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    return Lex_Whitespace();
                case '/':
                    if (_pos <= _doc.Chars.Count - 2)
                    {
                        if (_doc.Chars[_pos + 1] == '/')
                        {
                            return Lex_SingleLineComment();
                        }
                        else if (_doc.Chars[_pos + 1] == '*')
                        {
                            return Lex_MultiLineComment();
                        }
                    }
                    break;
                case '\"':
                    return Lex_String('\"');
                case '\'':
                    return Lex_String('\'');
                case '`':
                    return Lex_String('`');
                case '(':
                    {
                        _pos++;
                        var startPosition = new Position(_indexLine, _indexChar);
                        _indexChar++;
                        return new SyntaxToken(SyntaxKind.OpenParenthesisToken, startPosition, 1);
                    }
                case ')':
                    {
                        _pos++;
                        var startPosition = new Position(_indexLine, _indexChar);
                        _indexChar++;
                        return new SyntaxToken(SyntaxKind.CloseParenthesisToken, startPosition, 1);
                    }
                case '{':
                    {
                        _pos++;
                        var startPosition = new Position(_indexLine, _indexChar);
                        _indexChar++;
                        return new SyntaxToken(SyntaxKind.OpenBraceToken, startPosition, 1);
                    }
                case '}':
                    {
                        _pos++;
                        var startPosition = new Position(_indexLine, _indexChar);
                        _indexChar++;
                        return new SyntaxToken(SyntaxKind.CloseBraceToken, startPosition, 1);
                    }
                default:
                    break;
            }

            _pos++;
        }

        return new SyntaxToken(SyntaxKind.EndOfFileToken, new Position(_indexLine, _indexChar), 0);
    }

    /// <summary>
    /// TODO: Usage of reserved words with '@' prefix
    /// </summary>
    public SyntaxToken Lex_IdentifierOrKeyword()
    {
        // 'charIntSum' is a heuristic to detect possible keywords.
        // This is the only way I've thought to make this work and I'm not overly focused on optimizing this heuristic at the moment so I'm gonna continue using it.
        // You sum every character in the word, and enter a switch statement to compare that sum against hardcoded sums of every keyword that exists in the language.
        //
        var charIntSum = 0; // TODO: Google AI "a starting seed" // A standard starting seed for DJB2 is 5381"
        var startPosition = new Position(_indexLine, _indexChar);
        
        // TODO: if you just let it enter the loop you'd get rid of this specific line of duplicated hash logic
        charIntSum = ((charIntSum << 5) - charIntSum) + _doc.Chars[_pos];

        var length = 1;
        _pos++;
        _indexChar++;


        while (_pos < _doc.Chars.Count)
        {
            if (char.IsLetterOrDigit(_doc.Chars[_pos]))
            {
                length++;
                charIntSum = ((charIntSum << 5) - charIntSum) + _doc.Chars[_pos];
            }
            else
            {
                if (_doc.Chars[_pos] == '_')
                {
                    length++;
                    charIntSum = ((charIntSum << 5) - charIntSum) + _doc.Chars[_pos];
                }
                else
                {
                    break;
                }
            }

            _pos++;
            _indexChar++;
        }

        var syntaxKind = SyntaxKind.IdentifierToken;

        switch (charIntSum)
        {
            case 94844771: // const
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'c' &&
                    _doc.Chars[_pos - 4] == 'o' &&
                    _doc.Chars[_pos - 3] == 'n' &&
                    _doc.Chars[_pos - 2] == 's' &&
                    _doc.Chars[_pos - 1] == 't')
                {
                    syntaxKind = SyntaxKind.ConstKeywordToken;
                    break;
                }
                break;
            case 107035: // let
                if (length == 3 &&
                    _doc.Chars[_pos - 3] == 'l' &&
                    _doc.Chars[_pos - 2] == 'e' &&
                    _doc.Chars[_pos - 1] == 't')
                {
                    syntaxKind = SyntaxKind.LetKeywordToken;
                    break;
                }
                break;
            case 1380938712: // function
                if (length == 8 &&
                    _doc.Chars[_pos - 8] == 'f' &&
                    _doc.Chars[_pos - 7] == 'u' &&
                    _doc.Chars[_pos - 6] == 'n' &&
                    _doc.Chars[_pos - 5] == 'c' &&
                    _doc.Chars[_pos - 4] == 't' &&
                    _doc.Chars[_pos - 3] == 'i' &&
                    _doc.Chars[_pos - 2] == 'o' &&
                    _doc.Chars[_pos - 1] == 'n')
                {
                    syntaxKind = SyntaxKind.FunctionKeywordToken;
                    break;
                }
                break;
            case 3357: // if
                if (length == 2 &&
                    _doc.Chars[_pos - 2] == 'i' &&
                    _doc.Chars[_pos - 1] == 'f')
                {
                    syntaxKind = SyntaxKind.IfKeywordToken;
                    break;
                }
                break;
            case 115131: // try
                if (length == 3 &&
                    _doc.Chars[_pos - 3] == 't' &&
                    _doc.Chars[_pos - 2] == 'r' &&
                    _doc.Chars[_pos - 1] == 'y')
                {
                    syntaxKind = SyntaxKind.TryKeywordToken;
                    break;
                }
                break;
            case 101577: // for
                if (length == 3 &&
                    _doc.Chars[_pos - 3] == 'f' &&
                    _doc.Chars[_pos - 2] == 'o' &&
                    _doc.Chars[_pos - 1] == 'r')
                {
                    syntaxKind = SyntaxKind.ForKeywordToken;
                    break;
                }
                break;
            case 116519: // var
                if (length == 3 &&
                    _doc.Chars[_pos - 3] == 'v' &&
                    _doc.Chars[_pos - 2] == 'a' &&
                    _doc.Chars[_pos - 1] == 'r')
                {
                    syntaxKind = SyntaxKind.VarKeywordToken;
                    break;
                }
                break;
            case 94432955: // catch
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'c' &&
                    _doc.Chars[_pos - 4] == 'a' &&
                    _doc.Chars[_pos - 3] == 't' &&
                    _doc.Chars[_pos - 2] == 'c' &&
                    _doc.Chars[_pos - 1] == 'h')
                {
                    syntaxKind = SyntaxKind.CatchKeywordToken;
                    break;
                }
                break;
            case -934396624: // return
                if (length == 6 &&
                    _doc.Chars[_pos - 6] == 'r' &&
                    _doc.Chars[_pos - 5] == 'e' &&
                    _doc.Chars[_pos - 4] == 't' &&
                    _doc.Chars[_pos - 3] == 'u' &&
                    _doc.Chars[_pos - 2] == 'r' &&
                    _doc.Chars[_pos - 1] == 'n')
                {
                    syntaxKind = SyntaxKind.ReturnKeywordToken;
                    break;
                }
                break;
            case -889473228: // switch
                if (length == 6 &&
                    _doc.Chars[_pos - 6] == 's' &&
                    _doc.Chars[_pos - 5] == 'w' &&
                    _doc.Chars[_pos - 4] == 'i' &&
                    _doc.Chars[_pos - 3] == 't' &&
                    _doc.Chars[_pos - 2] == 'c' &&
                    _doc.Chars[_pos - 1] == 'h')
                {
                    syntaxKind = SyntaxKind.SwitchKeywordToken;
                    break;
                }
                break;
            case 3046192: // case
                if (length == 4 &&
                    _doc.Chars[_pos - 4] == 'c' &&
                    _doc.Chars[_pos - 3] == 'a' &&
                    _doc.Chars[_pos - 2] == 's' &&
                    _doc.Chars[_pos - 1] == 'e')
                {
                    syntaxKind = SyntaxKind.CaseKeywordToken;
                    break;
                }
                break;
            case 93127292: // async
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'a' &&
                    _doc.Chars[_pos - 4] == 's' &&
                    _doc.Chars[_pos - 3] == 'y' &&
                    _doc.Chars[_pos - 2] == 'n' &&
                    _doc.Chars[_pos - 1] == 'c')
                {
                    syntaxKind = SyntaxKind.AsyncKeywordToken;
                    break;
                }
                break;
            case 3116345: // else
                if (length == 4 &&
                    _doc.Chars[_pos - 4] == 'e' &&
                    _doc.Chars[_pos - 3] == 'l' &&
                    _doc.Chars[_pos - 2] == 's' &&
                    _doc.Chars[_pos - 1] == 'e')
                {
                    syntaxKind = SyntaxKind.ElseKeywordToken;
                    break;
                }
                break;
            case 1544803905: // default
                if (length == 7 &&
                    _doc.Chars[_pos - 7] == 'd' &&
                    _doc.Chars[_pos - 6] == 'e' &&
                    _doc.Chars[_pos - 5] == 'f' &&
                    _doc.Chars[_pos - 4] == 'a' &&
                    _doc.Chars[_pos - 3] == 'u' &&
                    _doc.Chars[_pos - 2] == 'l' &&
                    _doc.Chars[_pos - 1] == 't')
                {
                    syntaxKind = SyntaxKind.DefaultKeywordToken;
                    break;
                }
                break;
            case 110339814: // throw
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 't' &&
                    _doc.Chars[_pos - 4] == 'h' &&
                    _doc.Chars[_pos - 3] == 'r' &&
                    _doc.Chars[_pos - 2] == 'o' &&
                    _doc.Chars[_pos - 1] == 'w')
                {
                    syntaxKind = SyntaxKind.ThrowKeywordToken;
                    break;
                }
                break;
            case 108960: // new
                if (length == 3 &&
                    _doc.Chars[_pos - 3] == 'n' &&
                    _doc.Chars[_pos - 2] == 'e' &&
                    _doc.Chars[_pos - 1] == 'w')
                {
                    syntaxKind = SyntaxKind.NewKeywordToken;
                    break;
                }
                break;
            case 93223254: // await
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'a' &&
                    _doc.Chars[_pos - 4] == 'w' &&
                    _doc.Chars[_pos - 3] == 'a' &&
                    _doc.Chars[_pos - 2] == 'i' &&
                    _doc.Chars[_pos - 1] == 't')
                {
                    syntaxKind = SyntaxKind.AwaitKeywordToken;
                    break;
                }
                break;
            case 94742904: // class
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'c' &&
                    _doc.Chars[_pos - 4] == 'l' &&
                    _doc.Chars[_pos - 3] == 'a' &&
                    _doc.Chars[_pos - 2] == 's' &&
                    _doc.Chars[_pos - 1] == 's')
                {
                    syntaxKind = SyntaxKind.ClassKeywordToken;
                    break;
                }
                break;
            case -1588406278: // constructor
                if (length == 11 &&
                    _doc.Chars[_pos - 11] == 'c' &&
                    _doc.Chars[_pos - 10] == 'o' &&
                    _doc.Chars[_pos - 9]  == 'n' &&
                    _doc.Chars[_pos - 8]  == 's' &&
                    _doc.Chars[_pos - 7]  == 't' &&
                    _doc.Chars[_pos - 6]  == 'r' &&
                    _doc.Chars[_pos - 5]  == 'u' &&
                    _doc.Chars[_pos - 4]  == 'c' &&
                    _doc.Chars[_pos - 3]  == 't' &&
                    _doc.Chars[_pos - 2]  == 'o' &&
                    _doc.Chars[_pos - 1]  == 'r')
                {
                    syntaxKind = SyntaxKind.ConstructorKeywordToken;
                    break;
                }
                break;
            case -1184795739: // import
                if (length == 6 &&
                    _doc.Chars[_pos - 6] == 'i' &&
                    _doc.Chars[_pos - 5] == 'm' &&
                    _doc.Chars[_pos - 4] == 'p' &&
                    _doc.Chars[_pos - 3] == 'o' &&
                    _doc.Chars[_pos - 2] == 'r' &&
                    _doc.Chars[_pos - 1] == 't')
                {
                    syntaxKind = SyntaxKind.ImportKeywordToken;
                    break;
                }
                break;
            case 3151786: // from
                if (length == 4 &&
                    _doc.Chars[_pos - 4] == 'f' &&
                    _doc.Chars[_pos - 3] == 'r' &&
                    _doc.Chars[_pos - 2] == 'o' &&
                    _doc.Chars[_pos - 1] == 'm')
                {
                    syntaxKind = SyntaxKind.FromKeywordToken;
                    break;
                }
                break;
            case -1289153612: // export
                if (length == 6 &&
                    _doc.Chars[_pos - 6] == 'e' &&
                    _doc.Chars[_pos - 5] == 'x' &&
                    _doc.Chars[_pos - 4] == 'p' &&
                    _doc.Chars[_pos - 3] == 'o' &&
                    _doc.Chars[_pos - 2] == 'r' &&
                    _doc.Chars[_pos - 1] == 't')
                {
                    syntaxKind = SyntaxKind.ExportKeywordToken;
                    break;
                }
                break;
            case 3559070: // this
                if (length == 4 &&
                    _doc.Chars[_pos - 4] == 't' &&
                    _doc.Chars[_pos - 3] == 'h' &&
                    _doc.Chars[_pos - 2] == 'i' &&
                    _doc.Chars[_pos - 1] == 's')
                {
                    syntaxKind = SyntaxKind.ThisKeywordToken;
                    break;
                }
                break;
            case 113101617: // while
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'w' &&
                    _doc.Chars[_pos - 4] == 'h' &&
                    _doc.Chars[_pos - 3] == 'i' &&
                    _doc.Chars[_pos - 2] == 'l' &&
                    _doc.Chars[_pos - 1] == 'e')
                {
                    syntaxKind = SyntaxKind.WhileKeywordToken;
                    break;
                }
                break;
            case 94001407: // break
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'b' &&
                    _doc.Chars[_pos - 4] == 'r' &&
                    _doc.Chars[_pos - 3] == 'e' &&
                    _doc.Chars[_pos - 2] == 'a' &&
                    _doc.Chars[_pos - 1] == 'k')
                {
                    syntaxKind = SyntaxKind.BreakKeywordToken;
                    break;
                }
                break;
            case -567202649: // continue
                if (length == 8 &&
                    _doc.Chars[_pos - 8] == 'c' &&
                    _doc.Chars[_pos - 7] == 'o' &&
                    _doc.Chars[_pos - 6] == 'n' &&
                    _doc.Chars[_pos - 5] == 't' &&
                    _doc.Chars[_pos - 4] == 'i' &&
                    _doc.Chars[_pos - 3] == 'n' &&
                    _doc.Chars[_pos - 2] == 'u' &&
                    _doc.Chars[_pos - 1] == 'e')
                {
                    syntaxKind = SyntaxKind.ContinueKeywordToken;
                    break;
                }
                break;
            case 3569038: // true
                if (length == 4 &&
                    _doc.Chars[_pos - 4] == 't' &&
                    _doc.Chars[_pos - 3] == 'r' &&
                    _doc.Chars[_pos - 2] == 'u' &&
                    _doc.Chars[_pos - 1] == 'e')
                {
                    syntaxKind = SyntaxKind.TrueKeywordToken;
                    break;
                }
                break;
            case 97196323: // false
                if (length == 5 &&
                    _doc.Chars[_pos - 5] == 'f' &&
                    _doc.Chars[_pos - 4] == 'a' &&
                    _doc.Chars[_pos - 3] == 'l' &&
                    _doc.Chars[_pos - 2] == 's' &&
                    _doc.Chars[_pos - 1] == 'e')
                {
                    syntaxKind = SyntaxKind.FalseKeywordToken;
                    break;
                }
                break;
            case 3392903: // null
                if (length == 4 &&
                    _doc.Chars[_pos - 4] == 'n' &&
                    _doc.Chars[_pos - 3] == 'u' &&
                    _doc.Chars[_pos - 2] == 'l' &&
                    _doc.Chars[_pos - 1] == 'l')
                {
                    syntaxKind = SyntaxKind.NullKeywordToken;
                    break;
                }
                break;
            default:
                break;
        }

        return new SyntaxToken(syntaxKind, startPosition, length);
    }

    /// <summary>
    /// TODO: alternative syntaxes for typing numbers; supports '123' and '123.456'
    /// </summary>
    public SyntaxToken Lex_Number()
    {
        var startPosition = new Position(_indexLine, _indexChar);
        var length = 1;
        _pos++;
        _indexChar++;

        while (_pos < _doc.Chars.Count)
        {
            if (char.IsDigit(_doc.Chars[_pos]))
            {
                length++;
            }
            else
            {
                if (_doc.Chars[_pos] == '.')
                {
                    length++;
                }
                else
                {
                    break;
                }
            }

            _pos++;
            _indexChar++;
        }

        return new SyntaxToken(SyntaxKind.NumberToken, startPosition, length);
    }

    public SyntaxToken Lex_Whitespace()
    {
        var startPosition = new Position(_indexLine, _indexChar);
        var length = 1;
        switch (_doc.Chars[_pos])
        {
            case '\r':
                _indexLine++;
                _indexChar = 0;
                if (_pos <= _doc.Chars.Count - 2)
                {
                    if (_doc.Chars[_pos + 1] == '\n')
                    {
                        _pos++;
                    }
                }
                break;
            case '\n':
                _indexLine++;
                _indexChar = 0;
                break;
            default:
                _indexChar++;
                break;
        }
        _pos++;


        while (_pos < _doc.Chars.Count)
        {
            if (char.IsWhiteSpace(_doc.Chars[_pos]))
            {
                length++;
            }
            else
            {
                break;
            }

            switch (_doc.Chars[_pos])
            {
                case '\r':
                    _indexLine++;
                    _indexChar = 0;
                    if (_pos <= _doc.Chars.Count - 2)
                    {
                        if (_doc.Chars[_pos + 1] == '\n')
                        {
                            _pos++;
                        }
                    }
                    break;
                case '\n':
                    _indexLine++;
                    _indexChar = 0;
                    break;
                default:
                    _indexChar++;
                    break;
            }
            _pos++;
        }

        return new SyntaxToken(SyntaxKind.WhitespaceToken, startPosition, length);
    }

    public SyntaxToken Lex_String(char terminator)
    {
        _seenLineEnd_flagForStringsAndComments = false;

        var startPosition = new Position(_indexLine, _indexChar);
        var length = 1;
        _pos++;
        _indexChar++;

        while (_pos < _doc.Chars.Count)
        {
            switch (_doc.Chars[_pos])
            {
                case '\r':
                    length++;
                    _pos++;
                    _indexLine++;
                    _indexChar = 0;
                    if (_pos <= _doc.Chars.Count - 1)
                    {
                        if (_doc.Chars[_pos] == '\n')
                        {
                            // I'm going to have everything length wise as though '\r\n' are just '\n'.
                            // Maybe is best to make start and end positions I'm not sure.
                            // Either way my goal right now is to get the 'function' "keyword" appearing in text to not result in lsp saying a function definition exists there.
                            _pos++;
                        }
                    }
                    if (terminator == '`')
                    {
                        _seenLineEnd_flagForStringsAndComments = true;
                        break;
                    }
                    else
                    {
                        goto functionEnding;
                    }
                case '\n':
                    length++;
                    _pos++;
                    _indexLine++;
                    _indexChar = 0;
                    if (terminator == '`')
                    {
                        _seenLineEnd_flagForStringsAndComments = true;
                        break;
                    }
                    else
                    {
                        goto functionEnding;
                    }
                case '\\':
                    length++;
                    _pos++;
                    _indexChar++;
                    if (_pos <= _doc.Chars.Count - 1)
                    {
                        length++;
                        _pos++;
                        _indexChar++;
                    }
                    break;
                default:
                    if (_doc.Chars[_pos] == terminator)
                    {
                        length++;
                        _pos++;
                        _indexChar++;
                        goto functionEnding;
                    }
                    length++;
                    _pos++;
                    _indexChar++;
                    break;
            }
        }

        functionEnding:
        SyntaxHighlightByteList.SetSyntaxHighlighting(_pos - length, length, TrackedSyntaxKind.String);
        return new SyntaxToken(SyntaxKind.StringToken, startPosition, length, _pos - length);
    }

    public SyntaxToken Lex_SingleLineComment()
    {
        var startPosition = new Position(_indexLine, _indexChar);
        var length = 2;
        _pos += 2;
        _indexChar += 2;

        while (_pos < _doc.Chars.Count)
        {
            switch (_doc.Chars[_pos])
            {
                case '\r':
                    length++;
                    _pos++;
                    _indexLine++;
                    _indexChar = 0;
                    if (_pos <= _doc.Chars.Count - 1)
                    {
                        if (_doc.Chars[_pos] == '\n')
                        {
                            // I'm going to have everything length wise as though '\r\n' are just '\n'.
                            // Maybe is best to make start and end positions I'm not sure.
                            // Either way my goal right now is to get the 'function' "keyword" appearing in text to not result in lsp saying a function definition exists there.
                            _pos++;
                        }
                    }
                    goto functionEnding;
                case '\n':
                    length++;
                    _pos++;
                    _indexLine++;
                    _indexChar = 0;
                    goto functionEnding;
                default:
                    length++;
                    _pos++;
                    _indexChar++;
                    break;
            }
        }

        functionEnding:
        SyntaxHighlightByteList.SetSyntaxHighlighting(_pos - length, length, TrackedSyntaxKind.Comment);
        return new SyntaxToken(SyntaxKind.SingleLineCommentToken, startPosition, length);
    }

    /// <summary>
    /// I'm going to have everything length wise as though '\r\n' are just '\n'.
    /// </summary>
    public SyntaxToken Lex_MultiLineComment()
    {
        _seenLineEnd_flagForStringsAndComments = false;

        var startPosition = new Position(_indexLine, _indexChar);
        var length = 2;
        _pos += 2;
        _indexChar += 2;

        while (_pos < _doc.Chars.Count)
        {
            switch (_doc.Chars[_pos])
            {
                case '*':
                    length++;
                    _pos++;
                    _indexChar++;
                    if (_pos <= _doc.Chars.Count - 1 &&
                        _doc.Chars[_pos] == '/')
                    {
                        length++;
                        _pos++;
                        _indexChar++;
                        goto functionEnding;
                    }
                    break;
                case '\r':
                    _seenLineEnd_flagForStringsAndComments = true;
                    length++;
                    _pos++;
                    _indexLine++;
                    _indexChar = 0;
                    if (_pos <= _doc.Chars.Count - 1)
                    {
                        if (_doc.Chars[_pos] == '\n')
                        {
                            // I'm going to have everything length wise as though '\r\n' are just '\n'.
                            // Maybe is best to make start and end positions I'm not sure.
                            // Either way my goal right now is to get the 'function' "keyword" appearing in text to not result in lsp saying a function definition exists there.
                            _pos++;
                        }
                    }
                    break;
                case '\n':
                    _seenLineEnd_flagForStringsAndComments = true;
                    length++;
                    _pos++;
                    _indexLine++;
                    _indexChar = 0;
                    break;
                default:
                    length++;
                    _pos++;
                    _indexChar++;
                    break;
            }
        }

        functionEnding:
        return new SyntaxToken(SyntaxKind.MultiLineCommentToken, startPosition, length, _pos - length);
    }
}

/*
 * - [ ] Maybe will do what I call, but don't actually know the name of,  "implicit global variable declaration" where you leave off the 'let', 'const', or 'var' for an undefined variable that you assign.
*/
