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

namespace PPM;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private TranslateTransform imgTranslate;
    private ScaleTransform imgScale;
    private Point previousMousePos;
    
    public MainWindow()
    {
        InitializeComponent();
        var transforms = new TransformGroup();
        imgScale = new ScaleTransform();
        imgTranslate = new TranslateTransform();
        transforms.Children.Add(imgScale);
        transforms.Children.Add(imgTranslate);
        displayedImage.RenderTransform = transforms;

        displayedImage.PreviewMouseWheel += ImageZoom;
        displayedImage.PreviewMouseLeftButtonDown += StartDragging;
        displayedImage.PreviewMouseLeftButtonUp += StopDragging;
        displayedImage.PreviewMouseMove += HandleImageMove;
        displayedImage.MouseMove += DisplayPixelInfo;
    }
    private void ImageZoom(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            imgScale.ScaleX *= zoomFactor;
            imgScale.ScaleY *= zoomFactor;
            e.Handled = true;
        }
    }

    private void StartDragging(object sender, MouseButtonEventArgs e)
    {
        previousMousePos = e.GetPosition(displayedImage);
        displayedImage.CaptureMouse();
    }

    private void StopDragging(object sender, MouseButtonEventArgs e)
    {
        displayedImage.ReleaseMouseCapture();
    }

    private void HandleImageMove(object sender, MouseEventArgs e)
    {
        if (displayedImage.IsMouseCaptured)
        {
            Point currentPos = e.GetPosition(displayedImage);
            if (imgScale.ScaleX > 1 && imgScale.ScaleY > 1)
            {
                double moveX = currentPos.X - previousMousePos.X;
                double moveY = currentPos.Y - previousMousePos.Y;
                previousMousePos = currentPos;
                imgTranslate.X += moveX;
                imgTranslate.Y += moveY;
            }
        }
    }

    private void DisplayPixelInfo(object sender, MouseEventArgs e)
    {
        var mousePos = e.GetPosition(displayedImage);
        var imgSource = displayedImage.Source as BitmapSource;

        if (imgSource != null)
        {
            int x = (int)(mousePos.X * (imgSource.PixelWidth / displayedImage.ActualWidth));
            int y = (int)(mousePos.Y * (imgSource.PixelHeight / displayedImage.ActualHeight));

            if (x >= 0 && x < imgSource.PixelWidth && y >= 0 && y < imgSource.PixelHeight)
            {
                byte[] pixelData = new byte[4];
                var crop = new CroppedBitmap(imgSource, new Int32Rect(x, y, 1, 1));
                crop.CopyPixels(pixelData, 4, 0);
                var color = Color.FromArgb(pixelData[3], pixelData[0], pixelData[1], pixelData[2]);
                pixelInfoTextBlock.Text = $"R: {color.R}, G: {color.G}, B: {color.B}, X: {x}, Y: {y}";
            }
        }
    }

    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "PPM files|*.ppm|JPEG files|*.jpg;*.jpeg";

        if (openFileDialog.ShowDialog() == true)
        {
            string path = openFileDialog.FileName;
            if (path.EndsWith(".ppm", StringComparison.OrdinalIgnoreCase))
            {
                string ppmType = GetPPMType(path);
                if (ppmType == "P3")
                {
                    LoadAndDisplayPPMP3(path);
                }
                else if (ppmType == "P6")
                {
                    LoadAndDisplayPPMP6(path);
                }
                else
                {
                    MessageBox.Show("Unsupported PPM format.");
                }
            }
            else if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                DisplayJPEG(path);
            }
            else
            {
                MessageBox.Show("Unsupported file format.");
            }
        }
    }

    private void DisplayJPEG(string path)
    {
        try
        {
            var bitmap = new BitmapImage(new Uri(path));
            displayedImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading JPEG file: {ex.Message}");
        }
    }

    private void SaveToJPEG_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "JPEG files|*.jpg";

        if (saveFileDialog.ShowDialog() == true)
        {
            string path = saveFileDialog.FileName;

            try
            {
                var source = (BitmapSource)displayedImage.Source;
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = int.TryParse(qualityTextBox.Text, out int quality) ? quality : 95;
                encoder.Frames.Add(BitmapFrame.Create(source));

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                MessageBox.Show("Image saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving JPEG file: {ex.Message}");
            }
        }
    }

    private string GetPPMType(string path)
    {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (var reader = new StreamReader(fs))
        {
            return reader.ReadLine().Trim();
        }
    }

    private void LoadAndDisplayPPMP3(string filePath)
{
    try
    {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = new StreamReader(fs))
        {
            string format = reader.ReadLine();
            int width = 0, height = 0, maxValue = 0;
            string dimensionsLine = string.Empty;
            string leftoverData = string.Empty;

            while ((dimensionsLine = reader.ReadLine()) != null)
            {
                int commentPos = dimensionsLine.IndexOf('#');
                if (commentPos >= 0)
                    dimensionsLine = dimensionsLine.Substring(0, commentPos);

                string[] tokens = dimensionsLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string token in tokens)
                {
                    if (int.TryParse(token, out int value))
                    {
                        if (width == 0) width = value;
                        else if (height == 0) height = value;
                        else if (maxValue == 0) maxValue = value;
                        else leftoverData += token + '\n';
                    }
                }

                if (width > 0 && height > 0 && maxValue > 0)
                    break;
            }

            WriteableBitmap image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            var allPixels = new List<byte>();

            while (true)
            {
                char[] buffer = new char[4096];
                int bytesRead = reader.ReadBlock(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string blockData = leftoverData + new string(buffer, 0, bytesRead);
                leftoverData = HandleLeftover(buffer, bytesRead);

                if (blockData.Contains('#'))
                {
                    blockData = RemoveAllComments(blockData);
                }

                var lines = blockData.Split('\n');
                foreach (var line in lines)
                {
                    var tokenized = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var token in tokenized)
                    {
                        string parsedValue = ParseValue(token, maxValue);
                        allPixels.Add(byte.Parse(parsedValue));
                    }
                }
            }

            image.WritePixels(new Int32Rect(0, 0, width, height), allPixels.ToArray(), width * 3, 0);
            displayedImage.Source = image;
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Błąd podczas otwierania pliku: {ex.Message}");
    }
}

