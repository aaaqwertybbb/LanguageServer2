using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class Scope
{
    public Scope(Scope? parent, Body? body)
    {
        Parent = parent;
        Body = body;
    }

    public Scope? Parent { get; set; }

    public Body? Body { get; set; }

    /// <summary>
    /// Is strictly text comparison within a single scope
    /// 
    /// TODO: Are you certain this tends to be the local scope specifically?...
    /// ...I'm thinking based on some text you can match to some locally defined/declared node...
    /// ...but maybe this would more-so imply every available defined/declared node i.e.: the ones in parent scopes?
    /// 
    /// TODO: probably just have 1 dictionary active ever as you go for the instant lookups rather than per scope. (i.e.: you'd prob just update this as you move through the scopes and overwrite older scopes with the???
    /// maybe if shadow you move shadow'd to a list of shadow'd nodes or maybe a stack? and then you can resotre the shadow'd node when you out of the scope that did the shadow.
    /// </summary>
    public Dictionary<string, SyntaxNode> LexicalScope { get; } = new();

    public BodyKind GetBodyKind()
    {
        if (Body is null)
        {
            return BodyKind.GlobalBody;
        }

        return Body.Type;
    }

    public List<SyntaxNode> GetBodyList(List<SyntaxNode> globalBodyList)
    {
        if (Body is null)
        {
            return globalBodyList;
        }

        return Body.BodyList;
    }

    public bool AttemptEndScope(Position position, SyntaxKind syntaxKind)
    {
        if (Body is null)
            return false;

        if (!Body.IsEnded)
        {
            switch (Body.Type)
            {
                case BodyKind.ClassBody:
                    if (syntaxKind == SyntaxKind.CloseBraceToken)
                    {
                        Body.SetEnd(position);
                        return true;
                    }
                    break;
                case BodyKind.FunctionBody:
                    if (syntaxKind == SyntaxKind.CloseBraceToken)
                    {
                        Body.SetEnd(position);
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


