using System;
using System.Linq;

namespace BrightnessSwitch
{
    public class SupportVectorMachine
    {
        public double b = 2.5;
        public double w = -1;
        public double lambda = 1;

        public SupportVectorMachine(double separator = 0, double separationWidth = 1)
        {
            b = separator;
            w = 1 / separationWidth;
        }

        // This procedure optimizes the following cost function:
        // C = lambda / 2 * w**2 + 1 / (N * sum_i weights[i]) * sum_i (weights[i] * max(0, 1 - y_i * w * (x_i - b)))
        public void Train(double[] values, bool[] group, double[]? weights = null, int maxIter = 200, double eps = 1e-4)
        {
            if (values.Length != group.Length || (weights != null && values.Length != weights.Length))
            {
                throw new ArgumentException("The arguments have to be of the same length");
            }
            double normalization = 1.0 / (weights?.Sum() ?? values.Length);

            double w_backup = w;
            double b_backup = b;
            double previousCost = double.MaxValue;
            double initialCost = 0;
            double cost = 0;
            for (uint t = 1; t <= maxIter; t++)
            {
                double eta = 1 / (lambda * (t + 3));
                double w_new = w * (1 - lambda * eta);
                double b_new = b;
                cost = 0;
                for (uint i = 0; i < values.Length; i++)
                {
                    var x = values[i];
                    int y = group[i] ? 1 : -1;
                    var weight = weights != null ? weights[i] : 1;
                    if (y * w * (x - b) < 1)
                    {
                        w_new += eta * normalization * weight * y * (x - b);
                        b_new -= eta * normalization * weight * y * w;
                        cost += normalization * weight * (1 - y * w * (x - b));
                    }
                }
                w = w_new;
                b = b_new;
                if (Math.Abs(cost / previousCost - 1) < eps)
                {
                    break;
                }
                previousCost = cost;
                if (t == 1)
                {
                    initialCost = cost;
                }
            }

            if (initialCost < cost)
            {
                w = w_backup;
                b = b_backup;
            }
        }

        public (bool result, double certainty) Predict(double value)
        {
            var prediction = w * (value - b);
            return (prediction > 0, Math.Abs(prediction));
        }
    }
}