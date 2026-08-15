using System;
using System.Collections.Generic;

namespace _Project.Scripts.Systems.Performance
{
    public readonly struct PerformanceFrameStatistics
    {
        public readonly int FrameCount;
        public readonly double AverageFps;
        public readonly double MedianFps;
        public readonly double OnePercentLowFps;
        public readonly double MinimumFps;
        public readonly double AverageFrameMilliseconds;
        public readonly double P95FrameMilliseconds;
        public readonly double P99FrameMilliseconds;
        public readonly double MaximumFrameMilliseconds;

        public PerformanceFrameStatistics(
            int frameCount,
            double averageFps,
            double medianFps,
            double onePercentLowFps,
            double minimumFps,
            double averageFrameMilliseconds,
            double p95FrameMilliseconds,
            double p99FrameMilliseconds,
            double maximumFrameMilliseconds)
        {
            FrameCount = frameCount;
            AverageFps = averageFps;
            MedianFps = medianFps;
            OnePercentLowFps = onePercentLowFps;
            MinimumFps = minimumFps;
            AverageFrameMilliseconds = averageFrameMilliseconds;
            P95FrameMilliseconds = p95FrameMilliseconds;
            P99FrameMilliseconds = p99FrameMilliseconds;
            MaximumFrameMilliseconds = maximumFrameMilliseconds;
        }
    }

    public static class PerformanceStatistics
    {
        public static PerformanceFrameStatistics Calculate(IReadOnlyList<float> frameMilliseconds)
        {
            if (frameMilliseconds == null || frameMilliseconds.Count == 0)
            {
                return default;
            }

            float[] sorted = new float[frameMilliseconds.Count];
            double totalMilliseconds = 0d;

            for (int index = 0; index < frameMilliseconds.Count; index++)
            {
                float value = Math.Max(0.0001f, frameMilliseconds[index]);
                sorted[index] = value;
                totalMilliseconds += value;
            }

            Array.Sort(sorted);

            double averageFrameMilliseconds = totalMilliseconds / sorted.Length;
            double medianFrameMilliseconds = Percentile(sorted, 0.50d);
            double p95FrameMilliseconds = Percentile(sorted, 0.95d);
            double p99FrameMilliseconds = Percentile(sorted, 0.99d);
            double maximumFrameMilliseconds = sorted[sorted.Length - 1];

            int slowFrameCount = Math.Max(1, (int)Math.Ceiling(sorted.Length * 0.01d));
            double slowFrameTotal = 0d;
            for (int index = sorted.Length - slowFrameCount; index < sorted.Length; index++)
            {
                slowFrameTotal += sorted[index];
            }

            double slowFrameMean = slowFrameTotal / slowFrameCount;
            return new PerformanceFrameStatistics(
                sorted.Length,
                ToFps(averageFrameMilliseconds),
                ToFps(medianFrameMilliseconds),
                ToFps(slowFrameMean),
                ToFps(maximumFrameMilliseconds),
                averageFrameMilliseconds,
                p95FrameMilliseconds,
                p99FrameMilliseconds,
                maximumFrameMilliseconds);
        }

        private static double Percentile(float[] sorted, double percentile)
        {
            if (sorted.Length == 1)
            {
                return sorted[0];
            }

            double clampedPercentile = Math.Max(0d, Math.Min(1d, percentile));
            double position = (sorted.Length - 1) * clampedPercentile;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            if (lower == upper)
            {
                return sorted[lower];
            }

            double fraction = position - lower;
            return sorted[lower] + ((sorted[upper] - sorted[lower]) * fraction);
        }

        private static double ToFps(double frameMilliseconds)
        {
            return frameMilliseconds > 0d ? 1000d / frameMilliseconds : 0d;
        }
    }
}
