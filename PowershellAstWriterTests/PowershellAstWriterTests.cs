using NUnit.Framework;
using System;
using System.Linq;
using System.Management.Automation.Language;

namespace PowershellAstWriterTests
{
    public class PowershellAstWriterTests
    {
        IScriptExtent StubExtent = new ScriptExtent(new ScriptPosition("stub", 0, 0, string.Empty), new ScriptPosition("stub", 0, 0, string.Empty));

        [Test]
        public void ItThrowsArgumentNullExceptionWhenPassedNull()
        {
            var subject = new PowershellAstWriter.PowershellAstWriter();
            Assert.Throws<ArgumentNullException>(() => subject.Write(null));
        }

        [TestCase("")]
        [TestCase("42")]
        [TestCase("\"derp\"")]
        [TestCase("'derp'")]
        public void ItRoundTripsConstants(string code)
        {
            RoundTrip(code, code);
        }

        [Test]
        public void ItRendersConstantExpressionAsts()
        {
            var constant = new ConstantExpressionAst(StubExtent, 42);

            Assert.AreEqual("42", WriteExpression(constant));
        }

        [TestCase("$derp")]
        public void ItRoundTripsVariables(string code)
        {
            RoundTrip(code, code);
        }

        [Test]
        public void ItRendersVariableExpressionAst()
        {
            var variable = new VariableExpressionAst(StubExtent, "derp", splatted: false);

            Assert.AreEqual("$derp", WriteExpression(variable));
        }

        [TestCase("\"herp $($derp) flerp\"")]
        public void ItRoundTripsExpandableStrings(string code)
        {
            RoundTrip(code, code);
        }

        [Test]
        public void ItRendersExpandableStringExpressionAst()
        {
            var expandableExpression = new ExpandableStringExpressionAst(StubExtent, "herp $($derp) flerp", StringConstantType.DoubleQuoted);

            Assert.AreEqual("\"herp $($derp) flerp\"", WriteExpression(expandableExpression));
        }

        [TestCase("Invoke-SomeCmdlet")]
        [TestCase("& Invoke-SomeCmdlet")]
        [TestCase("Invoke-SomeCmdlet herp derp flerp")]
        [TestCase("Invoke-SomeCmdlet -Herp -Derp -Flerp")]
        [TestCase("Invoke-SomeCmdlet -Herp 123 -Derp 456 -Flerp 789")]
        [TestCase("Invoke-SomeCmdlet -Herp:$derp")]
        public void ItRoundTripsInvocations(string code)
        {
            RoundTrip(code, code);
        }

