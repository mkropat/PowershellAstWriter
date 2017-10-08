using System;
using System.Linq;
using System.Management.Automation.Language;

namespace PowershellAstWriter
{
    public class PowershellAstWriter
    {
        public string Write(ScriptBlockAst ast) {
            if (!ast.EndBlock.Statements.Any())
            {
                return string.Empty;
            }
            var pipe = ast.EndBlock.Statements.OfType<PipelineAst>().First();

            var cmd = pipe.PipelineElements.First();
            var cmdCmd = cmd as CommandAst;
            var cmdExpression = cmd as CommandExpressionAst;

            if (cmdCmd != null)
            {
                var prefix = "";
                if (cmdCmd.InvocationOperator != TokenKind.Unknown)
                {
                    prefix = TranslateToken(cmdCmd.InvocationOperator) + " ";
                }
                return prefix + TranslateExpression(cmdCmd.CommandElements.First());
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
