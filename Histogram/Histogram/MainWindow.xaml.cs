using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Histogram;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private BitmapImage sourceImage;
    private WriteableBitmap modifiedImage;

    int imgWidth, imgHeight;
    byte[] pixelData;
    int[] grayPixelValues;
    List<Color> colorPixels = new List<Color>();
    int bpp, rowStride;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog fileDialog = new OpenFileDialog
        {
            Filter = "JPEG Files|*.jpg;*.jpeg"
        };

        if (fileDialog.ShowDialog() == true)
        {
            string path = fileDialog.FileName;

            if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                LoadAndDisplayImage(path);
            }
            else
            {
                MessageBox.Show("Unsupported file format.");
            }
        }
    }

    private void LoadAndDisplayImage(string path)
    {
        try
        {
            BitmapImage img = new BitmapImage(new Uri(path));
            sourceImage = img;

            imgWidth = sourceImage.PixelWidth;
            imgHeight = sourceImage.PixelHeight;
            bpp = (sourceImage.Format.BitsPerPixel + 7) / 8;
            rowStride = imgWidth * bpp;

            byte[] rawPixels = new byte[imgHeight * rowStride];
            sourceImage.CopyPixels(rawPixels, rowStride, 0);
            pixelData = rawPixels;

            modifiedImage = new WriteableBitmap(sourceImage);
            displayedImage.Source = sourceImage;

            InitializeColorPixels();
            GenerateGrayScaleValues();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading JPEG file: " + ex.Message);
        }
    }

    private void StretchHistogram_Click(object sender, RoutedEventArgs e)
    {
        if (sourceImage != null)
        {
            ApplyHistogramStretch();
        }
    }

    private void EqualizeHistogram_Click(object sender, RoutedEventArgs e)
    {
        if (sourceImage != null)
        {
            ApplyHistogramEqualization();
        }
    }

    private void ApplyHistogramStretch()
    {
        int[] grayscaleValues = new int[colorPixels.Count];

        for (int i = 0; i < colorPixels.Count; i++)
        {
            grayscaleValues[i] = (colorPixels[i].R + colorPixels[i].B + colorPixels[i].G) / 3;
        }

        int minGray = grayscaleValues.Min();
        int maxGray = grayscaleValues.Max();

        for (int i = 0; i < grayscaleValues.Length; i++)
        {
            grayscaleValues[i] = (int)(255 * (double)(grayscaleValues[i] - minGray) / (maxGray - minGray));
        }
        UpdateImage(GenerateDrawableArray(grayscaleValues));
    }

    private void ApplyHistogramEqualization()
    {
        int[] grayscaleHistogram = CalculateGrayScaleHistogram();
        int totalPixels = pixelData.Length;

        int[] cumulativeDist = new int[256];
        cumulativeDist[0] = grayscaleHistogram[0];
        for (int i = 1; i < 256; i++)
        {
            cumulativeDist[i] = cumulativeDist[i - 1] + grayscaleHistogram[i];
        }

        int minCdf = cumulativeDist.Min();
        int maxCdf = cumulativeDist.Max();

        byte[] equalizedPixels = new byte[totalPixels];
        for (int i = 0; i < totalPixels; i += 4)
        {
            int grayValue = (pixelData[i] + pixelData[i + 1] + pixelData[i + 2]) / 3;
            int newGrayValue = (int)(255.0 * (cumulativeDist[grayValue] - minCdf) / (maxCdf - minCdf));
            equalizedPixels[i] = equalizedPixels[i + 1] = equalizedPixels[i + 2] = (byte)newGrayValue;
            equalizedPixels[i + 3] = 255;
        }
        UpdateImage(equalizedPixels);
        UpdateColorPixels(equalizedPixels);
    }

    private void ManualThreshold_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(ValueTextBox.Text, out int thresholdValue))
        {
            byte[] binaryPixels = GenerateBinaryImage(thresholdValue);
            UpdateImage(binaryPixels);
        }
        else
        {
            MessageBox.Show("Enter a valid threshold value.");
        }
    }

    private void PercentBlackSelection_Click(object sender, RoutedEventArgs e)
    {
        int[] histogramData = CalculateGrayScaleHistogram();
        int totalPixels = colorPixels.Count;
        int targetThreshold = 0;
        int targetPercentage = 50;
        bool foundThreshold = false;
        int cumulativeCount = 0, histIndex = 0;

        while (!foundThreshold)
        {
            cumulativeCount += histogramData[histIndex];
            double currentPercent = (double)cumulativeCount / totalPixels * 100;
            if (currentPercent < targetPercentage)
            {
                histIndex++;
            }
            else
            {
                foundThreshold = true;
            }
            targetThreshold = histIndex;
        }
        byte[] binaryData = GenerateBinaryImage(targetThreshold);
        UpdateImage(binaryData);
    }

    private void MeanIterativeSelection_Click(object sender, RoutedEventArgs e)
    {
        int threshold = 128;
        int[] histogram = CalculateGrayScaleHistogram();
        while (true)
        {
            int sumLower = 0, countLower = 0, sumUpper = 0, countUpper = 0;

            for (int i = 0; i < histogram.Length; i++)
            {
                if (i < threshold)
                {
                    sumLower += i * histogram[i];
                    countLower += histogram[i];
                }
                else
                {
                    sumUpper += i * histogram[i];
                    countUpper += histogram[i];
                }
            }

            int newThreshold = (sumLower / countLower + sumUpper / countUpper) / 2;
            if (newThreshold == threshold)
            {
                break;
            }

            threshold = newThreshold;
        }

        byte[] binaryPixels = GenerateBinaryImage(threshold);
        UpdateImage(binaryPixels);
    }

    private byte[] GenerateBinaryImage(int threshold)
    {
        byte[] binaryArray = new byte[pixelData.Length];
        for (int i = 0; i < pixelData.Length; i += 4)
        {
            binaryArray[i] = (byte)(grayPixelValues[i] < threshold ? 0 : 255);
            binaryArray[i + 1] = (byte)(grayPixelValues[i + 1] < threshold ? 0 : 255);
            binaryArray[i + 2] = (byte)(grayPixelValues[i + 2] < threshold ? 0 : 255);
            binaryArray[i + 3] = 255;
        }

        return binaryArray;
    }

    private byte[] GenerateDrawableArray(int[] valuesArray)
    {
        byte[] byteArr = new byte[pixelData.Length];
        int currentIndex = 0;
        for (int i = 0; i < pixelData.Length; i += 4)
        {
            byteArr[i] = byteArr[i + 1] = byteArr[i + 2] = (byte)valuesArray[currentIndex];
            byteArr[i + 3] = 255;
            currentIndex++;
        }
        return byteArr;
    }

    private int[] CalculateGrayScaleHistogram()
    {
        int[] grayscaleArray = new int[256];
        int avg;

        foreach (Color pixel in colorPixels)
        {
            avg = (pixel.R + pixel.B + pixel.G) / 3;
            grayscaleArray[avg]++;
        }
        return grayscaleArray;
    }

    private void UpdateImage(byte[] imgBytes)
    {
        modifiedImage.WritePixels(new Int32Rect(0, 0, imgWidth, imgHeight), imgBytes, rowStride, 0);
        displayedImage.Source = modifiedImage;
    }

    private void InitializeColorPixels()
    {
        colorPixels.Clear();
        for (int i = 0; i + 3 < pixelData.Length; i += 4)
        {
            colorPixels.Add(Color.FromArgb(255, pixelData[i + 2], pixelData[i + 1], pixelData[i]));
        }
    }

    private void UpdateColorPixels(byte[] pixelArray)
    {
        colorPixels.Clear();
        for (int i = 0; i + 3 < pixelArray.Length; i += 4)
        {
            colorPixels.Add(Color.FromArgb(255, pixelArray[i + 2], pixelArray[i + 1], pixelArray[i]));
        }
    }

    private void GenerateGrayScaleValues()
    {
        int avg;
        grayPixelValues = new int[pixelData.Length];

        for (int i = 0; i < pixelData.Length; i += 4)
        {
            avg = (pixelData[i] + pixelData[i + 1] + pixelData[i + 2]) / 3;
            grayPixelValues[i] = grayPixelValues[i + 1] = grayPixelValues[i + 2] = avg;
            grayPixelValues[i + 3] = 255;
        }
    }
}