﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    internal class GroupEntityCleaner : DbExpressionVisitor
    {
        public static Expression Clean(Expression source)
        {
            GroupEntityCleaner pc = new GroupEntityCleaner();
            return pc.Visit(source);
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null;

            if (exp.Type == typeof(Type))
                return VisitType(exp);
            else
                return base.Visit(exp);
        }

        protected override Expression VisitTypeFieldInit(TypeFieldInitExpression typeFie)
        {
            return base.VisitTypeFieldInit(typeFie);
        }

        private Expression VisitType(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
                return exp;

            return new TypeImplementedByAllExpression(QueryBinder.ExtractTypeId(exp));
        }

        protected override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newId = Visit(lite.Id);
            var newTypeId = Visit(lite.TypeId);
            var reference = Visit(lite.Reference);
            var toStr = Visit(lite.ToStr);
            return new LiteReferenceExpression(lite.Type, reference, newId, toStr, newTypeId);
        }

        protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
        {
            Expression newID = Visit(fieldInit.ExternalId);

            return new FieldInitExpression(fieldInit.Type, null, newID, fieldInit.Token); // eliminamos los bindings
        }

        protected override Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            var implementations = reference.Implementations
                .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            return new ImplementedByExpression(reference.Type, implementations);
        }

        protected override Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            var id = (ColumnExpression)Visit(reference.Id);
            var typeId = (TypeImplementedByAllExpression)Visit(reference.TypeId);

            return new ImplementedByAllExpression(reference.Type, id, typeId, reference.Token);
        }
    }
}
