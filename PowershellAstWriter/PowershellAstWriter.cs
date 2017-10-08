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
                throw new ArgumentNullException("ast");

            if (!ast.EndBlock.Statements.Any())
                return string.Empty;

            var pipe = ast.EndBlock.Statements.OfType<PipelineAst>().First();

            return string.Join(" | ", pipe.PipelineElements.Select(TranslateCommand));
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
                default:
                    throw new Exception("unhandled");
            }
        }

        private string TranslateExpression(object expression)
        {
            var type = expression.GetType();
            if (type == typeof(StringConstantExpressionAst))
            {
                var constant = (StringConstantExpressionAst)expression;
                switch (constant.StringConstantType)
                {
                    case StringConstantType.BareWord:
                        return constant.Value;
                    case StringConstantType.DoubleQuoted:
                        return '"' + constant.Value + '"';
                    case StringConstantType.SingleQuoted:
                        return '\'' + constant.Value + '\'';
                    default:
                        throw new Exception("unhandled");
                }
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
    }
}
