using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Pipeline.Filters;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.MlRecommendations.Application.Services;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Queries;
using DataBit.StockIA.Platform.MlRecommendations.Interfaces.REST.Transform;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.MlRecommendations.Interfaces.REST;

/// <summary>
///     Recommendations generated from the demand forecast. Administrator only. Purchase recommendations need a plan
///     that includes them (Starter and up); menu recommendations only Professional.
/// </summary>
[Authorize]
[RequireRole("ADMIN")]
[RequireFeature(PlanFeature.PurchaseRecommendations)]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Purchase and menu recommendations (Administrator)")]
public class RecommendationsController(
    IRecommendationCommandService recommendationCommandService,
    IRecommendationQueryService recommendationQueryService,
    ISubscriptionContextFacade subscriptionContextFacade,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List the recommendations (newest first); menu ones only when the plan includes them")]
    [EndpointName("GetRecommendations")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var recommendations = await recommendationQueryService.Handle(new GetRecommendationsByRestaurantIdQuery(currentUser.CurrentRestaurantId!.Value),
            cancellationToken);
        var menuAllowed = (await subscriptionContextFacade.CheckFeature(currentUser.CurrentRestaurantId!.Value, PlanFeature.MenuRecommendations,
            cancellationToken)).Allowed;
        return Ok(recommendations.Where(item => menuAllowed || item.Type != RecommendationType.MenuAdjustment)
            .Select(item => RecommendationResourceFromEntityAssembler.ToResourceFromEntity(item)));
    }

    [HttpPost("{id:int}/apply")]
    [EndpointSummary("Mark a recommendation as applied")]
    [EndpointName("ApplyRecommendation")]
    public async Task<IActionResult> Apply([FromRoute] int id, CancellationToken cancellationToken)
    {
        var existing = (await recommendationQueryService.Handle(new GetRecommendationsByRestaurantIdQuery(currentUser.CurrentRestaurantId!.Value),
            cancellationToken)).FirstOrDefault(item => item.Id == id);
        if (existing?.Type == RecommendationType.MenuAdjustment)
        {
            var check = await subscriptionContextFacade.CheckFeature(currentUser.CurrentRestaurantId!.Value, PlanFeature.MenuRecommendations,
                cancellationToken);
            if (!check.Allowed)
                return problemDetailsFactory.ToActionResult(StatusCodes.Status402PaymentRequired, check.Error!.Value.ToString(), check.Message,
                    check.Details);
        }

        var result = await recommendationCommandService.Handle(new ApplyRecommendationCommand(id, currentUser.CurrentRestaurantId!.Value),
            cancellationToken);
        return MlRecommendationsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            recommendation => Ok(RecommendationResourceFromEntityAssembler.ToResourceFromEntity(recommendation)));
    }
}
