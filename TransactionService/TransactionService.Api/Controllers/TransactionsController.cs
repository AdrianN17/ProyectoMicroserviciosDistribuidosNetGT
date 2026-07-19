using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionService.Api.Mapper;
using TransactionService.Application.Transactions.Queries.GetAllByFromWalletId;
using TransactionService.Api.Schema;

namespace TransactionService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController(IMediator mediator) : ControllerBase
    {
        [HttpPost(Name = "Transaction_Create")]
        [Authorize(Roles = "User-App")]
        public async Task<IActionResult> Create(
            [FromBody] TransactionSchemaRequest schema,
            [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(schema.ToCommand(idempotencyKey ?? Guid.NewGuid()), cancellationToken);

            return result.Match(
                transactionId => Ok(transactionId.ToTransactionIdResponse()),
                errors => ErrorOrHttp.MapToProblem(this, errors)
            );
        }

        [HttpDelete("{transactionId:guid}", Name = "Transaction_Delete")]
        [Authorize(Roles = "Support")]
        public async Task<IActionResult> DeleteById(Guid transactionId, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(transactionId.ToDeleteTransactionCommand(), cancellationToken);

            return result.Match(
                _ => NoContent(),
                errors => ErrorOrHttp.MapToProblem(this, errors)
            );
        }

        [HttpGet("wallet/{fromWalletId:guid}", Name = "Transaction_GetAllByFromWalletId")]
        [Authorize(Roles = "User-App")]
        public async Task<IActionResult> GetAllByFromWalletId(Guid fromWalletId, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetAllByFromWalletIdTransactionQuery(fromWalletId), cancellationToken);

            return result.Match(
                transactions => Ok(transactions.Select(t => t.ToResponse())),
                errors => ErrorOrHttp.MapToProblem(this, errors)
            );
        }
    }
}
