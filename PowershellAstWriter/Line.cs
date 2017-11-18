using System.Collections.Generic;
using System.Linq;

namespace PowershellAstWriter
{
    public class Line
    {
        public Line(string text, int indentLevel = 0)
        {
            IndentLevel = indentLevel;
            Text = text;
        }

        public string Text { get; }
        public int IndentLevel { get; }

        public string Render(string indent)
        {
            return string.Join(string.Empty, Enumerable.Repeat(indent, IndentLevel)) + Text;
        }

        public Line Indent()
        {
            return new Line(Text, IndentLevel + 1);
        }

        public IEnumerable<Line> Join(IEnumerable<Line> lines)
        {
            return new[] { new Line(Text + lines.First().Text) }.Concat(lines.Skip(1));
        }
    }
}
