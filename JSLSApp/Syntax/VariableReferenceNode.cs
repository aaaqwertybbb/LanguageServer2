using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class VariableReferenceNode : SyntaxNode
{
    public VariableReferenceNode(int distanceFromPreviousNode, int length, SyntaxNode variableDeclarationNode)
    {
        DistanceFromPreviousNode = distanceFromPreviousNode;
        Length = length;
        VariableDeclarationNode = variableDeclarationNode;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.VariableReferenceNode;

    public override int DistanceFromPreviousNode { get; set; }
    public override int Length { get; set; }
    /// <summary>
    /// TODO: Probably don't store this I don't know.
    /// Also, is there a way to ask the runtime what references to a certain address exist? Or something like that.
    /// If you change VariableDeclarationNode you might have to invalidate the references. Can I "ask C# who holds a reference to the definition?".
    /// 
    /// TODO: Don't make this a 'SyntaxNode'
    /// </summary>
    public SyntaxNode VariableDeclarationNode { get; set; }

    public override bool IsScopeProvider => false;
    public override bool IsIdentifiable => true;

    private int _identifierHash;

    public override ref int GetIdentifierHash() => ref _identifierHash;
}
