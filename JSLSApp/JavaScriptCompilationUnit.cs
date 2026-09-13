using System.Text;

namespace JSLSApp;

public class JavaScriptCompilationUnit
{
    public JavaScriptCompilationUnit(List<FunctionDefinitionSyntax> functionDefinitionStartPositionList, List<SyntaxNode> bodyList, Dictionary<string, SyntaxNode> lexicalScope)
    {
        FunctionDefinitionStartPositionList = functionDefinitionStartPositionList;
        BodyList = bodyList;
        LexicalScope = lexicalScope;
    }

    // TODO: This isn't optimal (and is incorrect given the lack of contextual information) but I want to get a "proof of concept"...
    // ...by getting a list of all the functions and then goto definition-ing one or something like this.
    public List<FunctionDefinitionSyntax> FunctionDefinitionStartPositionList { get; set; }

    public List<SyntaxNode> BodyList { get; set; }

    /// <summary>
    /// TODO: Are you certain this tends to be the local scope specifically?...
    /// ...I'm thinking based on some text you can match to some locally defined/declared node...
    /// ...but maybe this would more-so imply every available defined/declared node i.e.: the ones in parent scopes?
    /// </summary>
    public Dictionary<string, SyntaxNode> LexicalScope { get; set; }

    public string GetString()
    {
        // to string makes me anxious and there's too many decisions to be made right now

        // json serialization...?

        var sb = new StringBuilder();
        var indentationCount = 0;
        var indentationString = new string(' ', 0);

        sb.Append(indentationString); sb.Append("{\n");
        indentationCount++;
        indentationString = new string(' ', indentationCount);

        sb.Append(indentationString); sb.Append("\"type\": "); sb.Append("\"Program\",\n");
        sb.Append(indentationString); sb.Append("\"body\": "); sb.Append("[\n");
        indentationCount++;
        indentationString = new string(' ', indentationCount);
        foreach (var node in BodyList)
        {
            node.AppendString(sb, ref indentationCount, ref indentationString);
        }
        indentationCount--;
        indentationString = new string(' ', indentationCount);
        sb.Append(indentationString); sb.Append("],\n");

        indentationCount--;
        indentationString = new string(' ', indentationCount);
        sb.Append(indentationString); sb.Append("}");

        return sb.ToString();
    }
}
