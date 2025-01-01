using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static System.Math;


namespace JXMath.Algos
{
    public class GaussianMixtureModel
    {
        // Inputs:
        public double[] Mean;
        public double[] Variance;
        public double[] Weight;

        // Training parameters:
        public int MaxIterations = 100;

        /// <summary>
        /// Stops the algorithm when the percent change in log likelihood is less 
        /// than this value.
        /// </summary>
        public double TerminationTolerance = 1e-5;

        public bool Debug = false;

        // Outputs:

        /// <summary>
        /// Log likelihood of the model.
        /// </summary>
        public double Likelihood = 0;


        /// <summary>
        /// Sets the initial mean, variance and weight
        /// </summary>
        public GaussianMixtureModel(double[] mean, double[] variance, double[] weight)
        {
            Mean = mean;
            Variance = variance;
            Weight = weight;
        }


        /// <summary>
        /// Optimimzes (mean, variance, and weight) to fit the "data" using 
        /// the EM algorithm.
        /// </summary>
        public void Fit(double[] data)
        {
            _total_p = new double[data.Length];
            _gamma = new double[data.Length];

            _A = new double[Mean.Length];
            _B = new double[Mean.Length];

            PrintParams();

            Refresh_AB();

            Likelihood = LogLikelihood(data);
            Print($"Initial log likelihood: {Likelihood}");

            double old_likelihood = Likelihood;

            for (int i = 0; i < MaxIterations; i++)
            {
                // Compute total probability
                Refresh_TotalProbability(data);

                for (int k = 0; k < Mean.Length; k++)
                {
                    // Compute "_gamma" from existing mean, variance, and weight
                    Refresh_Gamma(data, k);

                    // Compute new mean, variance, and weight
                    Refresh_Model(data, k);
                }

                Likelihood = LogLikelihood(data);
                Print($"Iteration {i + 1}: log likelihood = {Likelihood}");

                if (Likelihood < old_likelihood)
                    break;

                if (Abs((Likelihood - old_likelihood) / old_likelihood) < TerminationTolerance)
                    break;

                // Prepare for next iteration
                old_likelihood = Likelihood;
                Refresh_AB();
            }

            PrintParams();
        }


        /// <summary>
        /// Attempt multiple fits using different initial values.
        /// </summary>
        public void MultipleFits(double[] data)
        {
            PrintParams();

            var candidates = new Candidate[1];
            candidates[0] = new Candidate()
            {
                InitialMean = Mean,
                InitialVariance = Variance,
                InitialWeight = Weight
            };

            for (int i = 0; i < MaxIterations; i++)
            {
                // Run "Fit" in parallel
                Parallel.For(0, candidates.Length, 
                    j => Fit(candidates, data, j));

                var best_candidate = FindBestCandidate(candidates);

                // Exit if the log likelihood is not improving
                if (i > 0)
                {
                    if (Abs((best_candidate.LogLikelihood - Likelihood) / Likelihood) < TerminationTolerance)
                        break;
                }

                // Update the model
                Mean = best_candidate.FinalMean;
                Variance = best_candidate.FinalVariance;
                Weight = best_candidate.FinalWeight;
                Likelihood = best_candidate.LogLikelihood;

                Print("Iteration " + (i + 1) + ": log likelihood = " + Likelihood);
                PrintParams();
                Print("\n");

                // Prepare for next iteration
                candidates = best_candidate.GetMoreCandidates();

                if (candidates.Length == 0)
                    break;
            }
        }


        #region EM Algo

        double[] _total_p = []; // total probability for all data points
        double[] _gamma = []; // contribution vector for category "k"

        // Gaussian probability density function computation temporary variables:
        // f(x) = A * exp(B(x - mean)^2).
        // Each category has its own "A" and "B".
        double[] _A = [];
        double[] _B = [];



        /// <summary>
        /// Compute the Gaussian probability density function using
        /// A * exp(B(x - mean)^2). Each category has its own "A", "B", and "mean".
        /// </summary>
        double Gaussian(double data, int category)
        {
            double A = _A[category];
            double B = _B[category];
            double mean = Mean[category];

            return A * Exp(B * Pow(data - mean, 2));
        }


        /// <summary>
        /// Determine the total log likelihood for the given "data".
        /// </summary>
        double LogLikelihood(double[] data)
        {
            double total = 0; // total likelihood

            for (int i = 0; i < data.Length; i++)
            {
                double likelihood = 0;
                for (int k = 0; k < Mean.Length; k++)
                    likelihood += Weight[k] * Gaussian(data[i], k);

                total += Log(likelihood);
            }

            return total;
        }


        void Print(string s)
        {
            if (Debug)
                Console.WriteLine(s);
        }


        void PrintParams()
        {
            Print("Mean: " + string.Join(", ", Mean));
            Print("Variance: " + string.Join(", ", Variance));
            Print("Weight: " + string.Join(", ", Weight));
        }


