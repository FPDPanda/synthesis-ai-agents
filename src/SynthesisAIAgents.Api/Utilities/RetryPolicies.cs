using Polly;

namespace SynthesisAIAgents.Api.Utilities
{
    public class RetryPolicies
    {
        public static IAsyncPolicy BasicRetry(int retries = 2, int delayMs = 500) =>
            Policy.Handle<Exception>().WaitAndRetryAsync(retries, i => TimeSpan.FromMilliseconds(delayMs));
    }
}