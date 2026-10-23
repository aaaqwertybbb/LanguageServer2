using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class Scope
{
    public Scope(Scope? parent, BodyKind bodyKind, ref int _unknownOpenBracesThatNeedMatched)
    {
        _unknownOpenBracesThatNeedMatched = 0;
        Parent = parent;
        Type = bodyKind;
    }

    public BodyKind Type { get; set; }
    public List<SyntaxNode> BodyList { get; } = new();
    public Position Start { get; private set; }
    public Position End { get; private set; }
    public bool IsStarted { get; set; }
    public bool IsEnded { get; set; }
    public int CountOfChildrenThatAreDefinitions { get; set; }

    public Scope? Parent { get; set; }

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
            }
            
        }

        return false;
    }
}

/// <summary>
/// Might be wrong right off the bat to use a global node rather than just inferring global due to a lack of parent idk I'll figure it out at some later point I just gotta type something first.
///
/// Actually it says body in the first json example.
/// < {
/// <   "type": "Program",
/// <   "body": [...
/// </summary>
//public class GlobalNode : SyntaxNode
//{
//    public override SyntaxKind SyntaxKind => SyntaxKind.GlobalNode;
//}


