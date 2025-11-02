using FluentAssertions;
using SynthesisAIAgents.Api.Utilities;

namespace SynthesisAIAgents.Tests.Utilities
{
    public class RetryPoliciesTests
    {
        [Fact]
        public async Task BasicRetry_DefaultRetries_SucceedsAfterRetries()
        {
            // Arrange
            var attempts = 0;
            // Make the delegate fail twice and succeed on the third call.
            async Task<int> Work()
            {
                attempts++;
                await Task.Yield();
                if (attempts < 3) throw new InvalidOperationException("transient");
                return 42;
            }

            var policy = RetryPolicies.BasicRetry(); // default retries = 2

            // Act
            var result = await policy.ExecuteAsync(Work);

            // Assert
            result.Should().Be(42);
            // initial try + 2 retries = 3 attempts
            attempts.Should().Be(3);
        }

        [Fact]
        public async Task BasicRetry_CustomRetries_ThrowsAfterExhaustingRetries()
        {
            // Arrange
            var attempts = 0;
            async Task Work()
            {
                attempts++;
                await Task.Yield();
                throw new InvalidOperationException("always fails");
            }

            var retries = 4;
            var policy = RetryPolicies.BasicRetry(retries: retries, delayMs: 1);

            // Act
            Func<Task> act = async () => await policy.ExecuteAsync(Work);

            // Assert: after exhausting retries the original exception should bubble up
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("always fails");
            // initial try + retries attempts in total
            attempts.Should().Be(retries + 1);
        }

        [Fact]
        public async Task BasicRetry_NoRetryNeeded_CallsOnce()
        {
            // Arrange
            var calls = 0;
            async Task Work()
            {
                calls++;
                await Task.CompletedTask;
            }

            var policy = RetryPolicies.BasicRetry(retries: 3, delayMs: 1);

            // Act
            await policy.ExecuteAsync(Work);

            // Assert
            calls.Should().Be(1);
        }
    }
}