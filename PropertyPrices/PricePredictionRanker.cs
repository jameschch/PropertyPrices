using SharpLearning.Containers.Matrices;
using SharpLearning.FeatureTransformations.MatrixTransforms;
using SharpLearning.InputOutput.Csv;
using SharpLearning.Metrics.Regression;
using SharpLearning.Neural;
using SharpLearning.Neural.Activations;
using SharpLearning.Neural.Layers;
using SharpLearning.Neural.Learners;
using SharpLearning.Neural.Loss;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpLearning.Containers.Extensions;
using Newtonsoft.Json;
using SharpLearning.Common.Interfaces;
using SharpLearning.RandomForest.Learners;
using SharpLearning.Neural.Optimizers;
using SharpLearning.GradientBoost.Learners;
using SharpLearning.AdaBoost.Learners;

namespace PropertyPrices
{
    class PricePredictionRanker
    {

        static string[] excludeColumns = { "Date", "RegionName", "AreaCode" };

        static string[] London = { "Barking and Dagenham", "Barnet", "Bexley", "Brent", "Bromley", "Camden", "Croydon", "Ealing", "Enfield", "Greenwich", "Hackney", "Hammersmith and Fulham", "Haringey", "Harrow", "Havering", "Hillingdon", "Hounslow", "Islington", "Kensington and Chelsea", "Kingston upon Thames", "Lambeth", "Lewisham", "Merton", "Newham", "Redbridge", "Richmond upon Thames", "Southwark", "Sutton", "Tower Hamlets", "Waltham Forest", "Wandsworth", "Westminster" };

        double _totalError = 0;

        string _targetName = "Flat12m%Change";

        int targetOffset = 2;

        public void Predict()
        {

            var header = File.ReadLines("UK-HPI-full-file-2019-07.csv").First();
            var columnNames = header.Split(",");

            // Use StreamReader(filepath) when running from filesystem
            var parser = new CsvParser(() => new StringReader(File.ReadAllText("UK-HPI-full-file-2019-07.csv")), ',', false, true);

            // read feature matrix
            var features = parser.EnumerateRows().ToArray();
            // read classification targets
            var targets = parser.EnumerateRows(_targetName).ToArray();

            string previous = null;
            Dictionary<string, (F64Matrix, double[])> data = new Dictionary<string, (F64Matrix, double[])>();

            List<double[]> regionFeatures = null;
            List<double> regionTargets = null;
            var isFirst = true;
            for (int i = 0; i < features.Length; i++)
            {
                var item = features[i];
                var key = item.GetValue("RegionName");
                if (key != previous)
                {
                    if (regionFeatures?.Any() ?? false)
                    {
                        if (regionTargets.Any(a => a != -1))
                        {
                            data.Add(previous, (regionFeatures.ToF64Matrix(), regionTargets.ToArray()));
                        }
                    }
                    regionFeatures = new List<double[]>();
                    regionTargets = new List<double>();

                    //if (isFirst)
                    //{
                    //    isFirst = false;
                    //}
                    //else
                    //{
                    //    break;
                    //}
                }

                regionFeatures.Add(item.GetValues(columnNames.Except(excludeColumns).ToArray()).Select(s => string.IsNullOrEmpty(s) ? -1d : double.Parse(s)).ToArray());

                //last target is future
                if (i > targetOffset && features.Length - targetOffset - i <= 0)
                {
                    regionTargets.Add(-1);
                }
                //target is next observation
                else if (features[i + targetOffset].GetValue("RegionName") == key)
                {
                    var value = targets.Skip(i).Take(targetOffset).ToArray().Where(w => !string.IsNullOrEmpty(w.GetValue(_targetName))).Select(s => s.GetValue(_targetName)).ToArray();
                    regionTargets.Add(!value.Any() ? 0d : value.Average(a => double.Parse(a)));
                }

                previous = key;
            }

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            //File.WriteAllText("property_data.json", json);

            var itemCount = 0;
            foreach (var item in data)
            {
                // and shift each feature to have a mean value of zero.
                var meanZeroTransformer = new MeanZeroFeatureTransformer();

                F64Matrix transformed = meanZeroTransformer.Transform(item.Value.Item1);
                //F64Matrix transformed = item.Value.Item1;

                var numberOfFeatures = transformed.ColumnCount;

                //var splitter = new StratifiedTrainingTestIndexSplitter<double>(trainingPercentage: 0.8, seed: 24);
                //var split = splitter.SplitSet(transformed, item.Value.Item2);

                //var learner = GetRandomForest();

                //var learner = GetNeuralnet(numberOfFeatures);

                var learner = GetAda();

                var allFeaturesExceptLast = (F64Matrix)transformed.Rows(Enumerable.Range(0, transformed.RowCount - targetOffset - 1).ToArray());
                var allTargetsExceptLast = item.Value.Item2.Take(transformed.RowCount - targetOffset - 1).ToArray();
                var model = learner.Learn(allFeaturesExceptLast, allTargetsExceptLast);

                var prediction = model.Predict(transformed.Row(transformed.RowCount - 1));
                //learner = null;
                //model = null;
                var before = item.Value.Item2[transformed.RowCount - targetOffset - 1];
                var change = Math.Round(prediction / before, 2);

                var allPrediction = model.Predict(allFeaturesExceptLast);
                var metric = new MeanSquaredErrorRegressionMetric();

                var error = metric.Error(allTargetsExceptLast, allPrediction);
                _totalError += error;
                itemCount++;
                var isLondon = London.Contains(item.Key);

                var message = $"TotalError: {(int)(_totalError / itemCount)}, Region: {item.Key}, London: {isLondon}, Error: {error}, Next: {prediction}, Change: {change}";

                Program.Logger.Info(message);

            }
            Console.ReadKey();
        }

        private ILearner<double> GetNeuralnet(int numberOfFeatures)
        {
            // define the neural net.
            var net = new NeuralNet();
            net.Add(new InputLayer(inputUnits: numberOfFeatures));
            net.Add(new DenseLayer(numberOfFeatures, Activation.Relu));
            net.Add(new SquaredErrorRegressionLayer());

            // using square error as error metric. This is only used for reporting progress.
            var learner = new RegressionNeuralNetLearner(net, learningRate: 0.001, iterations: 2000, loss: new SquareLoss(), batchSize: 180, optimizerMethod: OptimizerMethod.Adam);

            return learner;
        }

        private ILearner<double> GetRandomForest()
        {

            var learner = new RegressionRandomForestLearner(trees: 2000, maximumTreeDepth: 100, featuresPrSplit: 0, seed: 42);
            return learner;
        }
        private ILearner<double> GetBoost()
        {
            return new RegressionSquareLossGradientBoostLearner(iterations: 3000,/* learningRate: 0.028,*/ maximumTreeDepth: 2000/*, subSampleRatio: 0.559, featuresPrSplit: 10, runParallel: false*/);
        }

        private ILearner<double> GetAda()
        {
            return new RegressionAdaBoostLearner(maximumTreeDepth: 1000, iterations: 500, learningRate: 0.01);
        }

    }
}
