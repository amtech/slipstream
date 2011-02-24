﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.SqlTree
{
    public class ExpressionOperator : Node
    {
        private static readonly ExpressionOperator s_orOpr = new ExpressionOperator("or");
        private static readonly ExpressionOperator s_andOpr = new ExpressionOperator("and");
        private static readonly ExpressionOperator s_notOpr = new ExpressionOperator("not");
        private static readonly ExpressionOperator s_equalOpr = new ExpressionOperator("=");
        private static readonly ExpressionOperator s_notEqualOpr = new ExpressionOperator("<>");
        private static readonly ExpressionOperator s_greaterOpr = new ExpressionOperator(">");
        private static readonly ExpressionOperator s_greaterEqualOpr = new ExpressionOperator(">=");
        private static readonly ExpressionOperator s_lessOpr = new ExpressionOperator("<");
        private static readonly ExpressionOperator s_lessEqualOpr = new ExpressionOperator("<=");
        private static readonly ExpressionOperator s_likeOpr = new ExpressionOperator("like");
        private static readonly ExpressionOperator s_notLikeOpr = new ExpressionOperator("not like");


        public ExpressionOperator(string opr)
        {
            this.Operator = opr;
        }

        public string Operator { get; private set; }

        public static ExpressionOperator OrOperator { get { return s_orOpr; } }
        public static ExpressionOperator AndOperator { get { return s_andOpr; } }
        public static ExpressionOperator NotOperator { get { return s_notOpr; } }
        public static ExpressionOperator EqualOperator { get { return s_equalOpr; } }
        public static ExpressionOperator NotEqualOperator { get { return s_notEqualOpr; } }

        public override void Traverse(IVisitor visitor)
        {
            visitor.VisitBefore(this);
            visitor.VisitOn(this);
            visitor.VisitAfter(this);
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}