private string HandleLeftover(char[] buffer, int bytesRead)
{
    int lastNewline = Array.LastIndexOf(buffer, '\n');
    return lastNewline >= 0 ? new string(buffer, lastNewline + 1, bytesRead - lastNewline - 1) : string.Empty;
}

private string RemoveAllComments(string data)
{
    while (data.Contains('#'))
    {
        data = RemoveCommentsIfNeeded(data);
    }
    return data;
}

private string ParseValue(string token, int maxValue)
{
    if (maxValue > 255)
    {
        double scalingFactor = 255.0 / maxValue;
        return ((int)(int.Parse(token) * scalingFactor)).ToString();
    }
    return token;
}

    private void LoadAndDisplayPPMP6(string filePath)
{
    try
    {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            string format = Encoding.ASCII.GetString(reader.ReadBytes(2));

            int width = 0, height = 0, maxValue = 0;
            string dimensionsLine;

            while ((dimensionsLine = ReadLine(reader)) != null)
            {
                dimensionsLine = RemoveCommentsIfNeeded(dimensionsLine);

                string[] tokens = dimensionsLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    if (int.TryParse(token, out int value))
                    {
                        if (width == 0) width = value;
                        else if (height == 0) height = value;
                        else if (maxValue == 0)
                        {
                            maxValue = value;
                            break;
                        }
                    }
                }

                if (width > 0 && height > 0 && maxValue > 0)
                    break;
            }

            var image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            int dataSize = width * height * 3;
            byte[] allPixels = new byte[dataSize];
            int totalBytesRead = 0;

            while (totalBytesRead < dataSize)
            {
                int bytesToRead = Math.Min(dataSize - totalBytesRead, 4096);
                int bytesRead = reader.Read(allPixels, totalBytesRead, bytesToRead);
                if (bytesRead == 0) break;
                totalBytesRead += bytesRead;
            }

            image.WritePixels(new Int32Rect(0, 0, width, height), allPixels, width * 3, 0);
            displayedImage.Source = image;
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Błąd podczas otwierania pliku: {ex.Message}");
    }
}

private string RemoveCommentsIfNeeded(string line)
{
    int commentIndex = line.IndexOf('#');
    return commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
}

    private string ReadLine(BinaryReader reader)
    {
        List<byte> buffer = new List<byte>();
        byte currentByte;

        while ((currentByte = reader.ReadByte()) != 10) // 10 == newline in ASCII
        {
            buffer.Add(currentByte);
        }

        return Encoding.ASCII.GetString(buffer.ToArray()).Trim();
    }
}