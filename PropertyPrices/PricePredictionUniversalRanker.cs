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
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SharpLearning.Common.Interfaces;
using SharpLearning.RandomForest.Learners;
using SharpLearning.Neural.Optimizers;
using SharpLearning.GradientBoost.Learners;
using SharpLearning.AdaBoost.Learners;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Concurrent;
using ServiceStack.Text;

namespace PropertyPrices
{
    partial class PricePredictionUniversalRanker
    {

        static string[] excludeColumns = { "RegionName", "AreaCode" };

        static string[] London = { "Barking and Dagenham", "Barnet", "Bexley", "Brent", "Bromley", "Camden", "Croydon", "Ealing", "Enfield", "Greenwich", "Hackney", "Hammersmith and Fulham", "Haringey", "Harrow", "Havering", "Hillingdon", "Hounslow", "Islington", "Kensington and Chelsea", "Kingston upon Thames", "Lambeth", "Lewisham", "Merton", "Newham", "Redbridge", "Richmond upon Thames", "Southwark", "Sutton", "Tower Hamlets", "Waltham Forest", "Wandsworth", "Westminster" };

        double _totalError = 0;
        string _targetName = "FlatPrice";
        const int _targetOffset = 5;
        internal const int DefaultIterations = 100;
        private int _iterations = DefaultIterations;

        private BinaryFeatureEncoder _binaryFeatureEncoder = new BinaryFeatureEncoder();
        private CreditDataExtractor _creditDataExtractor = new CreditDataExtractor();
        private TargetCalculator _targetExtractor = new TargetCalculator();

        public void Predict(int iterations = DefaultIterations)
        {
            _iterations = iterations;

            Program.StatusLogger.Info($"Iterations: {_iterations}");
            Program.StatusLogger.Info($"Target: {_targetName}");
            Program.StatusLogger.Info($"Offset: {_targetOffset}");

            var data = new ConcurrentDictionary<int, ModelData>();

            if (File.Exists(Path()))
            {
                data = JsonConvert.DeserializeObject<ConcurrentDictionary<int, ModelData>>(File.ReadAllText(Path()));
                //data = TypeSerializer.DeserializeFromReader<ConcurrentDictionary<int, ModelData>>(new StreamReader(Path()));

                Program.StatusLogger.Info("Cached data was loaded.");
            }
            else
            {

                //http://publicdata.landregistry.gov.uk/market-trend-data/house-price-index-data/UK-HPI-full-file-2019-07.csv
                var header = File.ReadLines("UK-HPI-full-file-2019-07.csv").First();
                var columnNames = header.Split(",");

                var parser = new CsvParser(() => new StringReader(File.ReadAllText("UK-HPI-full-file-2019-07.csv")), ',', false, true);

                var creditData = _creditDataExtractor.ExtractQuarter();

                var featureRows = parser.EnumerateRows().ToArray();
                var targets = parser.EnumerateRows(_targetName).ToArray();

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

                    var creditDataKey = _creditDataExtractor.GetMonthOfPreviousQuarter(date);
                    if (!creditData.ContainsKey(creditDataKey))
                    {
                        regionFeatures = regionFeatures.Concat(Enumerable.Repeat(-1d, creditData.Values.First().Length));
                        Trace.WriteLine($"Credit data not found: {creditDataKey}");
                    }
                    else
                    {
                        regionFeatures = regionFeatures.Concat(creditData[creditDataKey]);
                    }

                    data.TryAdd(i, new ModelData { Name = key, Date = date, Observations = regionFeatures.ToArray(), OriginalTarget = ParseRowValue(item.GetValue(_targetName)) });
                }

                _targetExtractor.Calculate(data, _targetOffset);


                //TypeSerializer.SerializeToWriter<ConcurrentDictionary<int, ModelData>>(data, new StreamWriter(Path()));
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(Path(), json);
            }

            var regionNames = _binaryFeatureEncoder.Encode(data.Select(s => s.Value.Name));
            for (int i = 0; i < data.Count(); i++)
            {
                data[i].Observations = data[i].Observations.Concat(regionNames[data[i].Name]).ToArray();
            }

            //data.Where(d => d.Value.Target != -1)
            data = new ConcurrentDictionary<int, ModelData>(data.OrderBy(o => o.Value.Date));

            var itemCount = 0;

            //var numberOfFeatures = transformed.ColumnCount;

            //var learner = GetRandomForest();

            //var learner = GetNeuralnet(numberOfFeatures);

            var learner = GetAda();

            var lastDate = data.Last().Value.Date;

            var dataWithTarget = data.Where(s => s.Value.Target != -1);

            var allObservations = dataWithTarget.Select(s => s.Value.Observations).ToArray();
            var allTargets = dataWithTarget.Select(s => s.Value.Target).ToArray();

            //var splitter = new NoShuffleTrainingTestIndexSplitter<double>(0.8);
            //var split = splitter.SplitSet(dateSortedData.Select(s => s.First).ToArray(), dateSortedData.Select(s => s.Second).ToArray());

            var meanZeroTransformer = new MeanZeroFeatureTransformer();
            F64Matrix transformed = meanZeroTransformer.Transform(allObservations);

            Program.StatusLogger.Info("Learning commenced");
            var model = learner.Learn(transformed, allTargets);
            Program.StatusLogger.Info("Learning completed");

            var importanceSummary = string.Join(",\r\n", model.GetRawVariableImportance().Select((d, i) => i.ToString() + ":" + d.ToString()));

            Program.StatusLogger.Info("Raw variable importance:\r\n" + importanceSummary);

            var lastObservations = data.Where(s => s.Value.Date == lastDate).Select(s => s.Value.Observations).ToArray();

            var prediction = model.Predict(lastObservations);
            //var before = item.Targets[transformed.RowCount - _targetOffset - 1];
            //var change = Math.Round(prediction / before, 2);

            var allPrediction = model.Predict(transformed);

            var metric = new MeanSquaredErrorRegressionMetric();
            var error = metric.Error(allTargets, allPrediction);
            _totalError = error;
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
            return new RegressionAdaBoostLearner(maximumTreeDepth: 35, iterations: _iterations, learningRate: 0.1);
        }

        private double ParseRowValue(string value)
        {

            if (DateTime.TryParseExact(value, "dd/MM/yyyy", new CultureInfo("en-GB"), DateTimeStyles.AssumeLocal, out var parsed))
            {
                return (double)parsed.Ticks;
            }

            return string.IsNullOrEmpty(value) ? -1d : double.Parse(value);
        }

        private string Path()
        {
            return _targetOffset + "_" + _targetName + "_property_data.json";
        }

    }

}
