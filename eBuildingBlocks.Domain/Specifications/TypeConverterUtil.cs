using System.ComponentModel;
using System.Globalization;

namespace eBuildingBlocks.Domain.Specifications
{
    public static class TypeConverterUtil
    {
        public static object? ChangeType(object? value, Type targetType)
        {
            if (value is null) return null;

            var isNullable = Nullable.GetUnderlyingType(targetType) is Type u ? (targetType = u) != null : false;

            if (targetType.IsEnum)
            {
                if (value is string s) return Enum.Parse(targetType, s, ignoreCase: true);
                return Enum.ToObject(targetType, value);
            }

            if (targetType == typeof(Guid))
                return value is Guid g ? g : Guid.Parse(value.ToString()!);

            if (targetType == typeof(DateTimeOffset))
                return value is DateTimeOffset dto ? dto : DateTimeOffset.Parse(value.ToString()!, CultureInfo.InvariantCulture);

            if (targetType == typeof(DateTime))
                return value is DateTime dt ? dt : DateTime.Parse(value.ToString()!, CultureInfo.InvariantCulture);

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(value.GetType()))
                return converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }

}
