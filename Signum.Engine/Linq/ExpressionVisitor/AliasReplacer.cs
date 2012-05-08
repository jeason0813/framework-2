﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Collections.ObjectModel;

namespace Signum.Engine.Linq
{
    internal class AliasReplacer : DbExpressionVisitor
    {
        Dictionary<Alias, Alias> aliasMap;

        private AliasReplacer() { }

        public static Expression Replace(Expression source)
        {
            AliasReplacer ap = new AliasReplacer()
            {
                aliasMap = AliasGatherer.Gather(source).Reverse().ToDictionary(a => a, a => Alias.CloneAlias(a))
            };

            return ap.Visit(source);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if(aliasMap.ContainsKey(column.Alias))
                return new ColumnExpression(column.Type, aliasMap[column.Alias], column.Name);
            return column;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            if (aliasMap.ContainsKey(table.Alias))
                return new TableExpression(aliasMap[table.Alias], table.Name);
            return table;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);
            ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression> groupBy = this.VisitGroupBy(select.GroupBy);
            Alias newAlias = aliasMap.TryGetC(select.Alias) ?? select.Alias;

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy || newAlias != select.Alias)
                return new SelectExpression(newAlias, select.IsDistinct, select.IsReverse, top, columns, from, where, orderBy, groupBy);

            return select;
        }
    }
}
