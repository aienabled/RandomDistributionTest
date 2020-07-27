namespace WpfApp8_RandomDistribution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.KeyDown += (sender, args) =>
                            {
                                if (args.Key == Key.F5)
                                {
                                    this.Start();
                                }
                            };

            this.Start();
        }

        private static double CalcMedian(List<double> numbers)
        {
            int numberCount = numbers.Count;
            int halfIndex = numbers.Count / 2;
            var sortedNumbers = numbers.OrderBy(n => n);
            double median;
            if ((numberCount % 2) == 0)
            {
                median = (sortedNumbers.ElementAt(halfIndex)
                          + sortedNumbers.ElementAt((halfIndex - 1)))
                         / 2.0;
            }
            else
            {
                median = sortedNumbers.ElementAt(halfIndex);
            }

            return median;
        }

        private static Vector2[] CalculatePoints(
            out string text,
            double targetProbabilityReverse,
            out double thresholdLineX)
        {
            List<double> listSamples;
            (text, listSamples) = GenerateData(targetProbabilityReverse);

            var result = new List<Vector2>();

            const double scale = 100;
            var bucketsCount = Math.Min(targetProbabilityReverse, 50);
            double max = listSamples.Max();
            double min = listSamples.Min();
            double bucketSize = (max - min) / (double)bucketsCount;

            thresholdLineX = scale * targetProbabilityReverse / (max - min);

            for (var i = 0; i < bucketsCount; i++)
            {
                var currentRangeFrom = min + i * bucketSize;
                var currentRangeTo = min + (i + 1) * bucketSize;

                if (i == bucketsCount - 1)
                {
                    currentRangeTo = double.MaxValue;
                }

                var countInRange = listSamples.Count(v => v >= currentRangeFrom
                                                          && v < currentRangeTo);

                var x = scale * (i / (double)(bucketsCount - 1));
                var y = scale * countInRange / (double)listSamples.Count;

                // normalize values
                // TODO: this is not correct but essential to indicate
                y *= bucketsCount / 2.0;

                result.Add(new Vector2((float)x, (float)y));
            }

            return result.ToArray();
        }

        private static Geometry CreateGeometryForData(Vector2[] dataPoints)
        {
            var maxValue = 100.0; //double.MinValue;
            var minValue = 0.0;   //double.MaxValue;
            foreach (var dataPoint in dataPoints)
            {
                if (dataPoint.Y > maxValue)
                {
                    maxValue = dataPoint.Y;
                }

                if (dataPoint.Y < minValue)
                {
                    minValue = dataPoint.Y;
                }
            }

            var normalizationCoefficient = (100.0 / (maxValue - minValue));

            var g = new StreamGeometry();
            using var c = g.Open();

            var isFirst = true;
            foreach (var dataPoint in dataPoints)
            {
                var x = (double)dataPoint.X;
                var y = (double)dataPoint.Y;

                y -= minValue;
                y *= normalizationCoefficient;

                var point = new Point(x, y);
                if (isFirst)
                {
                    isFirst = false;
                    c.BeginFigure(point, isFilled: false, isClosed: false);
                    continue;
                }

                c.LineTo(point, isStroked: true, isSmoothJoin: false);
            }

            return g;
        }

        private static (string sb, List<double> list) GenerateData(double targetProbabilityReverse)
        {
            const int SamplesCount = 50000;

            var random = new Random();

            var listSamples = new List<double>();
            for (var sampleNumber = 0; sampleNumber < SamplesCount; sampleNumber++)
            {
                var attempt = 1;
                do
                {
                    //var p = targetProbabilityReverse;

                    // Probability compensation mechanism idea:
                    // the more attempts were made, the bigger threshold use.
                    var p = 2 * targetProbabilityReverse - attempt;

                    var rolledValue = random.NextDouble(); // roll random value in range from 0 to 1 (excluding 1)

                    if (rolledValue <= 1 / p)
                    {
                        listSamples.Add(attempt);
                        break;
                    }

                    attempt++;
                }
                while (true);
            }

            listSamples.Sort();

            var min = listSamples.Min();
            var max = listSamples.Max();

            var listAverage = listSamples.Sum() / listSamples.Count;
            var listMedian = CalcMedian(listSamples);

            //var loosers = listSamples.Where(s => s < 50).Count();
            //var winners = listSamples.Where(s => s > 3000).Count();

            var sb = new StringBuilder();
            sb.AppendLine($"Target probability: 1/{targetProbabilityReverse:0.##} | Samples count: {SamplesCount}");
            sb.AppendLine(
                string.Format("Random rolls necessary:"
                              + Environment.NewLine
                              + "- range: [{0:0.##};{1:0.##}]"
                              + Environment.NewLine
                              + "- average: {2:0.##}"
                              + Environment.NewLine
                              + "- median: {3:0.##}",
                              min,
                              max,
                              listAverage,
                              listMedian));
            sb.AppendLine("----------------------------------------------------------");

            var bucketsCount = 10;
            var step = (max - min) / (double)bucketsCount;

            for (var i = 0; i < bucketsCount; i++)
            {
                var currentRangeFrom = min + i * step;
                var currentRangeTo = min + (i + 1) * step;

                var currentRangeToForCheck = currentRangeTo;
                if (i == bucketsCount - 1)
                {
                    currentRangeToForCheck = double.MaxValue;
                }

                var countInRange = listSamples.Count(v => v >= currentRangeFrom
                                                          && v < currentRangeToForCheck);

                var signsCount = (int)Math.Round(120 * (double)countInRange / SamplesCount);

                sb.AppendLine(
                    "["
                    + string.Format("{0:0.##}",  (int)currentRangeFrom).PadLeft(6)
                    + string.Format(";{0:0.##}", (int)currentRangeTo).PadRight(7)
                    + "]  |"
                    + new string('#', signsCount)
                    + "|");
            }

            return (sb.ToString(), listSamples);
        }

        private void AddCoordinateGrid()
        {
            var strokeBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x55, 0x55, 0x55));
            var strokeThickness = 0.125;

            var scale = 100.0;
            for (var v = 0.1; v <= 0.9; v += 0.1)
            {
                var lineColumn = new Line()
                {
                    X1 = v * scale,
                    X2 = v * scale,
                    Y1 = 0,
                    Y2 = scale,
                    Stroke = strokeBrush,
                    StrokeThickness = strokeThickness
                };

                var lineRow = new Line()
                {
                    Y1 = v * scale,
                    Y2 = v * scale,
                    X1 = 0,
                    X2 = scale,
                    Stroke = strokeBrush,
                    StrokeThickness = strokeThickness
                };

                this.Grid.Children.Add(lineColumn);
                this.Grid.Children.Add(lineRow);
            }
        }

        private void Start()
        {
            // test for 1/Xth probability
            var targetProbabilityReverse = 1000;

            var dataPoints = CalculatePoints(out var text,
                                             targetProbabilityReverse: targetProbabilityReverse,
                                             out var thresholdLineX);
            this.Grid.Children.Clear();
            this.TextBlock.Text = text;

            this.AddCoordinateGrid();

            // add data points line
            this.Grid.Children.Add(
                new Path()
                {
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 0.25,
                    Data = CreateGeometryForData(dataPoints)
                });

            // add vertical line for the target probability
            this.Grid.Children.Add(
                new Path()
                {
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 0.25,
                    Data = new LineGeometry(startPoint: new Point(thresholdLineX, 0),
                                            endPoint: new Point(thresholdLineX,   100))
                });
        }
    }
}