        /// <summary>
        /// Refresh "_A" and "_B" for all categories.
        /// </summary>
        void Refresh_AB()
        {
            for (int k = 0; k < Variance.Length; k++)
            {
                _A[k] = 1 / Sqrt(2 * PI * Variance[k]);
                _B[k] = -1 / (2 * Variance[k]);
            }
        }


        /// <summary>
        /// Refresh "_gamma" for the given data and category.
        /// </summary>
        void Refresh_Gamma(double[] data, int category)
        {
            for (int i = 0; i < data.Length; i++)
                _gamma[i] = Weight[category] * Gaussian(data[i], category) / _total_p[i];
        }


        /// <summary>
        /// Refresh mean, variance, and weight, for a particular category,
        /// using "_gamma" and "data".
        /// </summary>
        void Refresh_Model(double[] data, int category)
        {
            double N = _gamma.Sum();

            // Compute new mean
            double total = 0;

            for (int i = 0; i < data.Length; i++)
                total += _gamma[i] * data[i];

            Mean[category] = total / N;

            // Compute new variance
            total = 0;

            for (int i = 0; i < data.Length; i++)
                total += _gamma[i] * Pow(data[i] - Mean[category], 2);

            Variance[category] = total / N;

            // Compute new weight
            Weight[category] = N / data.Length;
        }


        /// <summary>
        /// Compute "_total_p" for "data" using current "mean", "variance", and "weight"
        /// </summary>
        void Refresh_TotalProbability(double[] data)
        {
            // Clear "_total_p"
            for (int i = 0; i < data.Length; i++)
                _total_p[i] = 0;

            for (int i = 0; i < data.Length; i++)
                for (int k = 0; k < Mean.Length; k++)
                {
                    _total_p[i] += Weight[k] * Gaussian(data[i], k);
                }
        }

        #endregion


        #region High level search

        class Candidate
        {
            // Search input:
            public required double[] InitialMean;
            public required double[] InitialVariance;
            public required double[] InitialWeight;

            // Search output:
            public double LogLikelihood;

            public double[] FinalMean = [];
            public double[] FinalVariance = [];
            public double[] FinalWeight = [];


            public Candidate CreateNew_WithFinal()
            {
                var c = new Candidate()
                {
                    InitialMean = (double[])FinalMean.Clone(),
                    InitialVariance = (double[])FinalVariance.Clone(),
                    InitialWeight = (double[])FinalWeight.Clone()
                };

                return c;
            }


            /// <summary>
            /// Generate more candidates by comparing the initial and final mean and variance
            /// values.
            /// </summary>
            public Candidate[] GetMoreCandidates()
            {
                var candidates = new List<Candidate>();

                // Create candidates based on "FinalMean" changes
                if (FinalMean.Length == InitialMean.Length)
                {
                    for (int i = 0; i < FinalMean.Length; i++)
                    {
                        double delta = Abs(FinalMean[i] - InitialMean[i]) * 1.2;

                        if (delta > 0)
                        {
                            var c = CreateNew_WithFinal();
                            c.InitialMean[i] += delta;
                            candidates.Add(c);

                            c = CreateNew_WithFinal();
                            c.InitialMean[i] -= delta;
                            candidates.Add(c);
                        }
                    }
                }

                // Create candidates based on "FinalVariance" changes
                if (FinalVariance.Length == InitialVariance.Length)
                {
                    for (int i = 0; i < FinalVariance.Length; i++)
                    {
                        if (FinalVariance[i] != InitialVariance[i])
                        {
                            double delta = Abs(FinalVariance[i] / InitialVariance[i]) * 1.2;

                            var c = CreateNew_WithFinal();
                            c.InitialVariance[i] *= delta;
                            candidates.Add(c);

                            c = CreateNew_WithFinal();
                            c.InitialVariance[i] /= delta;
                            candidates.Add(c);
                        }
                    }
                }

                return candidates.ToArray();
            }
        }


        /// <summary>
        /// Fit the model using the given "candidates[index]" and "data".
        /// </summary>
        void Fit(Candidate[] candidates, double[] data, int index)
        {
            var c = candidates[index];

            var model = new GaussianMixtureModel(
                (double[])c.InitialMean.Clone(),
                (double[])c.InitialVariance.Clone(),
                (double[])c.InitialWeight.Clone())
            {
                MaxIterations = MaxIterations,
                TerminationTolerance = TerminationTolerance
            };

            model.Fit(data);

            c.FinalMean = model.Mean;
            c.FinalVariance = model.Variance;
            c.FinalWeight = model.Weight;
            c.LogLikelihood = model.Likelihood;
        }


        /// <summary>
        /// Returns the candidate with the highest log likelihood.
        /// </summary>
        Candidate FindBestCandidate(Candidate[] candidates)
        {
            double best_likelihood = candidates[0].LogLikelihood;
            int best_index = 0;

            for (int i = 1; i < candidates.Length; i++)
            {
                if (candidates[i].LogLikelihood > best_likelihood)
                {
                    best_likelihood = candidates[i].LogLikelihood;
                    best_index = i;
                }
            }

            return candidates[best_index];
        }

        #endregion

    }
}
