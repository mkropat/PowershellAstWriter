using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;

namespace PowershellAstWriter
{
    public class PowershellAstWriter
    {
        public string Write(ScriptBlockAst ast, string indent="    ")
        {
            if (ast == null)
                throw new ArgumentNullException(nameof(ast));

            return string.Join("\r\n", TranslateScript(ast).Select(x => x.Render(indent)));
        }

        static IEnumerable<Line> TranslateScript(ScriptBlockAst script)
        {
            if (!script.EndBlock.Statements.Any())
                return Enumerable.Empty<Line>();

            return script.EndBlock.Statements.SelectMany(x => TranslateStatement(x));
        }

        static IEnumerable<Line> TranslateStatementBlock(StatementBlockAst block)
        {
            var statements = block.Statements.SelectMany(x => TranslateStatement(x));
            return new[] { new Line("{") }
                .Concat(statements.Select(line => line.Indent()))
                .Concat(new [] { new Line("}") });
        }

        static IEnumerable<Line> TranslateStatement(StatementAst statement)
        {
            switch (statement)
            {
                case AssignmentStatementAst s:
                    foreach (var line in new Line($"{TranslateExpression(s.Left)} {TranslateToken(s.Operator)} ").Join(TranslateStatement(s.Right)))
                        yield return line;
                    yield break;

                case CommandBaseAst s:
                    yield return new Line(TranslateCommand(s));
                    yield break;

                case IfStatementAst s:
                    var first = s.Clauses.First();
                    foreach (var line in new Line($"if ({TranslatePipeline(first.Item1)}) ").Join(TranslateStatementBlock(first.Item2)))
                        yield return line;

                    var subsequent = s.Clauses.Skip(1);
                    var subsequentLines = subsequent.SelectMany(x => new Line($"elseif ({TranslatePipeline(x.Item1)}) ").Join(TranslateStatementBlock(x.Item2)));
                    foreach (var line in subsequentLines)
                        yield return line;

                    if (s.ElseClause != null)
                    {
                        foreach (var line in new Line("else ").Join(TranslateStatementBlock(s.ElseClause)))
                            yield return line;
                    }

                    yield break;

                case PipelineAst s:
                    yield return new Line(TranslatePipeline(s));
                    yield break;

                default:
                    throw new Exception("unhandled");
            }
        }

        static string TranslatePipeline(PipelineBaseAst pipeline)
        {
            switch (pipeline)
            {
                case PipelineAst p:
                    return string.Join(" | ", p.PipelineElements.Select(TranslateCommand));
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
                    return $"{{ {string.Join("; ", TranslateScript(e.ScriptBlock).Select(x => x.Text))} }}";

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
