using System;
using System.Linq;
using System.Management.Automation.Language;

namespace PowershellAstWriter
{
    public class PowershellAstWriter
    {
        public string Write(ScriptBlockAst ast)
        {
            if (ast == null)
                throw new ArgumentNullException(nameof(ast));

            return TranslateScript(ast);
        }

        static string TranslateScript(ScriptBlockAst script)
        {
            if (!script.EndBlock.Statements.Any())
                return string.Empty;

            return TranslateStatement(script.EndBlock.Statements.First());
        }

        static string TranslateStatement(StatementAst statement)
        {
            switch (statement)
            {
                case AssignmentStatementAst s:
                    return $"{TranslateExpression(s.Left)} {TranslateToken(s.Operator)} {TranslateStatement(s.Right)}";

                case CommandBaseAst s:
                    return TranslateCommand(s);

                case PipelineAst s:
                    return string.Join(" | ", s.PipelineElements.Select(TranslateCommand));

                default:
                    throw new Exception("unhandled");
            }
        }

        static string TranslateCommand(CommandBaseAst cmd)
        {
            var cmdCmd = cmd as CommandAst;
            var cmdExpression = cmd as CommandExpressionAst;

            if (cmdCmd != null)
            {
                var prefix = "";
                if (cmdCmd.InvocationOperator != TokenKind.Unknown)
                {
                    prefix = TranslateToken(cmdCmd.InvocationOperator) + " ";
                }
                return prefix + string.Join(" ", cmdCmd.CommandElements.Select(TranslateExpression));
            }
            else if (cmdExpression != null)
            {
                return TranslateExpression(cmdExpression.Expression);
            }
            else
            {
                throw new Exception("unhandled");
            }
        }

        static string TranslateToken(TokenKind token)
        {
            switch (token)
            {
                case TokenKind.Ampersand:
                    return "&";
                case TokenKind.Equals:
                    return "=";
                default:
                    if (TokenKindMapping.TokenToString.ContainsKey(token))
                        return TokenKindMapping.TokenToString[token];
                    else
                        throw new Exception("unhandled");
            }
        }

        static string TranslateExpression(object expression)
        {
            switch (expression)
            {
                case BinaryExpressionAst e:
                    return TranslateExpression(e.Left) + " " + TranslateToken(e.Operator) + " " + TranslateExpression(e.Right);

                case StringConstantExpressionAst e:
                    return TranslateString(e.Value, e.StringConstantType);

                case ConstantExpressionAst e:
                    return e.Value.ToString();

                case CommandParameterAst e:
                    var option = "-" + e.ParameterName;
                    return e.Argument == null ? option : $"{option}:{TranslateExpression(e.Argument)}";

                case ExpandableStringExpressionAst e:
                    return TranslateString(e.Value, e.StringConstantType);

                case InvokeMemberExpressionAst e:
                    var argumentList = e.Arguments == null
                        ? string.Empty
                        : string.Join(", ", e.Arguments.Select(TranslateExpression));
                    var separator = e.Static ? "::" : ".";
                    return TranslateExpression(e.Expression) +
                        separator +
                        TranslateExpression(e.Member) +
                        $"({argumentList})";

                case MemberExpressionAst e:
                    separator = e.Static ? "::" : ".";
                    return TranslateExpression(e.Expression) + separator + TranslateExpression(e.Member);

                case ScriptBlockExpressionAst e:
                    return $"{{ {TranslateScript(e.ScriptBlock)} }}";

                case UnaryExpressionAst e:
                    return TranslateToken(e.TokenKind) + ' ' + TranslateExpression(e.Child);

                case VariableExpressionAst e:
                    return '$' + e.VariablePath.UserPath;

                default:
                    throw new Exception("unhandled");
            }
        }

        static string TranslateString(string value, StringConstantType stringType)
        {
            switch (stringType)
            {
                case StringConstantType.BareWord:
                    return value;
                case StringConstantType.DoubleQuoted:
                    return '"' + value + '"';
                case StringConstantType.SingleQuoted:
                    return '\'' + value + '\'';
                default:
                    throw new Exception("unhandled");
            }
        }
    }
}
