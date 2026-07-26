using System;
using Utilities;
using Xunit;

namespace Notes.UnitTests
{
    /// <summary>
    /// Pruebas del rate limiter de ventana fija por partición (la IP del cliente).
    /// No se prueba aquí el reinicio de la ventana al expirar: el limiter lee
    /// DateTime.UtcNow directamente, sin reloj inyectable, así que esa prueba
    /// exigiría una espera real.
    /// </summary>
    public class RateLimiterTests
    {
        [Fact]
        public void Allows_exactly_up_to_the_configured_limit()
        {
            RateLimiter limiter = new RateLimiter(3, 60);

            Assert.True(limiter.IsAllowed("10.0.0.1"));
            Assert.True(limiter.IsAllowed("10.0.0.1"));
            Assert.True(limiter.IsAllowed("10.0.0.1"));
        }

        [Fact]
        public void Blocks_the_request_that_exceeds_the_limit_and_the_ones_after_it()
        {
            RateLimiter limiter = new RateLimiter(3, 60);
            for (int i = 0; i < 3; i++)
            {
                limiter.IsAllowed("10.0.0.1");
            }

            Assert.False(limiter.IsAllowed("10.0.0.1"));
            Assert.False(limiter.IsAllowed("10.0.0.1"));
        }

        [Fact]
        public void Partitions_are_independent()
        {
            RateLimiter limiter = new RateLimiter(1, 60);

            Assert.True(limiter.IsAllowed("10.0.0.1"));
            Assert.False(limiter.IsAllowed("10.0.0.1"));
            // Que una IP agote su cuota no debe afectar a otra.
            Assert.True(limiter.IsAllowed("10.0.0.2"));
        }

        [Fact]
        public void Null_and_empty_partition_keys_share_the_unknown_bucket()
        {
            RateLimiter limiter = new RateLimiter(1, 60);

            Assert.True(limiter.IsAllowed(null));
            Assert.False(limiter.IsAllowed(string.Empty));
        }

        [Fact]
        public void A_non_positive_limit_falls_back_to_one_hundred()
        {
            RateLimiter limiter = new RateLimiter(0, 60);
            for (int i = 0; i < 100; i++)
            {
                Assert.True(limiter.IsAllowed("10.0.0.1"));
            }

            Assert.False(limiter.IsAllowed("10.0.0.1"));
        }

        [Fact]
        public void Window_seconds_are_exposed_and_a_non_positive_window_falls_back_to_sixty()
        {
            Assert.Equal(30, new RateLimiter(10, 30).WindowSeconds);
            Assert.Equal(60, new RateLimiter(10, 0).WindowSeconds);
        }
    }
}
