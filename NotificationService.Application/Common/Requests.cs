using MediatR;

namespace NotificationService.Application.Common;

public interface ICommand : IRequest
{
}

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize);

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct);
}
