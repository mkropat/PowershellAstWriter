using NUnit.Framework;
using System.Management.Automation.Language;

namespace PowershellAstWriterTests
{
    public class RoundTripTests
    {
        [TestCase("", "")]
        [TestCase("42", "42")]
        [TestCase("\"derp\"", "\"derp\"")]
        [TestCase("'derp'", "'derp'")]
        public void ItRoundTripsConstants(string code, string expected)
        {
            RoundTrip(code, expected);
        }

        [TestCase("$derp", "$derp")]
        public void ItRoundTripsVariables(string code, string expected)
        {
            RoundTrip(code, expected);
        }

        [TestCase("Invoke-SomeCmdlet", "Invoke-SomeCmdlet")]
        [TestCase("& Invoke-SomeCmdlet", "& Invoke-SomeCmdlet")]
        public void ItRoundTripsInvocations(string code, string expected)
        {
            RoundTrip(code, expected);
        }

        void RoundTrip(string code, string expected)
        {
            var ast = Parser.ParseInput(code, out Token[] tokens, out ParseError[] errors);
            var subject = new PowershellAstWriter.PowershellAstWriter();
            Assert.AreEqual(expected, subject.Write(ast));
        }
    }
}
