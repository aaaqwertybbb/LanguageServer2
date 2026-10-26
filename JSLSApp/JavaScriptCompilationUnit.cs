using JSLSApp.Syntax;
using System.Text;

namespace JSLSApp;

public class JavaScriptCompilationUnit
{
    public JavaScriptCompilationUnit(List<FunctionDefinitionSyntax> functionDefinitionStartPositionList, GlobalNode globalNode, Dictionary<int, SyntaxNode> lexicalScope)
    {
        FunctionDefinitionStartPositionList = functionDefinitionStartPositionList;
        GlobalNode = globalNode;
        LexicalScope = lexicalScope;
    }

    // TODO: This isn't optimal (and is incorrect given the lack of contextual information) but I want to get a "proof of concept"...
    // ...by getting a list of all the functions and then goto definition-ing one or something like this.
    public List<FunctionDefinitionSyntax> FunctionDefinitionStartPositionList { get; set; }

    public GlobalNode GlobalNode { get; set; }

    /// <summary>
    /// TODO: Are you certain this tends to be the local scope specifically?...
    /// ...I'm thinking based on some text you can match to some locally defined/declared node...
    /// ...but maybe this would more-so imply every available defined/declared node i.e.: the ones in parent scopes?
    /// 
    /// TODO: probably just have 1 dictionary active ever as you go for the instant lookups rather than per scope. (i.e.: you'd prob just update this as you move through the scopes and overwrite older scopes with the???
    /// maybe if shadow you move shadow'd to a list of shadow'd nodes or maybe a stack? and then you can resotre the shadow'd node when you out of the scope that did the shadow.
    /// </summary>
    public Dictionary<int, SyntaxNode> LexicalScope { get; set; }
}
