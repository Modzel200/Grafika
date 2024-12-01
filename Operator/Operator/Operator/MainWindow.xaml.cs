using System.Runtime.InteropServices;
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

namespace Operator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private WriteableBitmap currentBitmap;
    private WriteableBitmap snapshot;
    private BitmapImage originalImage;
    private bool IsChangeOriginal = false;
    int width;
    int height;
    private const int BLACK = (255 << 24) | (0 << 16) | (0 << 8) | 0,
            WHITE = (255 << 24) | (255 << 16) | (255 << 8) | 255;
    public MainWindow()
    {
        InitializeComponent();
        TransformGroup group = new TransformGroup();

        ScaleTransform xform = new ScaleTransform();
        group.Children.Add(xform);

        TranslateTransform tt = new TranslateTransform();
        group.Children.Add(tt);

        displayedImage.RenderTransform = group;
    }

     private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Pliki JPEG|*.jpg;*.jpeg";

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
                MessageBox.Show("Błąd podczas wczytywania pliku JPEG: " + ex.Message);
            }
        }

    private void Dilatation_Click(object sender, RoutedEventArgs e)
    {
        if (currentBitmap != null)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
            WriteableBitmap dilatedBitmap = Dilation(writeableBitmap, WHITE, BLACK);
            displayedImage.Source = dilatedBitmap;
        }
    }
    public static WriteableBitmap Dilation(WriteableBitmap inputBitmap, int expandedColor, int shrunkenColor)
    {
        var bmp = (WriteableBitmap)inputBitmap;
        int wid = bmp.PixelWidth, hei = bmp.PixelHeight;
        int[] outBuf = new int[wid * hei];
        bmp.Lock();
        byte expVal = (byte)(expandedColor & 255),
            shrVal = (byte)(shrunkenColor & 255);
        unsafe
        {
            int* pIBeg = (int*)bmp.BackBuffer;
            int i = 0;
            for (int iY = 0; iY < hei; ++iY)
            {
                for (int iX = 0; iX < wid; ++iX)
                {
                    outBuf[i] = shrunkenColor;
                    for (int seY = -1; seY <= 1; ++seY)
                    {
                        for (int seX = -1; seX <= 1; ++seX)
                        {
                            int x = iX + seX, y = iY + seY;
                            byte pixVal = shrVal;
                            if (x >= 0 && x < wid && y >= 0 && y < hei)
                            {
                                pixVal = (byte)(*(pIBeg + x + wid * y) & 255);
                            }
                            if (pixVal == expVal)
                            {
                                outBuf[i] = expandedColor;
                                goto OUT_OF_STRUCTURED_ELEMENT;
                            }
                        }
                    }
                OUT_OF_STRUCTURED_ELEMENT:
                    ++i;
                }
            }
            Marshal.Copy(outBuf, 0, bmp.BackBuffer, outBuf.Length);
        }
        bmp.AddDirtyRect(new Int32Rect(0, 0, wid, hei));
        bmp.Unlock();
        return bmp;
    }

    private void Erosion_Click(object sender, RoutedEventArgs e)
        {
        if (currentBitmap != null)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
            WriteableBitmap dilatedBitmap = Dilation(writeableBitmap, BLACK, WHITE);
            displayedImage.Source = dilatedBitmap;
        }
    }

        private void HitOrMiss_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmap != null)
            {
                var newBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
                var pixels = GetPixels();

                var newPixels = PerformHitOrMiss(pixels, width, height);
                if (newPixels != null)
                {
                    newBitmap.WritePixels(new Int32Rect(0, 0, width, height), newPixels, width * 3, 0);
                    if (IsChangeOriginal)
                        currentBitmap = newBitmap;
                    displayedImage.Source = newBitmap;
                }
            }
            else
            {
                MessageBox.Show("Nie załadowano obrazu");
                return;
            }

        }

        private void Thinning_Click(object sender, RoutedEventArgs e)
        {
        if (currentBitmap != null)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
            var bmp = (WriteableBitmap)writeableBitmap;
            int wid = bmp.PixelWidth, hei = bmp.PixelHeight;
            int[] outBuf = new int[wid * hei];
            const int seWid = 3, seHei = 3;
            const byte o = 255, z = 0, n = 128;
            byte[][] strEls = new byte[8][];

            strEls[0] = new byte[] { o, o, o, n, o, n, z, z, z }; // B1
            strEls[1] = new byte[] { o, n, z, o, o, z, o, n, z }; // B2
            strEls[2] = new byte[] { z, z, z, n, o, n, o, o, o }; // B3
            strEls[3] = new byte[] { z, n, o, z, o, o, z, n, o }; // B4
            strEls[4] = new byte[] { o, o, n, o, o, z, n, z, z }; // B5
            strEls[5] = new byte[] { n, z, z, o, o, z, o, o, n }; // B6
            strEls[6] = new byte[] { z, z, n, z, o, o, n, o, o }; // B7
            strEls[7] = new byte[] { n, o, o, z, o, o, z, z, n }; // B8


            strEls[4][0] = strEls[5][6] = strEls[6][8] = strEls[7][2] = n;
            bmp.Lock();
            unsafe
            {
                int* pIBeg = (int*)bmp.BackBuffer;
                for (int i = 0; i < strEls.Length; ++i)
                {
                    HitOrMissTransform(strEls[i], seWid, seHei, pIBeg, outBuf, wid, hei,
                        BLACK);
                    Marshal.Copy(outBuf, 0, bmp.BackBuffer, outBuf.Length);
                }
            }
            bmp.AddDirtyRect(new Int32Rect(0, 0, wid, hei));
            bmp.Unlock();
            displayedImage.Source = bmp;
        }
    }
    unsafe private void HitOrMissTransform(byte[] structuringElement,
            int seWid, int seHei, int* src, int[] dst, int wid, int hei,
            int expandedColor)
    {
        int halfSeWid = seWid / 2, halfSeHei = seHei / 2;
        int oI = 0;
        int* pSrc = src;
        for (int iY = 0; iY < hei; ++iY)
        {
            for (int iX = 0; iX < wid; ++iX)
            {
                dst[oI] = *(pSrc++);
                int seI = 0;
                for (int seY = -halfSeHei; seY <= halfSeHei; ++seY)
                {
                    for (int seX = -halfSeWid; seX <= halfSeWid; ++seX)
                    {
                        int x = iX + seX, y = iY + seY;
                        byte pixVal = 0;
                        if (x >= 0 && x < wid && y >= 0 && y < hei)
                            pixVal = (byte)(*(src + x + wid * y) & 255);

                        if (structuringElement[seI] == 128)
                        {
                            ++seI;
                            continue;
                        }
                        if (pixVal != structuringElement[seI++])
                            goto OUT_OF_STRUCTURED_ELEMENT;
                    }
                }
                dst[oI] = expandedColor;
            OUT_OF_STRUCTURED_ELEMENT:
                ++oI;
            }
        }
    }

    private void Thickening_Click(object sender, RoutedEventArgs e)
        {
        if (currentBitmap != null)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
            var bmp = (WriteableBitmap)writeableBitmap;
            int wid = bmp.PixelWidth, hei = bmp.PixelHeight;
            int[] outBuf = new int[wid * hei];
            const int seWid = 3, seHei = 3;
            const byte o = 255, z = 0, n = 128;
            byte[][] strEls = new byte[8][];
            strEls[0] = new byte[] { o, o, n, o, z, n, o, n, z }; // B1
            strEls[1] = new byte[] { n, n, z, o, z, n, o, o, o }; // B2
            strEls[2] = new byte[] { z, n, o, n, z, o, n, o, o }; // B3
            strEls[3] = new byte[] { o, o, o, n, z, o, z, n, n }; // B4
            strEls[4] = new byte[] { n, o, o, n, z, o, z, n, o }; // B5
            strEls[5] = new byte[] { o, o, o, o, z, n, n, n, z }; // B6
            strEls[6] = new byte[] { o, n, z, o, z, n, o, o, n }; // B7
            strEls[7] = new byte[] { z, n, n, n, z, o, o, o, o }; // B8
            bmp.Lock();
            unsafe
            {
                int* pIBeg = (int*)bmp.BackBuffer;
                for (int i = 0; i < strEls.Length; ++i)
                {
                    HitOrMissTransform(strEls[i], seWid, seHei, pIBeg, outBuf, wid, hei,
                        WHITE);
                    Marshal.Copy(outBuf, 0, bmp.BackBuffer, outBuf.Length);
                }
            }
            bmp.AddDirtyRect(new Int32Rect(0, 0, wid, hei));
            bmp.Unlock();
            displayedImage.Source = bmp;
        }
    }

        private void Opening_Click(object sender, RoutedEventArgs e)
        {
        if (currentBitmap != null)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
            WriteableBitmap dilatedBitmap = Dilation(writeableBitmap, BLACK, WHITE);
            dilatedBitmap = Dilation(dilatedBitmap, WHITE, BLACK);
            displayedImage.Source = dilatedBitmap;
        }
    }

        private void Closing_Click(object sender, RoutedEventArgs e)
        {
        if (currentBitmap != null)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
            WriteableBitmap dilatedBitmap = Dilation(writeableBitmap, WHITE, BLACK);
            dilatedBitmap = Dilation(dilatedBitmap, BLACK, WHITE);
            displayedImage.Source = dilatedBitmap;
        }
    }

        private byte[] PerformDilatation(byte[] pixels, int width, int height)
        {
            var newPixels = new byte[width * height * 3];
            var mask = ParseMask();
            if (mask != null)
            {
                int offset = ((mask.GetLength(0) - 1) / 2);
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
            else
            {
                MessageBox.Show("Nie podano maski");
                return null;
            }
        }

        private byte[] PerformErosion(byte[] pixels, int width, int height)
        {
            var newPixels = new byte[width * height * 3];
            var mask = ParseMask();
            if (mask != null)
            {

                int offset = ((mask.GetLength(0) - 1) / 2);
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
            else
            {
                MessageBox.Show("Nie podano maski");
                return null;
            }
        }

        private byte[] PerformHitOrMiss(byte[] pixels, int width, int height)
        {
            var newPixels = new byte[width * height * 3];
            var hitMask = ParseMask();
            if (hitMask != null)
            {
                var maskSize = hitMask.GetLength(0);
                var missMask = new int[maskSize, maskSize];
                for (int i = 0; i < maskSize; i++)
                {
                    for (int j = 0; j < maskSize; j++)
                    {
                        missMask[i, j] = hitMask[i, j] == 1 ? 0 : hitMask[i, j] == 0 ? 1 : 2;
                    }
                }

                int offset = ((hitMask.GetLength(0) - 1) / 2);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 3;
                        bool HitAndMiss = true;
                        for (int offsetY = -offset; offsetY <= offset; offsetY++)
                        {
                            for (int offsetX = -offset; offsetX <= offset; offsetX++)
                            {
                                int pixelY = y + offsetY;
                                int pixelX = x + offsetX;

                                if (pixelY >= 0 && pixelX >= 0 && pixelX < width && pixelY < height)
                                {
                                    int currentIndex = (pixelY * width + pixelX) * 3;
                                    if (hitMask[offsetY + offset, offsetX + offset] == 1 && pixels[currentIndex] != 255)
                                    {
                                        HitAndMiss = false;
                                        break;
                                    }
                                    else if (missMask[offsetY + offset, offsetX + offset] == 1 && pixels[currentIndex] != 0)
                                    {
                                        HitAndMiss = false;
                                        break;
                                    }
                                }
                            }
                        }

                        newPixels[index] = HitAndMiss ? (byte)255 : (byte)0;
                        newPixels[index + 1] = HitAndMiss ? (byte)255 : (byte)0;
                        newPixels[index + 2] = HitAndMiss ? (byte)255 : (byte)0;
                    }
                }
                return newPixels;
            }
            else
            {
                MessageBox.Show("Nie podano maski");
                return null;
            }
        }

        private byte[] GetPixels()
        {
            int width = currentBitmap.PixelWidth;
            int height = currentBitmap.PixelHeight;
            var pixels = new byte[width * height * 3];
            currentBitmap.CopyPixels(pixels, width * 3, 0);
            for (int i = 0; i < 3 * width * height;)
            {
                var r = (int)pixels[i];
                var g = (int)pixels[i + 1];
                var b = (int)pixels[i + 2];

                var gray = (r + g + b) / 3;

                pixels[i++] = gray > 128 ? (byte)255 : (byte)0;
                pixels[i++] = gray > 128 ? (byte)255 : (byte)0;
                pixels[i++] = gray > 128 ? (byte)255 : (byte)0;

            }
            return pixels;
        }

        private int[,] ParseMask()
        {
            string[] mask = StructuringElementSize.Text.Split(' ');

            int row = 0, col = 0;
            var tmpSize = Math.Sqrt(mask.Length);
            if (tmpSize % 1 != 0 || tmpSize % 2 == 0 || tmpSize < 3)
            {
                MessageBox.Show($"Nieprawidłowa liczba elementów konstrukcyjnych, podaj minimum 9 elementów.\n Podano {mask.Length}");
                return null;
            }

            int kernelSize = (int)tmpSize;
            var kernel = new int[kernelSize, kernelSize];
            foreach (var val in mask)
            {
                var parsingResult = int.TryParse(val, out kernel[row, col]);
                if (!parsingResult || kernel[row, col] > 2 || kernel[row, col] < 0)
                {
                    MessageBox.Show($"Nieprawidłowe dane wejściowe maski: {val}");
                    return null;
                }
                row++;
                if (row == kernelSize)
                {
                    row = 0; col++;
                }
            }

            return kernel;
        }
}