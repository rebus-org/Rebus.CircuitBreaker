using System;

namespace Rebus.Config
{
    /// <summary>
    /// Contains the settings used by <see cref="CircuitBreakerSettings"/>
    /// </summary>
    internal class CircuitBreakerSettings
    {
        /// <summary>
        /// Default Attempts a circuit breaker will fail within a given <see cref="TrackingPeriod"/>
        /// </summary>
        public const int DefaultAttempts = 5;

        /// <summary>
        /// Default period where in errors are getting tracked
        /// </summary>
        public const int DefaultTrackingPeriodInSeconds = 30;

        /// <summary>
        /// Default time Interval for when the circuit breaker will move to Half Open after being in an Open State
        /// </summary>
        public const int DefaultHalfOpenResetInterval = 150;

        /// <summary>
        /// Default time Interval for when the circuit breaker will close after being opened 
        /// </summary>
        public const int DefaultCloseResetInterval = 300;

        /// <summary>
        /// Number of attempts that the circuit breaker will fail within a given <see cref="TrackingPeriod"/>
        /// </summary>
        internal int Attempts { get; private set; }

        /// <summary>
        /// Time window wherein consecutive errors are getting tracked
        /// </summary>
        internal TimeSpan TrackingPeriod { get; private set; }

        /// <summary>
        /// Time Interval for when the circuit breaker will close after being opened
        /// </summary>
        internal TimeSpan CloseResetInterval { get; private set; }

        /// <summary>
        /// Time Interval for when the circuit breaker will move to Half Open after being in an Open State
        /// </summary>
        internal TimeSpan HalfOpenResetInterval { get; private set; }

        /// <summary>
        /// Create a setting for a given circuit breaker
        /// </summary>
        internal CircuitBreakerSettings(int attempts, int trackingPeriodInSeconds, int halfOpenResetIntervalInSeconds, int closedResetIntervalInSeconds)
        {
            Attempts = attempts;
            TrackingPeriod = TimeSpan.FromSeconds(trackingPeriodInSeconds);
            HalfOpenResetInterval = TimeSpan.FromSeconds(halfOpenResetIntervalInSeconds);
            CloseResetInterval = TimeSpan.FromSeconds(closedResetIntervalInSeconds);
        }
    }
}
