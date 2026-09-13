namespace JSLSApp;

public enum SyntaxKind
{
    None,
    IdentifierToken,
    NumberToken,
    EndOfFileToken,
    FunctionKeywordToken,
    ClassKeywordToken,
    WhitespaceToken,
    StringToken,
    SingleLineCommentToken,
    MultiLineCommentToken,
    ClassDeclarationNode,
    //GlobalNode,
    FunctionDeclarationNode,
    OpenParenthesisToken,
    CloseParenthesisToken,
    OpenBraceToken,
    CloseBraceToken,
    ImportKeywordToken,
    LetKeywordContextualToken,
    ConstKeywordToken,
    VarKeywordToken,
    VariableDeclarationNode,
    VariableReferenceNode,
}
