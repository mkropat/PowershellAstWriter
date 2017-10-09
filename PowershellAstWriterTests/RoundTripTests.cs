using NUnit.Framework;
using System;
using System.Management.Automation.Language;

namespace PowershellAstWriterTests
{
    public class RoundTripTests
    {
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

        [TestCase("$derp")]
        public void ItRoundTripsVariables(string code)
        {
            RoundTrip(code, code);
        }

        [TestCase("\"herp $($derp) flerp\"")]
        public void ItRoundTripsExpandableStrings(string code)
        {
            RoundTrip(code, code);
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

        [TestCase("$derp.Frobnicate()")]
        [TestCase("$derp.Frobnicate(123, 456, 789)")]
        [TestCase("$derp::Frobnicate(123, 456, 789)")]
        public void ItRoundTripsMethodCalls(string code)
        {
            RoundTrip(code, code);
        }

        [TestCase("$herp.Derp")]
        [TestCase("$herp::Derp")]
        public void ItRoundTripsMemberAccess(string code)
        {
            RoundTrip(code, code);
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

        [TestCase("{ 42 }")]
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

        void RoundTrip(string code, string expected)
        {
            var ast = Parser.ParseInput(code, out Token[] tokens, out ParseError[] errors);
            var subject = new PowershellAstWriter.PowershellAstWriter();
            Assert.AreEqual(expected, subject.Write(ast));
        }
    }
}