        [TestCase("-bnot $derp")]
        [TestCase("-not $derp")]
        [TestCase("$herp -eq $derp")]
        [TestCase("$herp -ieq $derp", "$herp -eq $derp")]
        [TestCase("$herp -ceq $derp")]
        [TestCase("$herp -ne $derp")]
        [TestCase("$herp -ine $derp", "$herp -ne $derp")]
        [TestCase("$herp -cne $derp")]
        [TestCase("$herp -ge $derp")]
        [TestCase("$herp -ige $derp", "$herp -ge $derp")]
        [TestCase("$herp -cge $derp")]
        [TestCase("$herp -gt $derp")]
        [TestCase("$herp -igt $derp", "$herp -gt $derp")]
        [TestCase("$herp -cgt $derp")]
        [TestCase("$herp -lt $derp")]
        [TestCase("$herp -ilt $derp", "$herp -lt $derp")]
        [TestCase("$herp -clt $derp")]
        [TestCase("$herp -le $derp")]
        [TestCase("$herp -ile $derp", "$herp -le $derp")]
        [TestCase("$herp -cle $derp")]
        [TestCase("$herp -like $derp")]
        [TestCase("$herp -ilike $derp", "$herp -like $derp")]
        [TestCase("$herp -clike $derp")]
        [TestCase("$herp -notlike $derp")]
        [TestCase("$herp -inotlike $derp", "$herp -notlike $derp")]
        [TestCase("$herp -cnotlike $derp")]
        [TestCase("$herp -match $derp")]
        [TestCase("$herp -imatch $derp", "$herp -match $derp")]
        [TestCase("$herp -cmatch $derp")]
        [TestCase("$herp -notmatch $derp")]
        [TestCase("$herp -inotmatch $derp", "$herp -notmatch $derp")]
        [TestCase("$herp -cnotmatch $derp")]
        [TestCase("$herp -replace $derp")]
        [TestCase("$herp -ireplace $derp", "$herp -replace $derp")]
        [TestCase("$herp -creplace $derp")]
        [TestCase("$herp -contains $derp")]
        [TestCase("$herp -icontains $derp", "$herp -contains $derp")]
        [TestCase("$herp -ccontains $derp")]
        [TestCase("$herp -notcontains $derp")]
        [TestCase("$herp -inotcontains $derp", "$herp -notcontains $derp")]
        [TestCase("$herp -cnotcontains $derp")]
        [TestCase("$herp -in $derp")]
        [TestCase("$herp -iin $derp", "$herp -in $derp")]
        [TestCase("$herp -cin $derp")]
        [TestCase("$herp -notin $derp")]
        [TestCase("$herp -inotin $derp", "$herp -notin $derp")]
        [TestCase("$herp -cnotin $derp")]
        [TestCase("$herp -split $derp")]
        [TestCase("$herp -isplit $derp", "$herp -split $derp")]
        [TestCase("$herp -csplit $derp")]
        [TestCase("$herp -isnot $derp")]
        [TestCase("$herp -is $derp")]
        [TestCase("$herp -as $derp")]
        [TestCase("$herp -f $derp")]
        [TestCase("$herp -and $derp")]
        [TestCase("$herp -band $derp")]
        [TestCase("$herp -or $derp")]
        [TestCase("$herp -bor $derp")]
        [TestCase("$herp -xor $derp")]
        [TestCase("$herp -bxor $derp")]
        [TestCase("$herp -join $derp")]
        [TestCase("$herp -shl $derp")]
        [TestCase("$herp -shr $derp")]
        public void ItRoundTripsOperators(string code, string expected = null)
        {
            RoundTrip(code, expected ?? code);
        }

        [Test]
        public void ItRendersUnaryExpressionAst()
        {
            var variable = new VariableExpressionAst(StubExtent, "derp", splatted: false);
            var unary = new UnaryExpressionAst(StubExtent, TokenKind.Not, variable);

            Assert.AreEqual("-not $derp", WriteExpression(unary));
        }

        [Test]
        public void ItRendersBinaryExpressionAst()
        {
            var leftVariable = new VariableExpressionAst(StubExtent, "herp", splatted: false);
            var rightVariable = new VariableExpressionAst(StubExtent, "derp", splatted: false);
            var binary = new BinaryExpressionAst(StubExtent, leftVariable, TokenKind.Ieq, rightVariable, StubExtent);

            Assert.AreEqual("$herp -eq $derp", WriteExpression(binary));
        }

        [TestCase("$derp.Frobnicate()")]
        [TestCase("$derp.Frobnicate(123, 456, 789)")]
        [TestCase("$derp::Frobnicate(123, 456, 789)")]
        public void ItRoundTripsMethodCalls(string code)
        {
            RoundTrip(code, code);
        }

        [Test]
        public void ItRendersInvokeMemberExpressionAst()
        {
            var obj = new VariableExpressionAst(StubExtent, "derp", splatted: false);
            var member = new StringConstantExpressionAst(StubExtent, "Frobnicate", StringConstantType.BareWord);
            var invokeMember = new InvokeMemberExpressionAst(StubExtent, obj, member, Enumerable.Empty<ExpressionAst>(), @static: false);

            Assert.AreEqual("$derp.Frobnicate()", WriteExpression(invokeMember));
        }

        [TestCase("$herp.Derp")]
        [TestCase("$herp::Derp")]
        public void ItRoundTripsMemberAccess(string code)
        {
            RoundTrip(code, code);
        }

        [Test]
        public void ItRendersMemberExpressionAst()
        {
            var obj = new VariableExpressionAst(StubExtent, "herp", splatted: false);
            var member = new StringConstantExpressionAst(StubExtent, "Derp", StringConstantType.BareWord);
            var memberExpression = new MemberExpressionAst(StubExtent, obj, member, @static: false);

            Assert.AreEqual("$herp.Derp", WriteExpression(memberExpression));
        }

