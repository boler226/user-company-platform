using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Platform.Aggregator.Models;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Platform.Aggregator.Clients;

public abstract class ServiceClient(HttpClient http, ILogger logger, string service)
{
    protected async Task<ServiceResult<T>> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Calling {Service}", service);
        try
        {
            using var response = await http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("{Service} returned {StatusCode}", service, (int)response.StatusCode);
                return new(null, new(service, $"http_{(int)response.StatusCode}",
                    $"{service} did not return the requested data."));
            }

            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: deadline.Token);
            return data is null
                ? new(null, new(service, "empty_response", $"{service} returned an empty response."))
                : new(data, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
            or TimeoutRejectedException or BrokenCircuitException or JsonException or IOException)
        {
            logger.LogWarning("Call to {Service} failed with {ErrorType}", service, exception.GetType().Name);
            return new(null, new(service, "unavailable", $"{service} is temporarily unavailable or returned invalid data."));
        }
        finally
        {
            logger.LogInformation("Call to {Service} finished in {ElapsedMs} ms", service, timer.ElapsedMilliseconds);
        }
    }
}
