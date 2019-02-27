
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMVVM
{
    enum Operator
    {
        Add,
        Sub,
        Mul,
        Div,
    }

    enum Comparator
    {
        Equal,
        NotEqual,
        Greater,
        GEqual,
        Less,
        LEqual,
    }

    internal abstract class BaseExpression
    {
        public abstract object GetValue();
    }

    internal class ComparatorExpression : BaseExpression
    {
        public Comparator comp;
        public BaseExpression lhs;
        public BaseExpression rhs;

        public ComparatorExpression(Comparator comp, BaseExpression lhs, BaseExpression rhs) { this.comp = comp; this.lhs = lhs; this.rhs = rhs; }

        public override object GetValue()
        {
            var l = lhs.GetValue();
            var r = rhs.GetValue();
            switch (comp)
            {
                case Comparator.Equal: return l is string ? l as string == r.ToString() : l == r;
                case Comparator.NotEqual: return l is string ? l as string != r.ToString() : l != r;
                case Comparator.Greater: return Convert.ToDouble(l) > Convert.ToDouble(r);
                case Comparator.GEqual: return Convert.ToDouble(l) >= Convert.ToDouble(r);
                case Comparator.Less: return Convert.ToDouble(l) < Convert.ToDouble(r);
                case Comparator.LEqual: return Convert.ToDouble(l) <= Convert.ToDouble(r);
            }
            return null;
        }
    }

    internal class OperatorExpression : BaseExpression
    {
        public Operator op;
        public BaseExpression lhs;
        public BaseExpression rhs;

        public OperatorExpression(Operator op, BaseExpression lhs, BaseExpression rhs) { this.op = op; this.lhs = lhs; this.rhs = rhs; }

        public override object GetValue()
        {
            if (op == Operator.Add && lhs.GetValue() is string)
            {
                return (lhs.GetValue() as string) + rhs.GetValue().ToString();
            }
            else
            {
                var l = Convert.ToDouble(lhs.GetValue());
                var r = Convert.ToDouble(rhs.GetValue());
                switch (op)
                {
                    case Operator.Add: return l + r;
                    case Operator.Sub: return l - r;
                    case Operator.Mul: return l * r;
                    case Operator.Div: return l / r;
                }
            }

            return null;
        }
    }

    internal class TernaryOperatorExpression : BaseExpression
    {
        public BaseExpression condition;
        public BaseExpression lhs;
        public BaseExpression rhs;

        public TernaryOperatorExpression(BaseExpression condition, BaseExpression lhs, BaseExpression rhs) { this.condition = condition; this.lhs = lhs; this.rhs = rhs; }

        public override object GetValue()
        {
            var value = condition.GetValue();
            if (value == null) return rhs.GetValue();
            if (value is bool) return Convert.ToBoolean(value) ? lhs.GetValue() : rhs.GetValue();
            if (value is int || value is float) return Convert.ToDouble(value) != 0 ? lhs.GetValue() : rhs.GetValue();
            return value != null ? lhs.GetValue() : rhs.GetValue();
        }
    }

    internal class ConstExpression : BaseExpression
    {
        public object value;
        public ConstExpression(object value) { this.value = value; }
        public override object GetValue() { return value; }
    }

    internal class DataExpression : BaseExpression
    {
        public string name;
        public ViewData data = null;
        public DataExpression(string name) { this.name = name; }
        public override object GetValue()
        {
            if (data != null)
                return data.value;
            return null;
        }
    }

    internal class ParserArgument
    {
        private List<BaseExpression> expressions = new List<BaseExpression>();
        public Dictionary<string, DataExpression> dataExpressions = new Dictionary<string, DataExpression>();
        private string ParserText(Match m)
        {
            expressions.Add(new ConstExpression(m.Groups[2].Value));
            return "@" + (expressions.Count - 1);
        }
        private string ParserData(Match m)
        {
            var name = m.Groups[0].Value;
            if (dataExpressions.ContainsKey(name)) return "@" + expressions.IndexOf(dataExpressions[name]);
            var dataExpression = new DataExpression(name);
            dataExpressions[name] = dataExpression;
            expressions.Add(dataExpression);
            return "@" + (expressions.Count - 1);
        }
        private string ParserNegativeNumber(Match m)
        {
            expressions.Add(new ConstExpression(-Convert.ToDouble(m.Groups[2].Value)));
            return m.Groups[1].Value + "@" + (expressions.Count - 1);
        }
        private string ParserNumber(Match m)
        {
            expressions.Add(new ConstExpression(Convert.ToDouble(m.Groups[2].Value)));
            return m.Groups[1].Value + "@" + (expressions.Count - 1);
        }
        private string ParserBrackets(Match m)
        {
            ParserExpression(m.Groups[1].Value);
            return "@" + (expressions.Count - 1);
        }
        private BaseExpression ParserExpression(string argument)
        {
            // * /
            var m = Regex.Match(argument, @"@(\d+)\s*([\*/])\s*@(\d+)");
            if (m.Success)
            {
                expressions.Add(new OperatorExpression(m.Groups[2].Value == "*" ? Operator.Mul : Operator.Div, expressions[Convert.ToInt32(m.Groups[1].Value)], expressions[Convert.ToInt32(m.Groups[3].Value)]));
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            // + -
            m = Regex.Match(argument, @"@(\d+)\s*([\+-])\s*@(\d+)");
            if (m.Success)
            {
                expressions.Add(new OperatorExpression(m.Groups[2].Value == "+" ? Operator.Add : Operator.Sub, expressions[Convert.ToInt32(m.Groups[1].Value)], expressions[Convert.ToInt32(m.Groups[3].Value)]));
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            // > >= < <= == !=
            m = Regex.Match(argument, @"@(\d+)\s*([><=!]=?)\s*@(\d+)");
            if (m.Success)
            {
                Comparator comp;
                switch (m.Groups[2].Value)
                {
                    case ">": comp = Comparator.Greater; break;
                    case ">=": comp = Comparator.GEqual; break;
                    case "<": comp = Comparator.Less; break;
                    case "<=": comp = Comparator.LEqual; break;
                    case "!=": comp = Comparator.NotEqual; break;
                    default: comp = Comparator.Equal; break;
                }
                expressions.Add(new ComparatorExpression(comp, expressions[Convert.ToInt32(m.Groups[1].Value)], expressions[Convert.ToInt32(m.Groups[3].Value)]));
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            // a?b:c
            m = Regex.Match(argument, @"@(\d+)\s*?\s*@(\d+)\s*:\s*@(\d+)");
            if (m.Success)
            {
                expressions.Add(new TernaryOperatorExpression(expressions[Convert.ToInt32(m.Groups[1].Value)], expressions[Convert.ToInt32(m.Groups[2].Value)], expressions[Convert.ToInt32(m.Groups[3].Value)]));
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            //@
            m = Regex.Match(argument, @"@(\d+)");
            if (m.Success) return expressions[Convert.ToInt32(m.Groups[1].Value)];

            Debug.LogError("Error Expression!");
            return null;
        }
        public BaseExpression Parser(string argument)
        {
            if (argument == null || argument.Length == 0) return null;
            argument = Regex.Replace(argument, @"(['""])([^'""]*)\1", new MatchEvaluator(ParserText));
            argument = Regex.Replace(argument, @"[A-Za-z]\w+", new MatchEvaluator(ParserData));
            argument = Regex.Replace(argument, @"(^|[^\d])-(\d*\.?\d+)", new MatchEvaluator(ParserNegativeNumber));
            argument = Regex.Replace(argument, @"(^|[^@])(\d*\.?\d+)", new MatchEvaluator(ParserNumber));
            string newArgument;
            while ((newArgument = Regex.Replace(argument, @"\(([^\(\)]+)\)", new MatchEvaluator(ParserBrackets))) != argument)
            {
                argument = newArgument;
            }
            return ParserExpression(argument);
        }
    }
}
