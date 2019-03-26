
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMVVM
{
    internal abstract class BaseExpression
    {
        public abstract object GetValue();
    }

    class InvertExpression : BaseExpression
    {
        public BaseExpression hs;
        public override object GetValue() { return !Convert.ToBoolean(hs.GetValue()); }
    }

    class BinaryOperation : BaseExpression
    {
        public BaseExpression lhs;
        public BaseExpression rhs;
        public override object GetValue()
        {
            return null;
        }
    }

    class OperationEqual : BinaryOperation
    {
        public override object GetValue()
        {
            var l = lhs.GetValue();
            var r = rhs.GetValue();
            if (l is string) return l as string == r.ToString();
            else if (l is bool) return Convert.ToBoolean(l) == Convert.ToBoolean(r);
            else if (l is int || l is float) return Convert.ToSingle(l) == Convert.ToSingle(r);
            else return l == r;
        }
    }
    class OperationNotEqual : OperationEqual { public override object GetValue() { return !(bool)base.GetValue(); } }
    class OperationGreater : BinaryOperation { public override object GetValue() { return Convert.ToSingle(lhs.GetValue()) > Convert.ToSingle(rhs.GetValue()); } }
    class OperationGEqual : BinaryOperation { public override object GetValue() { return Convert.ToSingle(lhs.GetValue()) >= Convert.ToSingle(rhs.GetValue()); } }
    class OperationLess : BinaryOperation { public override object GetValue() { return Convert.ToSingle(lhs.GetValue()) < Convert.ToSingle(rhs.GetValue()); } }
    class OperationLEqual : BinaryOperation { public override object GetValue() { return Convert.ToSingle(lhs.GetValue()) <= Convert.ToSingle(rhs.GetValue()); } }
    class OperationLogicAnd : BinaryOperation { public override object GetValue() { return Convert.ToBoolean(lhs.GetValue()) && Convert.ToBoolean(rhs.GetValue()); } }
    class OperationLogicOr : BinaryOperation { public override object GetValue() { return Convert.ToBoolean(lhs.GetValue()) || Convert.ToBoolean(rhs.GetValue()); } }
    class OperationAdd : BinaryOperation
    {
        public override object GetValue()
        {
            var l = lhs.GetValue();
            var r = rhs.GetValue();
            if (l is string || r is string) return Convert.ToString(l) + Convert.ToString(r);
            return Convert.ToSingle(l) + Convert.ToSingle(r);
        }
    }
    class OperationSub : BinaryOperation { public override object GetValue() { return Convert.ToSingle(lhs.GetValue()) - Convert.ToSingle(rhs.GetValue()); } }
    class OperationMul : BinaryOperation { public override object GetValue() { return Convert.ToSingle(lhs.GetValue()) * Convert.ToSingle(rhs.GetValue()); } }
    class OperationDiv : BinaryOperation { public override object GetValue() { return Convert.ToSingle(lhs.GetValue()) / Convert.ToSingle(rhs.GetValue()); } }

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
            if (value is int || value is float) return Convert.ToSingle(value) != 0 ? lhs.GetValue() : rhs.GetValue();
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
    internal class ArrayExpression : BaseExpression
    {
        private object[] values;
        private BaseExpression[] list;
        public ArrayExpression(BaseExpression[] list)
        {
            this.list = list;
            values = new object[list.Length];
        }
        public int Length { get { return list.Length; } }
        public override object GetValue()
        {
            var count = list.Length;
            for (var i = 0; i < count; ++i)
                values[i] = list[i].GetValue();
            return values;
        }
    }

    internal class ParserArgument
    {
        private List<BaseExpression> expressions = new List<BaseExpression>();
        public Dictionary<string, DataExpression> dataExpressions = new Dictionary<string, DataExpression>();
        public ArrayExpression rootExpression = null;

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
            var ret = ParserExpression(m.Groups[1].Value);
            return "@" + expressions.IndexOf(ret);
        }
        private ArrayExpression ParserArray(string argument)
        {
            var mc = Regex.Matches(argument, @"([^,]+)");
            var list = new BaseExpression[mc.Count];
            var i = 0;
            foreach (Match m in mc)
            {
                list[i++] = ParserExpression(m.Value);
            }
            return new ArrayExpression(list);
        }

        private BaseExpression ParserExpression(string argument)
        {
            Match m;

            // !
            m = Regex.Match(argument, @"!\s*@(\d+)");
            if (m.Success)
            {
                var inv = new InvertExpression();
                inv.hs = expressions[Convert.ToInt32(m.Groups[1].Value)];
                expressions.Add(inv);
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            BinaryOperation binaryOp;
            // * /
            m = Regex.Match(argument, @"@(\d+)\s*([\*/])\s*@(\d+)");
            if (m.Success)
            {
                if (m.Groups[2].Value == "*") binaryOp = new OperationMul(); else binaryOp = new OperationDiv();
                binaryOp.lhs = expressions[Convert.ToInt32(m.Groups[1].Value)];
                binaryOp.rhs = expressions[Convert.ToInt32(m.Groups[3].Value)];
                expressions.Add(binaryOp);
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            // + -
            m = Regex.Match(argument, @"@(\d+)\s*([\+-])\s*@(\d+)");
            if (m.Success)
            {
                if (m.Groups[2].Value == "+") binaryOp = new OperationAdd(); else binaryOp = new OperationSub();
                binaryOp.lhs = expressions[Convert.ToInt32(m.Groups[1].Value)];
                binaryOp.rhs = expressions[Convert.ToInt32(m.Groups[3].Value)];
                expressions.Add(binaryOp);
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            // > >= < <= == !=
            m = Regex.Match(argument, @"@(\d+)\s*([><=!]=?)\s*@(\d+)");
            if (m.Success)
            {
                switch (m.Groups[2].Value)
                {
                    case ">": binaryOp = new OperationGreater(); break;
                    case ">=": binaryOp = new OperationGEqual(); break;
                    case "<": binaryOp = new OperationLess(); break;
                    case "<=": binaryOp = new OperationLEqual(); break;
                    case "!=": binaryOp = new OperationNotEqual(); break;
                    default: binaryOp = new OperationEqual(); break;
                }
                binaryOp.lhs = expressions[Convert.ToInt32(m.Groups[1].Value)];
                binaryOp.rhs = expressions[Convert.ToInt32(m.Groups[3].Value)];
                expressions.Add(binaryOp);
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            // && ||
            m = Regex.Match(argument, @"@(\d+)\s*(&{2}|\|{2})\s*@(\d+)");
            if (m.Success)
            {
                if (m.Groups[2].Value == "&&") binaryOp = new OperationLogicAnd();
                else binaryOp = new OperationLogicOr();
                binaryOp.lhs = expressions[Convert.ToInt32(m.Groups[1].Value)];
                binaryOp.rhs = expressions[Convert.ToInt32(m.Groups[3].Value)];
                expressions.Add(binaryOp);
                return ParserExpression(argument.Replace(m.Groups[0].Value, "@" + (expressions.Count - 1)));
            }

            // a?b:c
            m = Regex.Match(argument, @"@(\d+)\s*\?\s*@(\d+)\s*:\s*@(\d+)");
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
        public void Parser(string argument)
        {
            if (argument == null || argument.Length == 0) return;
            argument = Regex.Replace(argument, @"(['""])([^'""]*)\1", new MatchEvaluator(ParserText));
            argument = Regex.Replace(argument, @"[A-Za-z]\w+", new MatchEvaluator(ParserData));
            argument = Regex.Replace(argument, @"(^|[^\d])-(\d*\.?\d+)", new MatchEvaluator(ParserNegativeNumber));
            argument = Regex.Replace(argument, @"(^|[^@])(\d*\.?\d+)", new MatchEvaluator(ParserNumber));
            string newArgument;
            while ((newArgument = Regex.Replace(argument, @"\(([^\(\)]+)\)", new MatchEvaluator(ParserBrackets))) != argument)
            {
                argument = newArgument;
            }
            rootExpression = ParserArray(argument);
        }
    }
}
