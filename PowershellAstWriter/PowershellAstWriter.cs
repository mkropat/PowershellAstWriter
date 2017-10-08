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

        public string TranslateScript(ScriptBlockAst script)
        {
            if (!script.EndBlock.Statements.Any())
                return string.Empty;

            return TranslateStatement(script.EndBlock.Statements.First());
        }

        private string TranslateStatement(StatementAst statement)
        {
            var type = statement.GetType();
            if (type == typeof(PipelineAst))
            {
                var pipe = (PipelineAst)statement;
                return string.Join(" | ", pipe.PipelineElements.Select(TranslateCommand));
            }
            else if (type == typeof(AssignmentStatementAst))
            {
                var assignment = (AssignmentStatementAst)statement;
                return $"{TranslateExpression(assignment.Left)} {TranslateToken(assignment.Operator)} {TranslateStatement(assignment.Right)}";
            }
            else if (statement is CommandBaseAst)
            {
                return TranslateCommand((CommandBaseAst)statement);
            }
            else
            {
                throw new Exception("unhandled");
            }
        }

        private string TranslateCommand(CommandBaseAst cmd)
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

        private string TranslateToken(TokenKind token)
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

        private string TranslateExpression(object expression)
        {
            var type = expression.GetType();
            if (type == typeof(StringConstantExpressionAst))
            {
                var constant = (StringConstantExpressionAst)expression;
                return TranslateString(constant.Value, constant.StringConstantType);
            }
            else if (type == typeof (BinaryExpressionAst))
            {
                var binary = (BinaryExpressionAst)expression;
                return TranslateExpression(binary.Left) + " " + TranslateToken(binary.Operator) + " " + TranslateExpression(binary.Right);
            }
            else if (type == typeof(ConstantExpressionAst))
            {
                var constant = (ConstantExpressionAst)expression;
                return constant.Value.ToString();
            }
            else if (type == typeof (CommandParameterAst))
            {
                var parameter = (CommandParameterAst)expression;
                var option = "-" + parameter.ParameterName;
                return parameter.Argument == null ? option : $"{option}:{TranslateExpression(parameter.Argument)}";
            }
            else if (type == typeof (ExpandableStringExpressionAst))
            {
                var str = (ExpandableStringExpressionAst)expression;
                return TranslateString(str.Value, str.StringConstantType);
            }
            else if (type == typeof (InvokeMemberExpressionAst))
            {
                var invokeExpression = (InvokeMemberExpressionAst)expression;
                var argumentList = invokeExpression.Arguments == null
                    ? string.Empty
                    : string.Join(", ", invokeExpression.Arguments.Select(TranslateExpression));
                var separator = invokeExpression.Static ? "::" : ".";
                return TranslateExpression(invokeExpression.Expression) +
                    separator +
                    TranslateExpression(invokeExpression.Member) +
                    $"({argumentList})";
            }
            else if (type == typeof (MemberExpressionAst))
            {
                var member = (MemberExpressionAst)expression;
                var separator = member.Static ? "::" : ".";
                return TranslateExpression(member.Expression) + separator + TranslateExpression(member.Member);
            }
            else if (type == typeof (ScriptBlockExpressionAst))
            {
                var script = (ScriptBlockExpressionAst)expression;
                return $"{{ {TranslateScript(script.ScriptBlock)} }}";
            }
            else if (type == typeof(UnaryExpressionAst))
            {
                var unary = (UnaryExpressionAst)expression;
                return TranslateToken(unary.TokenKind) + ' ' + TranslateExpression(unary.Child);
            }
            else if (type == typeof(VariableExpressionAst))
            {
                var var = (VariableExpressionAst)expression;
                return '$' + var.VariablePath.UserPath;
            }
            else
            {
                throw new Exception("unhandled");
            }
        }

        private string TranslateString(string value, StringConstantType stringType)
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
