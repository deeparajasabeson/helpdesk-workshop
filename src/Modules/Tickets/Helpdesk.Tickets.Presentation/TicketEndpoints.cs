using Helpdesk.SharedKernel;
using Helpdesk.Tickets.Application.Abstractions;
using Helpdesk.Tickets.Application.Tickets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Helpdesk.Tickets.Presentation;

public sealed record CreateTicketRequest(string Title);
public sealed record CreateTicketResponse(Guid Id);

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        // Slices add endpoints here.
        app.MapPost("/tickets", async (
            CreateTicketRequest request,
            ICommandHandler<CreateTicket, Guid> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CreateTicket(request.Title), ct);
            return result.IsSuccess
                ? Results.Created($"/tickets/{result.Value}", new CreateTicketResponse(result.Value))
                : ToFailure(result.Error);
        });

        app.MapPost("/tickets/{id:guid}/close", async (
            Guid id,
            ICommandHandler<CloseTicket, bool> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CloseTicket(id), ct);
            return result.IsSuccess ? Results.NoContent() : ToFailure(result.Error);
        });

        app.MapGet("/tickets/{id:guid}", async (
            Guid id,
            IQueryHandler<GetTicketById, 
            TicketResponse> handler, 
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetTicketById(id), ct);
            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error.Code == "Tickets.NotFound"
                ? Results.NotFound()
                : Results.BadRequest(new { result.Error.Code, result.Error.Message });
        });

        return app;
    }

    private static IResult ToFailure(Error error) =>
        error.Code.EndsWith("NotFound", StringComparison.Ordinal)
            ? Results.NotFound(error)
            : Results.BadRequest(error);
}
