using JSLSApp.LspTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace JSLSApp;

public abstract class SyntaxNode
{
    public abstract SyntaxKind SyntaxKind { get; }
    public abstract Position Start { get; set; }
    public abstract Position End { get; set; }

    /// <summary>
    /// I'm being a bit awkward with how strictly I'm following the example
    /// but I just need the first version to work.
    /// 
    /// As well the JSON may or may not be a simple default serialization.
    /// Based on the JSON it seems every node has a list of children... or maybe the body.
    /// But maybe this is all a shared list or etc... I have no idea.
    /// </summary>
    public abstract IdKind Id_type { get; }
    public abstract string Id_name { get; }
    public Body? Body { get; set; }
    /*
    < {
<   "type": "ClassDeclaration",
<   "id": { "type": "Identifier", "name": "Foo" },
<   "body": {
<     "type": "ClassBody",
<     "body": [
<       {
<         "type": "UnregisteredNode",
<         "tokens": ['Bar', '(', ')', '{', 'console', '.', 'log', ...],
<         "start": { "line": 2, "column": 1 },
<         "end": { "line": 4, "column": 2 }
<       }
<     ]
<   }
< }*/

    public virtual void AppendString(StringBuilder sb, ref int indentationCount, ref string indentationString)
    {
        /*
         * anxiety/procrastination: I need to just start doing something I REALLY do NOT like how this reads it is just terrible lol
         */

        sb.Append(indentationString); sb.Append("{\n");
        indentationCount++;
        indentationString = new string(' ', indentationCount);

        sb.Append(indentationString); sb.Append("\"type\": "); sb.Append($"{SyntaxKind},\n");
        sb.Append(indentationString); sb.Append("\"id\": { "); sb.Append("\"type\": "); sb.Append($"\"{Id_type}\", "); sb.Append("\"name\": "); sb.Append($"\"{Id_name}\" }},\n");
        
        
        sb.Append(indentationString); sb.Append("\"start\": { "); sb.Append("\"line\": "); sb.Append($"{Start.line}, "); sb.Append("\"column\": "); sb.Append($"{Start.character} }},\n");
        sb.Append(indentationString); sb.Append("\"end\": { "); sb.Append("\"line\": "); sb.Append($"{End.line}, "); sb.Append("\"column\": "); sb.Append($"{End.character} }},\n");


        indentationCount--;
        indentationString = new string(' ', indentationCount);
        sb.Append(indentationString); sb.Append("},\n");
    }
}

public class ClassDeclarationNode : SyntaxNode
{
    public ClassDeclarationNode(string id_name, int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Id_name = id_name;
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.ClassDeclarationNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    public override string Id_name { get; }
}

public class FunctionDeclarationNode : SyntaxNode
{
    public FunctionDeclarationNode(string id_name, int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Id_name = id_name;
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.FunctionDeclarationNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    public override string Id_name { get; }
}

public class VariableDeclarationNode : SyntaxNode
{
    public VariableDeclarationNode(string id_name, int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Id_name = id_name;
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.VariableDeclarationNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    public override string Id_name { get; }
}

public class VariableReferenceNode : SyntaxNode
{
    public VariableReferenceNode(string id_name, int startLine, int startCharacter, int endLine, int endCharacter, int definitionLine)
    {
        Id_name = id_name;
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);
        DefinitionLine = definitionLine;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.VariableReferenceNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    public override string Id_name { get; }
    public int DefinitionLine { get; set; }
}

public enum IdKind
{
    Identifier,
}

public class Body
{
    public Body(Scope parent, BodyKind bodyKind, ref int _unknownOpenBracesThatNeedMatched)
    {
        Scope = new(parent, this);
        _unknownOpenBracesThatNeedMatched = 0;
        Type = bodyKind;
    }

    public BodyKind Type { get; set; }
    public List<SyntaxNode> BodyList { get; } = new();
    public Scope Scope { get; set; }
    public Position Start { get; private set; }
    public Position End { get; private set; }
    public bool IsStarted { get; set; }
    public bool IsEnded { get; set; }

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
}

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

public enum BodyKind
{
    //GlobalBody,
    ClassBody,
    FunctionBody,
    GlobalBody,

    EXTERNAL_FunctionBody,
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


