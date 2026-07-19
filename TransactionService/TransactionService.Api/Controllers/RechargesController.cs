using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionService.Api.Mapper;
using TransactionService.Application.Recharge.Queries.GetAllByWalletId;
using TransactionService.Api.Schema;

namespace TransactionService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RechargesController(IMediator mediator) : ControllerBase
    {
        [HttpPost(Name = "Recharge_Create")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Create(
            [FromBody] RechargeSchemaRequest schema,
            [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(schema.ToCommand(idempotencyKey ?? Guid.NewGuid()), cancellationToken);

            return result.Match(
                rechargeId => Ok(rechargeId.ToRechargeIdResponse()),
                errors => ErrorOrHttp.MapToProblem(this, errors)
            );
        }

        [HttpDelete("{rechargeId:guid}", Name = "Recharge_Delete")]
        [Authorize(Roles = "Support")]
        public async Task<IActionResult> DeleteById(Guid rechargeId, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(rechargeId.ToDeleteRechargeCommand(), cancellationToken);

            return result.Match(
                _ => NoContent(),
                errors => ErrorOrHttp.MapToProblem(this, errors)
            );
        }

        [HttpGet("wallet/{walletId:guid}", Name = "Recharge_GetAllByWalletId")]
        [Authorize(Roles = "Seller,User-App")]
        public async Task<IActionResult> GetAllByWalletId(Guid walletId, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetAllByWalletIdRechargeQuery(walletId), cancellationToken);

            return result.Match(
                recharges => Ok(recharges.Select(r => r.ToResponse())),
                errors => ErrorOrHttp.MapToProblem(this, errors)
            );
        }
    }
}
