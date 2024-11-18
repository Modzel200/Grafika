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

namespace GreenColor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private WriteableBitmap currentBitmap;
    private BitmapImage originalImage;
    private bool isOriginalImageChanged = false;
    private int width;
    private int height;
    private int[,] mask;
    public MainWindow()
    {
        InitializeComponent();
        InitializeImageTransform();
    }
    private void InitializeImageTransform()
    {
        TransformGroup group = new TransformGroup();
        ScaleTransform scaleTransform = new ScaleTransform();
        group.Children.Add(scaleTransform);

        TranslateTransform translateTransform = new TranslateTransform();
        group.Children.Add(translateTransform);

        displayedImage.RenderTransform = group;
    }

    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Pliki JPEG|*.jpg;*.jpeg"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            if (filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                LoadAndDisplayJPEG(filePath);
            }
            else
            {
                MessageBox.Show("Nieobsługiwany format pliku.");
            }
        }
    }

    private void LoadAndDisplayJPEG(string filePath)
    {
        try
        {
            BitmapImage image = new BitmapImage(new Uri(filePath));
            originalImage = image;

            var Width = originalImage.PixelWidth;
            var Height = originalImage.PixelHeight;
            var bytesPerPixel = (originalImage.Format.BitsPerPixel + 7) / 8;
            var stride = Width * bytesPerPixel;

            byte[] pixelData = new byte[Height * stride];
            originalImage.CopyPixels(pixelData, stride, 0);

            var Pixels = pixelData;

            currentBitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Rgb24, null);
            currentBitmap.WritePixels(new Int32Rect(0, 0, Width, Height), pixelData, 3 * Width, 0);

            width = currentBitmap.PixelWidth;
            height = currentBitmap.PixelHeight;

            displayedImage.Source = originalImage;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas wczytywania pliku JPEG: {ex.Message}");
        }
    }

    private byte[] PerformDilatation(byte[] pixels)
    {
        var newPixels = new byte[width * height * 3];
        int offset = (mask.GetLength(0) - 1) / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width + x) * 3;
                byte maxR = 0, maxG = 0, maxB = 0;

                for (int offsetY = -offset; offsetY <= offset; offsetY++)
                {
                    for (int offsetX = -offset; offsetX <= offset; offsetX++)
                    {
                        int pixelY = y + offsetY;
                        int pixelX = x + offsetX;

                        if (pixelY >= 0 && pixelX >= 0 && pixelX < width && pixelY < height)
                        {
                            int currentIndex = (pixelY * width + pixelX) * 3;
                            if (mask[offsetY + offset, offsetX + offset] == 1)
                            {
                                maxR = Math.Max(maxR, pixels[currentIndex]);
                                maxG = Math.Max(maxG, pixels[currentIndex + 1]);
                                maxB = Math.Max(maxB, pixels[currentIndex + 2]);
                            }
                        }
                    }
                }

                newPixels[index] = maxR;
                newPixels[index + 1] = maxG;
                newPixels[index + 2] = maxB;
            }
        }

        return newPixels;
    }

    private byte[] PerformErosion(byte[] pixels)
    {
        var newPixels = new byte[width * height * 3];
        int offset = (mask.GetLength(0) - 1) / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width + x) * 3;
                byte minR = 255, minG = 255, minB = 255;

                for (int offsetY = -offset; offsetY <= offset; offsetY++)
                {
                    for (int offsetX = -offset; offsetX <= offset; offsetX++)
                    {
                        int pixelY = y + offsetY;
                        int pixelX = x + offsetX;

                        if (pixelY >= 0 && pixelX >= 0 && pixelX < width && pixelY < height)
                        {
                            int currentIndex = (pixelY * width + pixelX) * 3;
                            if (mask[offsetY + offset, offsetX + offset] == 1)
                            {
                                minR = Math.Min(minR, pixels[currentIndex]);
                                minG = Math.Min(minG, pixels[currentIndex + 1]);
                                minB = Math.Min(minB, pixels[currentIndex + 2]);
                            }
                        }
                    }
                }

                newPixels[index] = minR;
                newPixels[index + 1] = minG;
                newPixels[index + 2] = minB;
            }
        }

        return newPixels;
    }

    private int[,] ParseMask()
    {
        string[] maskValues = StructuringElementSize.Text.Split(' ');

        int kernelSize = (int)Math.Sqrt(maskValues.Length);
        if (kernelSize % 2 == 0 || kernelSize < 3)
        {
            MessageBox.Show("Nieprawidłowa liczba elementów konstrukcyjnych, podaj minimum 9 elementów.");
            return null;
        }

        int[,] kernel = new int[kernelSize, kernelSize];
        int row = 0, col = 0;

        foreach (var value in maskValues)
        {
            if (int.TryParse(value, out kernel[row, col]) && kernel[row, col] >= 0 && kernel[row, col] <= 1)
            {
                row++;
                if (row == kernelSize)
                {
                    row = 0;
                    col++;
                }
            }
            else
            {
                MessageBox.Show($"Nieprawidłowe dane wejściowe maski: {value}");
                return null;
            }
        }

        return kernel;
    }

    private void Count_Click(object sender, RoutedEventArgs e)
    {
        if (currentBitmap == null) return;

        mask = ParseMask();
        if (mask == null) return;

        byte[] pixels = new byte[width * height * 3];
        byte[] newPixels = new byte[width * height * 3];

        currentBitmap.CopyPixels(pixels, width * 3, 0);

        double red = RedSlider.Value, green = GreenSlider.Value, blue = BlueSlider.Value;
        var colorValue = System.Drawing.Color.FromArgb((byte)red, (byte)green, (byte)blue);
        if (!int.TryParse(Tolerance.Text, out int tolerance))
        {
            MessageBox.Show("Invalid tolerance value.");
            return;
        }

        double valueFloor = Math.Round(colorValue.GetHue()) - tolerance;
        double valueCeiling = Math.Round(colorValue.GetHue()) + tolerance;

        int pixelCount = 0;
        for (int i = 0; i < width * height * 3;)
        {
            var r = pixels[i];
            var g = pixels[i + 1];
            var b = pixels[i + 2];
            var color = System.Drawing.Color.FromArgb(r, g, b);

            double hue = color.GetHue();
            if (hue <= valueCeiling && hue >= valueFloor)
            {
                newPixels[i++] = 255;
                newPixels[i++] = 255;
                newPixels[i++] = 255;
            }
            else
            {
                newPixels[i++] = 0;
                newPixels[i++] = 0;
                newPixels[i++] = 0;
            }
        }

        newPixels = PerformErosion(newPixels);
        newPixels = newPixels != null ? PerformDilatation(newPixels) : null;

        if (newPixels != null)
        {
            newPixels = PerformDilatation(newPixels);
            pixelCount = 0;
            for (int i = 0; i < width * height * 3; i += 3)
            {
                if (newPixels[i] == 255)
                {
                    pixelCount++;
                }
            }

            var newBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            newBitmap.WritePixels(new Int32Rect(0, 0, width, height), newPixels, width * 3, 0);
            currentBitmap = newBitmap;
            displayedImage.Source = currentBitmap;

            double pixelPercent = (double)pixelCount / (width * height) * 100;
            MessageBox.Show($"Procent z \n R: {red:F2} \n G: {green:F2} \n B: {blue:F2} \n to {pixelPercent:F2}%");
        }
    }
}