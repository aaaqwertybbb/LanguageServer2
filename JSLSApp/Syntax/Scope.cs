using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public struct Scope
{
    public BodyKind Type { get; set; }
    /// <summary>
    /// If you ever move a node from one scope's BodyList to another, you need to ensure that the moved node isn't tracking its parent.
    /// If it is tracking its parent, you need to update things accordingly.
    /// TODO: Consider making a function that adds a node rather than invoking the List type's 'Add' function so that the above described logic can be encapsulated.
    /// </summary>
    public List<SyntaxNode> BodyList { get; set; }
    public Position Start { get; private set; }
    public Position End { get; private set; }
    public bool IsStarted { get; set; }
    public bool IsEnded { get; set; }
    public int CountOfChildrenThatAreDefinitions { get; set; }

    public SyntaxNode? ParentScopeProvider { get; set; }

    public void SetStart(Position start)
    {
        Start = start;
        IsStarted = true;
    }

    public void SetEnd(Position end)
    {
        End = end;
        IsEnded = true;
    }

    [Obsolete]
    public BodyKind GetBodyKind()
    {
        return Type;
    }

    [Obsolete]
    public List<SyntaxNode> GetBodyList(List<SyntaxNode> globalBodyList)
    {
        return BodyList;
    }

    public bool AttemptEndScope(Position position, SyntaxKind syntaxKind)
    {
        if (!IsEnded)
        {
            switch (Type)
            {
                case BodyKind.ClassBody:
                    if (syntaxKind == SyntaxKind.CloseBraceToken)
                    {
                        SetEnd(position);
                        return true;
                    }
                    break;
                case BodyKind.FunctionBody:
                    if (syntaxKind == SyntaxKind.CloseBraceToken)
                    {
                        SetEnd(position);
                        return true;
                    }
                    break;
                case BodyKind.IfStatementBody:
                    if (syntaxKind == SyntaxKind.CloseBraceToken)
                    {
                        SetEnd(position);
                        return true;
                    }
                    break;
                case BodyKind.ForStatementBody:
                    if (syntaxKind == SyntaxKind.CloseBraceToken)
                    {
                        SetEnd(position);
                        return true;
                    }
                    break;
                case BodyKind.BlockBody:
                    if (syntaxKind == SyntaxKind.CloseBraceToken)
                    {
                        SetEnd(position);
                        return true;
                    }
                    break;
            }
            
        }

        return false;
    }
}
