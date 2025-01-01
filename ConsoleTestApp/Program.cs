using JXMath;
using JXMath.Algos;

using static System.Console;
using static JXMath.Globals;



// Using "Fit" directly require good in itial guess
var rv = new RandomVariableSet();
rv.Add_NormalRV(0.3, 1, 1);
rv.Add_NormalRV(0.7, 4, 2);

var data = rv.Sample(10000);

var model = new GaussianMixtureModel([1, 4], [1, 4], [0.3, 0.7]);

model.Debug = true;

model.Fit(data);


WriteLine("\nUsing 'MultipleFits()'");

var model2 = new GaussianMixtureModel([0, 6], [1, 1], [0.5, 0.5]);
model2.Debug = true;
model2.TerminationTolerance = 1e-7;
model2.MultipleFits(data);

