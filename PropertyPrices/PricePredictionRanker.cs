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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PropertyPrices
{
    class PricePredictionRanker
    {

        static string[] excludeColumns = { "RegionName", "AreaCode" };

        static string[] London = { "Barking and Dagenham", "Barnet", "Bexley", "Brent", "Bromley", "Camden", "Croydon", "Ealing", "Enfield", "Greenwich", "Hackney", "Hammersmith and Fulham", "Haringey", "Harrow", "Havering", "Hillingdon", "Hounslow", "Islington", "Kensington and Chelsea", "Kingston upon Thames", "Lambeth", "Lewisham", "Merton", "Newham", "Redbridge", "Richmond upon Thames", "Southwark", "Sutton", "Tower Hamlets", "Waltham Forest", "Wandsworth", "City of Westminster" };

        double _totalError = 0;
        double _totalCrossError = 0;
        string _targetName = "FlatPrice";
        const int _targetOffset = 1;
        internal const int DefaultIterations = 1200;
        private int _iterations = DefaultIterations;
        static object Locker = new object();

        private CreditDataExtractor _creditDataExtractor = new CreditDataExtractor();
        private TargetExtractor _targetExtractor = new TargetExtractor();

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

                var creditData = _creditDataExtractor.Extract();

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

                    var creditDataKey = _creditDataExtractor.GetKey(date, creditData.Keys.ToArray());
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

                _targetExtractor.Extract(data, _targetOffset);


                //TypeSerializer.SerializeToWriter<ConcurrentDictionary<int, ModelData>>(data, new StreamWriter(Path()));
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(Path(), json);
            }



            var itemCount = 0;
            Parallel.ForEach(data.OrderBy(o => o.Value.Date).GroupBy(g => g.Value.Name).AsParallel(), new ParallelOptions { MaxDegreeOfParallelism = -1 }, (grouping) =>
             {

                 var lastDate = grouping.Last().Value.Date;
                 var dataWithTarget = grouping.Where(s => s.Value.Target != -1);

                 if (dataWithTarget.Any())
                 {
                     var allObservations = dataWithTarget.Select(s => s.Value.Observations).ToArray();
                     var allTargets = dataWithTarget.Select(s => s.Value.Target).ToArray();

                     //var learner = GetRandomForest();
                     //var learner = GetAda();
                     var learner = GetNeuralnet(grouping.First().Value.Observations.Length, allObservations.Length);

                     //var validation = new TimeSeriesCrossValidation<double>((int)(allObservationsExceptLast.RowCount * 0.8), 0, 1);
                     //var validationPredictions = validation.Validate((IIndexedLearner<double>)learner, allObservationsExceptLast, allTargetsExceptLast);
                     //var crossMetric = new MeanSquaredErrorRegressionMetric();
                     //var crossError = crossMetric.Error(validation.GetValidationTargets(allTargetsExceptLast), validationPredictions);
                     //_totalCrossError += crossError;
                     var meanZeroTransformer = new MeanZeroFeatureTransformer();
                     var minMaxTransformer = new MinMaxTransformer(0d, 1d);
                     var lastObservations = grouping.Last().Value.Observations;
                     F64Matrix allTransformed = minMaxTransformer.Transform(meanZeroTransformer.Transform(allObservations.Append(lastObservations).ToArray()));
                     var transformed = new F64Matrix(allTransformed.Rows(Enumerable.Range(0, allTransformed.RowCount - 1).ToArray()).Data(), allTransformed.RowCount - 1, allTransformed.ColumnCount);

                     Program.StatusLogger.Info("Learning commenced " + grouping.First().Value.Name);
                     var model = learner.Learn(transformed, allTargets);
                     Program.StatusLogger.Info("Learning completed " + grouping.First().Value.Name);

                     if (model.GetRawVariableImportance().Any(a => a > 0))
                     {
                         var importanceSummary = string.Join(",\r\n", model.GetRawVariableImportance().Select((d, i) => i.ToString() + ":" + d.ToString()));
                         Program.StatusLogger.Info("Raw variable importance:\r\n" + importanceSummary);
                     }


                     var lastTransformed = allTransformed.Row(transformed.RowCount);
                     var prediction = model.Predict(lastTransformed);

                     //var before = item.Value.Item2[transformed.RowCount - _targetOffset - 1];
                     var change = -1;//Math.Round(prediction / before, 2);

                     var allPrediction = model.Predict(transformed);

                     var metric = new MeanSquaredErrorRegressionMetric();
                     var error = metric.Error(allTargets, allPrediction);
                     _totalError += error;
                     itemCount++;
                     var isLondon = London.Contains(grouping.First().Value.Name);

                     //var message = $"TotalError: {(int)(_totalError / itemCount)}, TotalCrossError: {(_totalCrossError / itemCount)}, Region: {item.Key}, London: {isLondon}, Error: {error}, CrossError: {crossError}, Next: {prediction}, Change: {change}";
                     var message = $"TotalError: {Math.Round(_totalError / itemCount, 3)}, Region: {grouping.First().Value.Name}, London: {isLondon}, Error: {Math.Round(error, 3)}, Next: {Math.Round(prediction, 3)}, Change: {change}";

                     Program.Logger.Info(message);
                 }
             });

            Console.ReadKey();
        }

        private ILearner<double> GetNeuralnet(int numberOfFeatures, int batchSize)
        {
            var net = new NeuralNet();
            net.Add(new InputLayer(inputUnits: numberOfFeatures));
            net.Add(new DenseLayer(numberOfFeatures, Activation.Sigmoid));
            net.Add(new SquaredErrorRegressionLayer());

            var learner = new RegressionNeuralNetLearner(net, learningRate: 0.01, iterations: _iterations, loss: new SquareLoss(), batchSize: batchSize, optimizerMethod: OptimizerMethod.RMSProp);

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
            return new RegressionAdaBoostLearner(maximumTreeDepth: 35, iterations: _iterations/*200*/, learningRate: 0.1, loss: AdaBoostRegressionLoss.Linear);
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
