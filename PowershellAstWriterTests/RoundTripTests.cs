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

        [TestCase("$derp.Frobnicate()")]
        [TestCase("$derp.Frobnicate(123, 456, 789)")]
        [TestCase("$derp::Frobnicate(123, 456, 789)")]
        public void ItRoundTripsMethodCalls(string code)
        {
            RoundTrip(code, code);
        }

        [TestCase("Invoke-SomeCmdlet | Invoke-AnotherCmdlet | Invoke-AThirdCmdlet")]
        [TestCase("42 | Invoke-SomeCmdlet")]
        public void ItRoundTripsPipelines(string code)
        {
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
