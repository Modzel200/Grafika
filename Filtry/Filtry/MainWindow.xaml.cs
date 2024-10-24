using System.IO;
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

namespace Filtry;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private BitmapImage sourceImage;
    private bool isOriginalImageChanged = false;
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnLoadImage_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog fileDialog = new OpenFileDialog();
        fileDialog.Filter = "JPEG Files|*.jpg;*.jpeg";

        if (fileDialog.ShowDialog() == true)
        {
            string selectedFilePath = fileDialog.FileName;

            if (selectedFilePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                selectedFilePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                DisplayImage(selectedFilePath);
            }
            else
            {
                MessageBox.Show("Unsupported file format.");
            }
        }
    }

    private void DisplayImage(string filePath)
    {
        try
        {
            BitmapImage image = new BitmapImage(new Uri(filePath));
            sourceImage = image;
            MainImage.Source = image;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading JPEG file: " + ex.Message);
        }
    }

    private void OnAddition_Click(object sender, RoutedEventArgs e)
    {
        ExecuteArithmeticOperation(1);
    }

    private void OnSubtraction_Click(object sender, RoutedEventArgs e)
    {
        ExecuteArithmeticOperation(-1);
    }

    private void OnMultiplication_Click(object sender, RoutedEventArgs e)
    {
        ExecuteArithmeticOperation(2, true);
    }

    private void OnDivision_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(ParameterTextBox.Text, out double divisor) && divisor != 0)
        {
            ExecuteArithmeticOperation(1.0 / divisor, true);
        }
        else
        {
            MessageBox.Show("Please enter a valid divisor.");
        }
    }

    private void OnBrightnessChange_Click(object sender, RoutedEventArgs e)
    {
        AdjustBrightnessLevel(2);
    }

    private void OnGrayscale1_Click(object sender, RoutedEventArgs e)
    {
        ApplyGrayscale();
    }

    private void OnGrayscale2_Click(object sender, RoutedEventArgs e)
    {
        ApplyToGrayscaleAlternative();
    }

    private void OnBlurEffect_Click(object sender, RoutedEventArgs e)
    {
        ApplyGaussianBlurFilter();
    }

    private void OnMedianEffect_Click(object sender, RoutedEventArgs e)
    {
        ApplyMedianFilter();
    }

    private void OnSobelEffect_Click(object sender, RoutedEventArgs e)
    {
        ApplySobelFilter();
    }

    private void OnHighPassEffect_Click(object sender, RoutedEventArgs e)
    {
        ApplyHighPassFilter();
    }

    private void OnGaussianBlurEffect_Click(object sender, RoutedEventArgs e)
    {
        ApplyGaussianBlurFilter();
    }

    private void ExecuteArithmeticOperation(double factor, bool isMultiplication = false)
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 4;

                    byte newR, newG, newB;
                    if (isMultiplication)
                    {
                        newR = (byte)Math.Max(0, Math.Min(255, pixelData[offset + 2] * factor));
                        newG = (byte)Math.Max(0, Math.Min(255, pixelData[offset + 1] * factor));
                        newB = (byte)Math.Max(0, Math.Min(255, pixelData[offset] * factor));
                    }
                    else
                    {
                        newR = (byte)Math.Max(0, Math.Min(255, pixelData[offset + 2] + factor));
                        newG = (byte)Math.Max(0, Math.Min(255, pixelData[offset + 1] + factor));
                        newB = (byte)Math.Max(0, Math.Min(255, pixelData[offset] + factor));
                    }

                    pixelData[offset + 2] = newR;
                    pixelData[offset + 1] = newG;
                    pixelData[offset] = newB;
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32 , null, pixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    private void AdjustBrightnessLevel(double brightnessFactor)
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 4;

                    byte newR = (byte)Math.Max(0, Math.Min(255, pixelData[offset + 2] + brightnessFactor));
                    byte newG = (byte)Math.Max(0, Math.Min(255, pixelData[offset + 1] + brightnessFactor));
                    byte newB = (byte)Math.Max(0, Math.Min(255, pixelData[offset] + brightnessFactor));

                    pixelData[offset + 2] = newR;
                    pixelData[offset + 1] = newG;
                    pixelData[offset] = newB;
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    private void ApplyGrayscale()
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 4;

                    byte grayValue = (byte)((pixelData[offset + 2] + pixelData[offset + 1] + pixelData[offset]) / 3);

                    pixelData[offset + 2] = grayValue;
                    pixelData[offset + 1] = grayValue;
                    pixelData[offset] = grayValue;
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    private void ApplyToGrayscaleAlternative()
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 4;

                    byte grayValue = (byte)(0.299 * pixelData[offset + 2] + 0.587 * pixelData[offset + 1] + 0.114 * pixelData[offset]);

                    pixelData[offset + 2] = grayValue;
                    pixelData[offset + 1] = grayValue;
                    pixelData[offset] = grayValue;
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    private void ApplyGaussianBlurFilter()
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            byte[] newPixelData = new byte[pixelData.Length];
            Array.Copy( pixelData, newPixelData, pixelData.Length);

            double[] kernel = {
                1, 2, 1,
                2, 4, 2,
                1,  2, 1
            };

            double kernelSum = kernel.Sum();

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int offset = y * stride + x * 4;

                    double red = 0, green = 0, blue = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int neighborOffset = (y + i) * stride + (x + j) * 4;

                            red += pixelData[neighborOffset + 2] * kernel[(i + 1) * 3 + (j + 1)];
                            green += pixelData[neighborOffset + 1] * kernel[(i + 1) * 3 + (j + 1)];
                            blue += pixelData[neighborOffset] * kernel[(i + 1) * 3 + (j + 1)];
                        }
                    }

                    red /= kernelSum;
                    green /= kernelSum;
                    blue /= kernelSum;

                    newPixelData[offset + 2] = (byte)Math.Max(0, Math.Min(255, red));
                    newPixelData[offset + 1] = (byte)Math.Max(0, Math.Min(255, green));
                    newPixelData[offset] = (byte)Math.Max(0, Math.Min(255, blue));
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, newPixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    private void ApplyMedianFilter()
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int offset = y * stride + x * 4;

                    byte[] redValues = new byte[9];
                    byte[] greenValues = new byte[9];
                    byte[] blueValues = new byte[9];

                    int index = 0;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int neighborOffset = (y + i) * stride + (x + j) * 4;
                            redValues[index] = pixelData[neighborOffset + 2];
                            greenValues[index] = pixelData[neighborOffset + 1];
                            blueValues[index] = pixelData[neighborOffset];
                            index++;
                        }
                    }

                    Array.Sort(redValues);
                    Array.Sort(greenValues);
                    Array.Sort(blueValues);

                    byte newR = redValues[4];
                    byte newG = greenValues[4];
                    byte newB = blueValues[4];

                    pixelData[offset + 2] = newR;
                    pixelData[offset + 1] = newG;
                    pixelData[offset] = newB;
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    private void ApplySobelFilter()
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            byte[] newPixelData = new byte[pixelData.Length];
            Array.Copy(pixelData, newPixelData, pixelData.Length);

            int[] sobelX = { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
            int[] sobelY = { -1, -2, -1, 0, 0, 0, 1, 2, 1 };

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int offset = y * stride + x * 4;

                    int redX = 0, greenX = 0, blueX = 0;
                    int redY = 0, greenY = 0, blueY = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int neighborOffset = (y + i) * stride + (x + j) * 4;
                            int sobelIndex = (i + 1) * 3 + (j + 1);

                            redX += pixelData[neighborOffset + 2] * sobelX[sobelIndex];
                            greenX += pixelData[neighborOffset + 1] * sobelX[sobelIndex];
                            blueX += pixelData[neighborOffset] * sobelX[sobelIndex];

                            redY += pixelData[neighborOffset + 2] * sobelY[sobelIndex];
                            greenY += pixelData[neighborOffset + 1] * sobelY[sobelIndex];
                            blueY += pixelData[neighborOffset] * sobelY[sobelIndex];
                        }
                    }

                    int newRed = (int)Math.Sqrt(redX * redX + redY * redY);
                    int newGreen = (int)Math.Sqrt(greenX * greenX + greenY * greenY);
                    int newBlue = (int)Math.Sqrt(blueX * blueX + blueY * blueY);

                    newPixelData[offset + 2] = (byte)Math.Min(255, newRed);
                    newPixelData[offset + 1] = (byte)Math.Min(255, newGreen);
                    newPixelData[offset] = (byte)Math.Min(255, newBlue);
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, newPixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    private void ApplyHighPassFilter()
    {
        if (sourceImage != null)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            int stride = width * 4;
            byte[] pixelData = new byte[height * stride];
            sourceImage.CopyPixels(pixelData, stride, 0);

            byte[] newPixelData = new byte[pixelData.Length];
            Array.Copy(pixelData, newPixelData, pixelData.Length);

            int[] highPassKernel = {
                0, -1, 0,
                -1, 5, -1,
                0, -1, 0
            };

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int offset = y * stride + x * 4;

                    int totalR = 0, totalG = 0, totalB = 0;
                    int kernelIndex = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int neighborOffset = (y + i) * stride + (x + j) * 4;

                            totalR += pixelData[neighborOffset + 2] * highPassKernel[kernelIndex];
                            totalG += pixelData[neighborOffset + 1] * highPassKernel[kernelIndex];
                            totalB += pixelData[neighborOffset] * highPassKernel[kernelIndex];

                            kernelIndex++;
                        }
                    }

                    byte newR = (byte)Math.Max(0, Math.Min(255, totalR));
                    byte newG = (byte)Math.Max(0, Math.Min(255, totalG));
                    byte newB = (byte)Math.Max(0, Math.Min(255, totalB));

                    newPixelData[offset + 2] = newR;
                    newPixelData[offset + 1] = newG;
                    newPixelData[offset] = newB;
                }
            }

            BitmapSource processedBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, newPixelData, stride);
            if (isOriginalImageChanged) { sourceImage = ConvertToBitmapImage(processedBitmap); }
            MainImage.Source = processedBitmap;
        }
        else
        {
            MessageBox.Show("Please load an image first.");
        }
    }

    public BitmapImage ConvertToBitmapImage(BitmapSource bitmapSource)
    {
        if (bitmapSource != null)
        {
            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        else
        {
            MessageBox.Show("An error occurred.");
            return null;
        }
    }

    private void OnEffectsOption_Checked(object sender, RoutedEventArgs e)
    {
        if (EffectsPanel != null && TransformPanel != null && valueFieldGrid != null)
        {
            EffectsPanel.Visibility = Visibility.Visible;
            TransformPanel.Visibility = Visibility.Collapsed;
            valueFieldGrid.Visibility = Visibility.Collapsed;
        }
    }

    private void OnTransformOption_Checked(object sender, RoutedEventArgs e)
    {
        if (EffectsPanel != null && TransformPanel != null && valueFieldGrid != null)
        {
            EffectsPanel.Visibility = Visibility.Collapsed;
            TransformPanel.Visibility = Visibility.Visible;
            valueFieldGrid.Visibility = Visibility.Visible;
        }
    }
}
