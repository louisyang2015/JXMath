using JXMath;
using JXMath.Algos;

using static System.Console;
using static JXMath.Globals;



// Generate test data
var rv = new RandomVariableSet();
rv.Add_NormalRV(1, 1, likelihood: 0.3);
rv.Add_NormalRV(4, 2, likelihood: 0.7);

var data = rv.Sample(10000);

// Example Part 1
// Using "Fit" directly require good in initial guess
var model = new GaussianMixtureModel([1, 4], [1, 4], [0.3, 0.7]);

model.Debug = true;

model.Fit(data);


// Example Part 2 - using ".MultipleFits()" still require approximate
// initial guess
WriteLine("\nUsing 'MultipleFits()'");

var model2 = new GaussianMixtureModel([0, 6], [1, 1], [0.5, 0.5]);
model2.Debug = true;
model2.TerminationTolerance = 1e-7;
model2.MultipleFits(data);


// Example Part 3 - use histogram to get an estimate first.
// But sometimes 3 components are found (instead of 2).
WriteLine("\nUsing histogram to get an estimate first.");
var histogram = new Histogram(data);
var estimates = histogram.Get_GMM_Estimate();

var model3 = new GaussianMixtureModel(estimates.mean, estimates.variance, estimates.weights);
model3.Debug = true;
model3.TerminationTolerance = 1e-7;
model3.MultipleFits(data);


