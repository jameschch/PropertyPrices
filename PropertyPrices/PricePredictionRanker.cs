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
using SharpLearning.CrossValidation.TimeSeries;
using System.Globalization;

namespace PropertyPrices
{
    class PricePredictionRanker
    {

        static string[] excludeColumns = { "Date", "RegionName", "AreaCode" };

        static string[] London = { "Barking and Dagenham", "Barnet", "Bexley", "Brent", "Bromley", "Camden", "Croydon", "Ealing", "Enfield", "Greenwich", "Hackney", "Hammersmith and Fulham", "Haringey", "Harrow", "Havering", "Hillingdon", "Hounslow", "Islington", "Kensington and Chelsea", "Kingston upon Thames", "Lambeth", "Lewisham", "Merton", "Newham", "Redbridge", "Richmond upon Thames", "Southwark", "Sutton", "Tower Hamlets", "Waltham Forest", "Wandsworth", "Westminster" };

        double _totalError = 0;
        double _totalCrossError = 0;
        string _targetName = "Flat12m%Change";
        const int _targetOffset = 3;

        public void Predict()
        {
            //http://publicdata.landregistry.gov.uk/market-trend-data/house-price-index-data/UK-HPI-full-file-2019-07.csv
            var header = File.ReadLines("UK-HPI-full-file-2019-07.csv").First();
            var columnNames = header.Split(",");

            var parser = new CsvParser(() => new StringReader(File.ReadAllText("UK-HPI-full-file-2019-07.csv")), ',', false, true);

            var features = parser.EnumerateRows().ToArray();
            var targets = parser.EnumerateRows(_targetName).ToArray();

            string previous = null;
            Dictionary<string, (List<double[]>, double[])> data = new Dictionary<string, (List<double[]>, double[])>();

            List<double[]> regionFeatures = null;
            List<double> regionTargets = null;
            //var isFirst = true;
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
                            data.Add(previous, (regionFeatures, regionTargets.ToArray()));
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
                regionFeatures.Add(item.GetValues(new[] { "Date" }).Select(s => (double)DateTime.Parse(s, new CultureInfo("en-GB")).Ticks).ToArray());

                //last target is future
                if (i > _targetOffset && features.Length - _targetOffset - i <= 0)
                {
                    regionTargets.Add(-1);
                }
                //target is next observation
                else if (features[i + _targetOffset].GetValue("RegionName") == key)
                {
                    var value = targets.Skip(i).Take(_targetOffset).ToArray().Where(w => !string.IsNullOrEmpty(w.GetValue(_targetName))).Select(s => s.GetValue(_targetName)).ToArray();
                    regionTargets.Add(!value.Any() ? 0d : value.Average(a => double.Parse(a)));
                }

                previous = key;
            }

            //var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            //File.WriteAllText("property_data.json", json);

            var regionNames = new BinaryFeatureEncoder().Encode(data.Keys);

            var itemCount = 0;
            foreach (var item in data)
            {
                var meanZeroTransformer = new MeanZeroFeatureTransformer();
                for (int i = 0; i < item.Value.Item1.Count(); i++)
                {
                    item.Value.Item1[i] = item.Value.Item1[i].Concat(regionNames[item.Key]).ToArray();
                }

                F64Matrix transformed = meanZeroTransformer.Transform(item.Value.Item1.ToArray());
                //F64Matrix transformed = item.Value.Item1;

                var numberOfFeatures = transformed.ColumnCount;

                //var learner = GetRandomForest();

                //var learner = GetNeuralnet(numberOfFeatures);

                var learner = GetAda();

                var allObservationsExceptLast = (F64Matrix)transformed.Rows(Enumerable.Range(0, transformed.RowCount - _targetOffset - 1).ToArray());
                var allTargetsExceptLast = item.Value.Item2.Take(transformed.RowCount - _targetOffset - 1).ToArray();
                var model = learner.Learn(allObservationsExceptLast, allTargetsExceptLast);

                //var validation = new TimeSeriesCrossValidation<double>((int)(allObservationsExceptLast.RowCount * 0.8), 0, 1);
                //var validationPredictions = validation.Validate((IIndexedLearner<double>)learner, allObservationsExceptLast, allTargetsExceptLast);
                //var crossMetric = new MeanSquaredErrorRegressionMetric();
                //var crossError = crossMetric.Error(validation.GetValidationTargets(allTargetsExceptLast), validationPredictions);
                //_totalCrossError += crossError;

                var prediction = model.Predict(transformed.Row(transformed.RowCount - 1));
                var before = item.Value.Item2[transformed.RowCount - _targetOffset - 1];
                var change = Math.Round(prediction / before, 2);

                var allPrediction = model.Predict(allObservationsExceptLast);

                var metric = new MeanSquaredErrorRegressionMetric();
                var error = metric.Error(allTargetsExceptLast, allPrediction);
                _totalError += error;
                itemCount++;
                var isLondon = London.Contains(item.Key);

                //var message = $"TotalError: {(int)(_totalError / itemCount)}, TotalCrossError: {(_totalCrossError / itemCount)}, Region: {item.Key}, London: {isLondon}, Error: {error}, CrossError: {crossError}, Next: {prediction}, Change: {change}";
                var message = $"TotalError: {(int)(_totalError / itemCount)}, Region: {item.Key}, London: {isLondon}, Error: {error}, Next: {prediction}, Change: {change}";


                Program.Logger.Info(message);

            }
            Console.ReadKey();
        }

        private ILearner<double> GetNeuralnet(int numberOfFeatures)
        {
            var net = new NeuralNet();
            net.Add(new InputLayer(inputUnits: numberOfFeatures));
            net.Add(new DenseLayer(numberOfFeatures, Activation.Relu));
            net.Add(new SquaredErrorRegressionLayer());

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
            return new RegressionSquareLossGradientBoostLearner(iterations: 3000, maximumTreeDepth: 2000);
        }

        private ILearner<double> GetAda()
        {
            return new RegressionAdaBoostLearner(maximumTreeDepth: 1000, iterations: 500, learningRate: 0.01);
        }

    }
}
