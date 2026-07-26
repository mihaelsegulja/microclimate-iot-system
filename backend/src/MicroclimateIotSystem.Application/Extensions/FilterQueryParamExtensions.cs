using System.Globalization;
using System.Linq.Expressions;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Extensions;

public static class FilterQueryParamExtensions
{
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<FilterRule>? rules)
    {
        if (rules == null || !rules.Any()) return query;

        var param = Expression.Parameter(typeof(T), "x");
        Expression? finalExpression = null;

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Key) || string.IsNullOrWhiteSpace(rule.Value))
                continue;

            var prop = typeof(T).GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, rule.Key, StringComparison.OrdinalIgnoreCase));

            if (prop == null) continue;

            var member = Expression.Property(param, prop);

            object? convertedValue;
            try
            {
                convertedValue = ConvertValue(rule.Value, prop.PropertyType);
            }
            catch
            {
                continue;
            }

            var constant = Expression.Constant(convertedValue, prop.PropertyType);
            var comparison = BuildComparison(member, rule.Op, constant, prop.PropertyType);

            finalExpression = finalExpression == null 
                ? comparison 
                : Expression.AndAlso(finalExpression, comparison);
        }

        if (finalExpression == null) return query;

        var lambda = Expression.Lambda<Func<T, bool>>(finalExpression, param);
        return query.Where(lambda);
    }

    private static Expression BuildComparison(MemberExpression member, FilterOperation op, ConstantExpression constant, Type propType)
    {
        // string
        if (propType == typeof(string))
        {
            return op switch
            {
                FilterOperation.Contains => Expression.Call(member, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, constant),
                FilterOperation.StartsWith => Expression.Call(member, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, constant),
                FilterOperation.EndsWith => Expression.Call(member, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, constant),
                FilterOperation.NotEquals => Expression.NotEqual(member, constant),
                _ => Expression.Equal(member, constant)
            };
        }

        // numeric, bool, datetime
        return op switch
        {
            FilterOperation.NotEquals => Expression.NotEqual(member, constant),
            FilterOperation.GreaterThan => Expression.GreaterThan(member, constant),
            FilterOperation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, constant),
            FilterOperation.LessThan => Expression.LessThan(member, constant),
            FilterOperation.LessThanOrEqual => Expression.LessThanOrEqual(member, constant),
            _ => Expression.Equal(member, constant)
        };
        
    }
    
    private static object? ConvertValue(string rawValue, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(Guid))
            return Guid.Parse(rawValue);

        if (underlyingType.IsEnum)
            return Enum.Parse(underlyingType, rawValue, ignoreCase: true);

        if (underlyingType == typeof(DateTime))
            return DateTime.Parse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

        return Convert.ChangeType(rawValue, underlyingType, CultureInfo.InvariantCulture);
    }
}

