using JSLSApp.LspTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace JSLSApp.Syntax;

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
    public Scope? Scope { get; set; }
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


