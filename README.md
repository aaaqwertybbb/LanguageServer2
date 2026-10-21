# JavaScript_LanguageServer

Publish the JSLSApp project

Modify the editor repo's main.cjs file:
> languageServer = spawn(`C:\\Users\\hunte\\Repos\\New folder (7)\\LanguageServer2\\JSLSApp\\bin\\Release\\net10.0\\publish\\JSLSApp.exe`);

change it to:

> languageServer = spawn(YOUR_ABSOLUTE_PATH_STRING);
