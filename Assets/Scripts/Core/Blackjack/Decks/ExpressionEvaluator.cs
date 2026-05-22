using System;
using System.Globalization;

namespace Blackjack.Decks
{
    /// <summary>
    /// Avalia expressões matemáticas simples usadas nas cartas.
    /// Suporta: +, -, *, x (multiplicação como *), /, ×, ÷, √, ², ³, parênteses e "?" (Ás = 11).
    /// Preparado para expressões compostas dos inimigos avançados (ex: "(10 + 5 - 7)").
    /// </summary>
    public static class ExpressionEvaluator
    {
        public static int Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0;

            int pos = 0;
            string s = expression.Trim();
            double result = ParseExpression(s, ref pos);
            return (int)Math.Round(result);
        }

        // expression = term (('+' | '-') term)*
        private static double ParseExpression(string s, ref int pos)
        {
            double left = ParseTerm(s, ref pos);
            SkipSpaces(s, ref pos);

            while (pos < s.Length && IsAddOrSub(s[pos]))
            {
                char op = s[pos++];
                double right = ParseTerm(s, ref pos);
                left = op == '+' ? left + right : left - right;
                SkipSpaces(s, ref pos);
            }

            return left;
        }

        // term = unary (('*'|'x'|'X'|'×'|'/'|'÷') unary)*
        private static double ParseTerm(string s, ref int pos)
        {
            double left = ParseUnary(s, ref pos);
            SkipSpaces(s, ref pos);

            while (pos < s.Length && IsMulOrDiv(s[pos]))
            {
                char op = s[pos++];
                double right = ParseUnary(s, ref pos);
                left = (op == '/' || op == '÷') ? left / right : left * right;
                SkipSpaces(s, ref pos);
            }

            return left;
        }

        // unary = '√' unary | '-' unary | postfix
        private static double ParseUnary(string s, ref int pos)
        {
            SkipSpaces(s, ref pos);

            if (pos < s.Length && s[pos] == '\u221A') // √
            {
                pos++;
                return Math.Sqrt(ParseUnary(s, ref pos));
            }

            if (pos < s.Length && (s[pos] == '-' || s[pos] == '\u2212'))
            {
                pos++;
                return -ParseUnary(s, ref pos);
            }

            return ParsePostfix(s, ref pos);
        }

        // postfix = primary ('²' | '³')*
        private static double ParsePostfix(string s, ref int pos)
        {
            double val = ParsePrimary(s, ref pos);
            SkipSpaces(s, ref pos);

            while (pos < s.Length && (s[pos] == '\u00B2' || s[pos] == '\u00B3'))
            {
                int exp = s[pos] == '\u00B2' ? 2 : 3;
                val = Math.Pow(val, exp);
                pos++;
                SkipSpaces(s, ref pos);
            }

            return val;
        }

        // primary = NUMBER | '?' | '(' expression ')'
        private static double ParsePrimary(string s, ref int pos)
        {
            SkipSpaces(s, ref pos);

            if (pos < s.Length && s[pos] == '?')
            {
                pos++;
                return 11;
            }

            if (pos < s.Length && s[pos] == '(')
            {
                pos++;
                double val = ParseExpression(s, ref pos);
                SkipSpaces(s, ref pos);
                if (pos < s.Length && s[pos] == ')')
                    pos++;
                return val;
            }

            return ParseNumber(s, ref pos);
        }

        private static double ParseNumber(string s, ref int pos)
        {
            SkipSpaces(s, ref pos);
            int start = pos;

            while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '.'))
                pos++;

            if (pos == start)
                return 0;

            return double.Parse(s.Substring(start, pos - start), CultureInfo.InvariantCulture);
        }

        private static bool IsAddOrSub(char c) =>
            c == '+' || c == '-' || c == '\u2212';

        private static bool IsMulOrDiv(char c) =>
            c == '*' || c == 'x' || c == 'X' || c == '/' || c == '\u00D7' || c == '\u00F7';

        private static void SkipSpaces(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos]))
                pos++;
        }
    }
}
