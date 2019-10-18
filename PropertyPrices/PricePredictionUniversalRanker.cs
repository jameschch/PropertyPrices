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
using SharpLearning.CrossValidation.TrainingTestSplitters;

namespace PropertyPrices
{
    class PricePredictionUniversalRanker
    {

        static string[] excludeColumns = { "RegionName", "AreaCode" };

        static string[] London = { "Barking and Dagenham", "Barnet", "Bexley", "Brent", "Bromley", "Camden", "Croydon", "Ealing", "Enfield", "Greenwich", "Hackney", "Hammersmith and Fulham", "Haringey", "Harrow", "Havering", "Hillingdon", "Hounslow", "Islington", "Kensington and Chelsea", "Kingston upon Thames", "Lambeth", "Lewisham", "Merton", "Newham", "Redbridge", "Richmond upon Thames", "Southwark", "Sutton", "Tower Hamlets", "Waltham Forest", "Wandsworth", "Westminster" };

        double _totalError = 0;
        double _totalCrossError = 0;
        string _targetName = "Flat12m%Change";
        const int _targetOffset = 10;
        private int _iterations = 500;

        private BinaryFeatureEncoder _binaryFeatureEncoder = new BinaryFeatureEncoder();
        private CreditDataExtractor _creditDataExtractor = new CreditDataExtractor();

        public void Predict(int iterations = 500)
        {
            _iterations = iterations;

            Program.StatusLogger.Info($"Iterations: {_iterations}");

            //http://publicdata.landregistry.gov.uk/market-trend-data/house-price-index-data/UK-HPI-full-file-2019-07.csv
            var header = File.ReadLines("UK-HPI-full-file-2019-07.csv").First();
            var columnNames = header.Split(",");

            var parser = new CsvParser(() => new StringReader(File.ReadAllText("UK-HPI-full-file-2019-07.csv")), ',', false, true);

            var creditData = _creditDataExtractor.Extract();

            var featureRows = parser.EnumerateRows().ToArray();
            var targets = parser.EnumerateRows(_targetName).ToArray();

            var data = new List<Data>();
            string previousKey = null;

            for (int i = 0; i < featureRows.Length; i++)
            {
                var item = featureRows[i];
                var key = item.GetValue("RegionName");
                var date = DateTime.ParseExact(item.GetValue("Date"), "dd/MM/yyyy", new CultureInfo("en-GB"), DateTimeStyles.AssumeLocal);

                if (key != previousKey)
                {
                    Program.StatusLogger.Info($"Processing {key}");
                }
                previousKey = key;

                var regionFeatures = item.GetValues(columnNames.Except(excludeColumns).ToArray()).Select(s => ParseRowValue(s));
                var creditDataKey = _creditDataExtractor.GetKey(date);
                if (!creditData.ContainsKey(creditDataKey))
                {
                    Program.StatusLogger.Info($"Credit data not found: {creditDataKey}");
                }
                else
                {
                    regionFeatures = regionFeatures.Concat(creditData[creditDataKey]);
                }

                double regionTargets = -1;
                //last target is future
                if (i > _targetOffset && featureRows.Length - _targetOffset - i <= 0)
                {
                    regionTargets = -1;
                }
                //target is next observation
                else if (featureRows[i + _targetOffset].GetValue("RegionName") == key)
                {
                    var value = targets.Skip(i).Take(_targetOffset).ToArray().Where(w => !string.IsNullOrEmpty(w.GetValue(_targetName))).Select(s => s.GetValue(_targetName)).ToArray();
                    regionTargets = !value.Any() ? -1d : value.Average(a => double.Parse(a));
                }

                data.Add(new Data { Key = key, Date = date, Observations = regionFeatures.ToArray(), Targets = regionTargets });
            }

            //var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            //File.WriteAllText("property_data.json", json);

            var regionNames = _binaryFeatureEncoder.Encode(data.Select(s => s.Key));
            for (int i = 0; i < data.Count(); i++)
            {
                data[i].Observations = data[i].Observations.Concat(regionNames[data[i].Key]).ToArray();
            }

            data = data.Where(d => d.Targets != -1).OrderBy(o => o.Date).ToList();

            var itemCount = 0;

            //var numberOfFeatures = transformed.ColumnCount;

            //var learner = GetRandomForest();

            //var learner = GetNeuralnet(numberOfFeatures);

            var learner = GetAda();

            var lastDate = data.Last().Date;

            var allObservationsExceptLast = data.Where(s => s.Date != lastDate).Select(s => s.Observations).ToArray();
            var allTargetsExceptLast = data.Where(s => s.Date != lastDate).Select(s => s.Targets).ToArray();

            //var splitter = new NoShuffleTrainingTestIndexSplitter<double>(0.8);
            //var split = splitter.SplitSet(dateSortedData.Select(s => s.First).ToArray(), dateSortedData.Select(s => s.Second).ToArray());

            var meanZeroTransformer = new MeanZeroFeatureTransformer();
            F64Matrix transformed = meanZeroTransformer.Transform(allObservationsExceptLast);

            Program.StatusLogger.Info("Learning commenced");
            var model = learner.Learn(transformed, allTargetsExceptLast);
            Program.StatusLogger.Info("Learning completed");

            var importanceSummary = string.Join(",\r\n", model.GetRawVariableImportance().Select((d, i) => i.ToString() + ":" + d.ToString()));

            Program.StatusLogger.Info("Raw variable importance:" + importanceSummary);

            var lastObservations = data.Where(s => s.Date == lastDate).Select(s => s.Observations).ToArray();

            var prediction = model.Predict(lastObservations);
            //var before = item.Targets[transformed.RowCount - _targetOffset - 1];
            //var change = Math.Round(prediction / before, 2);

            var allPrediction = model.Predict(meanZeroTransformer.Transform(allObservationsExceptLast));

            var metric = new MeanSquaredErrorRegressionMetric();
            var error = metric.Error(allTargetsExceptLast, allPrediction);
            _totalError += error;
            itemCount++;

            foreach (var item in lastObservations.Zip(prediction))
            {
                var regionName = _binaryFeatureEncoder.Decode(item.First);
                var isLondon = London.Contains(regionName);

                //var message = $"TotalError: {(int)(_totalError / itemCount)}, TotalCrossError: {(_totalCrossError / itemCount)}, Region: {item.Key}, London: {isLondon}, Error: {error}, CrossError: {crossError}, Next: {prediction}, Change: {change}";
                //var message = $"TotalError: {(int)(_totalError / itemCount)}, Region: {item.Key}, London: {isLondon}, Error: {error}, Next: {prediction}, Change: {change}";
                var message = $"TotalError: {Math.Round(_totalError, 6)}, Region: {regionName}, London: {isLondon}, Error: -1, Next: {item.Second}, Change: -1";
                Program.Logger.Info(message);
            }

            Program.StatusLogger.Info("Prediction completed");
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
            return new RegressionAdaBoostLearner(maximumTreeDepth: 1000, iterations: _iterations, learningRate: 0.01);
        }

        private double ParseRowValue(string value)
        {

            if (DateTime.TryParseExact(value, "dd/MM/yyyy", new CultureInfo("en-GB"), DateTimeStyles.AssumeLocal, out var parsed))
            {
                return (double)parsed.Ticks;
            }

            return string.IsNullOrEmpty(value) ? -1d : double.Parse(value);
        }

        private class Data
        {
            public string Key;
            public DateTime Date;
            public double[] Observations;
            public double Targets;
        }

    }

}
