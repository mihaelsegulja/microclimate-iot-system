namespace MicroclimateIotSystem.Application.Models;

public record PagingQueryParams(int Page = 1, int PageSize = 10);

public record LookupPagingQueryParams(int Page = 1, int PageSize = 5);

public record FilterQueryParams(List<FilterRule> FilterRules);

public enum FilterOperation
{
    Equals,
    NotEquals,
    Contains,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

public record FilterRule(
    string Key,
    FilterOperation Op,
    string? Value
);