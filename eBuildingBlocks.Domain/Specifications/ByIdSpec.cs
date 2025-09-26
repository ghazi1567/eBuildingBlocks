using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Specifications
{

    public sealed class ByIdSpec<T> : Specification<T>
    {
        private static readonly string IdPropName = "Id";
        public ByIdSpec() : this(default!) { }
        public ByIdSpec(object idValue) : this(idValue, IdPropName) { }

        public ByIdSpec(object idValue, string idPropertyName, bool NoTracking = true)
        {
            var param = Expression.Parameter(typeof(T), "e");
            var prop = Expression.Property(param, idPropertyName);
            var constExpr = Expression.Constant(idValue);
            var body = Expression.Equal(prop, Expression.Convert(constExpr, prop.Type));
            Criteria = Expression.Lambda<Func<T, bool>>(body, param);
            if (NoTracking)
            {
                WithTracking();
            }
        }
    }
}
