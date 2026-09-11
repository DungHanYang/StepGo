using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace StepGo.Shared.Infrastructure.Dynamo;

/// <summary>
/// Thin wrapper around TransactWriteItems that retries only on the hot-item contention case
/// (TransactionConflictException / ProvisionedThroughputExceededException) — see design.md Risks:
/// AGGREGATE#PLATFORM/SUMMARY and TEACHER#.../SUMMARY#... are written by every payment/payout
/// transaction and are expected to occasionally collide under load. Any other failure (e.g. a
/// ConditionalCheckFailed from the idempotency guard) is a real business outcome and is never retried.
/// </summary>
public sealed class DynamoTransactionWriter(IAmazonDynamoDB client)
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(400)];

    public async Task<bool> TryWriteAsync(IReadOnlyList<TransactWriteItem> items, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await client.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = [.. items] }, ct);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
            catch (TransactionCanceledException ex) when (ex.CancellationReasons?.Any(r => r.Code == "ConditionalCheckFailed") == true)
            {
                return false;
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[attempt], ct);
            }
        }
    }

    private static bool IsRetryable(Exception ex) =>
        ex is TransactionConflictException or ProvisionedThroughputExceededException or InternalServerErrorException;
}
