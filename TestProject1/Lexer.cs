using System;
using System.Collections.Generic;
using System.Text;

namespace TestProject1;

internal class Lexer(string source)
{
    private readonly string _source = source;

    private int _pos;

    internal Token NextToken()
    {
        while (true)
        {
            if (_pos >= _source.Length)
            {
                return new Token(TokenType.EOF, _source.Length, _source.Length);
            }

            switch (_source[_pos])
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return LexNumber();
                default:
                    _pos++;
                    break;
            }
        }
    }

    public Token LexNumber()
    {
        var start = _pos++;

        while (char.IsDigit(_source[_pos]))
        {
            _pos++;
        }

        return new Token(TokenType.EOF, start, _pos);
    }
}
