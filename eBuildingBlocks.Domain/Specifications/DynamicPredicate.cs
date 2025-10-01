using System.Linq.Expressions;
using System.Reflection;
namespace eBuildingBlocks.Domain.Specifications
{
    public static class DynamicPredicate
    {
        public static Expression<Func<T, bool>> Build<T>(
            string propertyPath,
            ComparisonOperator op,
            object? rawValue)
        {
            var param = Expression.Parameter(typeof(T), "e");

            // e.Property or e.Nested.Property
            Expression member = param;
            foreach (var segment in propertyPath.Split('.'))
            {
                member = Expression.PropertyOrField(member, segment);
            }

            var memberType = ((member as MemberExpression)?.Member as PropertyInfo)?.PropertyType
                             ?? member.Type;

            Expression body;

            if (op == ComparisonOperator.In)
            {
                // Expect rawValue to be IEnumerable of something convertible to memberType
                if (rawValue is not System.Collections.IEnumerable seq || rawValue is string)
                    throw new ArgumentException("For 'In' operator, value must be a non-string IEnumerable.");

                var listType = typeof(List<>).MakeGenericType(memberType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

                foreach (var v in seq)
                    list.Add(TypeConverterUtil.ChangeType(v, memberType));

                var listConst = Expression.Constant(list);
                var containsMethod = listType.GetMethod("Contains", new[] { memberType })!;
                body = Expression.Call(listConst, containsMethod, ToNonNullable(member));
                return Expression.Lambda<Func<T, bool>>(body, param);
            }

            // Regular binary/string ops
            var convertedValue = TypeConverterUtil.ChangeType(rawValue, Nullable.GetUnderlyingType(memberType) ?? memberType);
            var constant = Expression.Constant(convertedValue, Nullable.GetUnderlyingType(memberType) ?? memberType);

            var left = ToNonNullable(member);
            var right = constant;

            body = op switch
            {
                ComparisonOperator.Equal => Expression.Equal(left, right),
                ComparisonOperator.NotEqual => Expression.NotEqual(left, right),
                ComparisonOperator.GreaterThan => Expression.GreaterThan(left, right),
                ComparisonOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
                ComparisonOperator.LessThan => Expression.LessThan(left, right),
                ComparisonOperator.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
                ComparisonOperator.Contains => CallStringMethod(left, right, nameof(string.Contains)),
                ComparisonOperator.StartsWith => CallStringMethod(left, right, nameof(string.StartsWith)),
                ComparisonOperator.EndsWith => CallStringMethod(left, right, nameof(string.EndsWith)),
                _ => throw new NotSupportedException($"Operator {op} not supported.")
            };

            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        private static Expression ToNonNullable(Expression expr)
        {
            var t = expr.Type;
            var u = Nullable.GetUnderlyingType(t);
            if (u is null) return expr;
            return Expression.Convert(expr, u);
        }

        private static Expression CallStringMethod(Expression left, Expression right, string methodName)
        {
            // Convert operands to string
            if (left.Type != typeof(string)) left = Expression.Call(left, "ToString", Type.EmptyTypes);
            if (right.Type != typeof(string)) right = Expression.Call(right, "ToString", Type.EmptyTypes);

            var method = typeof(string).GetMethod(methodName, new[] { typeof(string) })!;
            return Expression.Call(left, method, right);
        }
    }

}