        [TestCase("Invoke-SomeCmdlet | Invoke-AnotherCmdlet | Invoke-AThirdCmdlet")]
        [TestCase("42 | Invoke-SomeCmdlet")]
        public void ItRoundTripsPipelines(string code)
        {
            RoundTrip(code, code);
        }

        [TestCase("$derp = 42")]
        [TestCase("$derp = 42 | Invoke-SomeCmdlet")]
        public void ItRoundTripsAssignments(string code)
        {
            RoundTrip(code, code);
        }

        [Test]
        public void ItRendersAssignmentStatementAst()
        {
            var left = new VariableExpressionAst(StubExtent, "derp", splatted: false);
            var right = new CommandExpressionAst(StubExtent, new ConstantExpressionAst(StubExtent, 42), Enumerable.Empty<RedirectionAst>());
            var assignment = new AssignmentStatementAst(StubExtent, left, TokenKind.Equals, right, StubExtent);

            Assert.AreEqual("$derp = 42", WriteStatement(assignment));
        }

        [TestCase("{ 42 }")]
        [TestCase("{ 'herp'; 'derp'; 'flerp' }")]
        public void ItRoundTripsScriptBlocks(string code)
        {
            RoundTrip(code, code);
        }

        [Test]
        public void ItRoundTripsMultipleStatements()
        {
            var code = @"123
456";
            RoundTrip(code, code);
        }

        [Test]
        public void ItRoundTripsIfStatements()
        {
            var code = @"if ($Derp) {
    Invoke-SomeCommand
    Invoke-AnotherCommand
}";
            RoundTrip(code, code);
        }

        [Test]
        public void ItRendersIfStatementAst()
        {

            var derpVar = new VariableExpressionAst(StubExtent, "Derp", splatted: false);
            var condition = new PipelineAst(StubExtent, new CommandAst(StubExtent, new[] { derpVar }, TokenKind.Unknown, Enumerable.Empty<RedirectionAst>()));
            var command = new CommandExpressionAst(StubExtent, new StringConstantExpressionAst(StubExtent, "Invoke-SomeCommand", StringConstantType.BareWord), Enumerable.Empty<RedirectionAst>());
            var statement = new StatementBlockAst(StubExtent, new[] { command }, Enumerable.Empty<TrapStatementAst>());
            var ifStatement = new IfStatementAst(StubExtent, new [] { Tuple.Create<PipelineBaseAst, StatementBlockAst>(condition, statement) }, elseClause: null);

            var expected = @"if ($Derp) {
    Invoke-SomeCommand
}";
            Assert.AreEqual(expected, WriteStatement(ifStatement));
        }

        [Test]
        public void ItRoundTripsIfChains()
        {
            var code = @"if ($Herp) {
    Invoke-SomeCommand
}
elseif ($Derp) {
    Invoke-AnotherCommand
}
elseif ($Flerp) {
    Invoke-AThirdCommand
}";
            RoundTrip(code, code);
        }

        [Test]
        public void ItRoundTripsIfElseStatements()
        {
            var code = @"if ($Derp) {
    Invoke-SomeCommand
}
else {
    Invoke-AnotherCommand
}";
            RoundTrip(code, code);
        }

        [Test]
        public void ItRoundTripsNestedIndents()
        {
            var code = @"if ($Herp) {
    if ($Derp) {
        if ($Flerp) {
            Invoke-SomeCommand
            Invoke-AnotherCommand
        }
    }
}";
            RoundTrip(code, code);
        }

        void RoundTrip(string code, string expected)
        {
            var ast = Parser.ParseInput(code, out Token[] tokens, out ParseError[] errors);
            var subject = new PowershellAstWriter.PowershellAstWriter();
            var actual = subject.Write(ast);
            Assert.AreEqual(expected, actual);
        }

        string WriteExpression(ExpressionAst expression)
        {
            var command = new CommandExpressionAst(StubExtent, expression, null);
            var statement = new PipelineAst(StubExtent, new CommandBaseAst[] { command });
            return WriteStatement(statement);
        }

        string WriteStatement(StatementAst statement)
        {
            var ast = new ScriptBlockAst(StubExtent, null, new StatementBlockAst(StubExtent, new[] { statement }, null), false);

            var subject = new PowershellAstWriter.PowershellAstWriter();
            return subject.Write(ast);
        }
    }
}
