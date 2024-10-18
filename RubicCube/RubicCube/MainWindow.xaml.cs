using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf;

namespace RubicCube;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool isInternalChange = false;
    private Point previousMousePosition;
    private bool isDragging = false;
    private ModelVisual3D visualModel;
    private double horizontalAngle = 0.0;
    private double verticalAngle = 0.0;
    public MainWindow()
    {
        InitializeComponent();
        InitializeComponent();
        BuildRGBCube();
        viewport3D.RotateGesture = new MouseGesture(MouseAction.LeftClick);
        viewport3D.PanGesture = new MouseGesture(MouseAction.RightClick);
        viewport3D.ZoomGesture = new MouseGesture(MouseAction.MiddleClick);
        viewport3D.CameraRotationMode = CameraRotationMode.Turntable;
        viewport3D.MouseDown += OnViewportMouseDown;
        viewport3D.MouseMove += OnViewportMouseMove;
        viewport3D.MouseUp += OnViewportMouseUp;
    }

    private void OnRGBtoCMYKChecked(object sender, RoutedEventArgs e)
    {
        if (RGBInputsPanel != null && CMYKInputsPanel != null)
        {
            Grid.SetRow(RGBInputsPanel, 1);
            Grid.SetRow(CMYKInputsPanel, 2);
        }
    }

    private void OnCMYKtoRGBChecked(object sender, RoutedEventArgs e)
    {
        if (RGBInputsPanel != null && CMYKInputsPanel != null)
        {
            Grid.SetRow(CMYKInputsPanel, 1);
            Grid.SetRow(RGBInputsPanel, 2);
        }
    }

    private void OnColorSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!isInternalChange)
            RefreshConvertedColor(false);
    }

    private void OnColorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!isInternalChange)
            RefreshConvertedColor(true);
    }

    private void RefreshConvertedColor(bool isTextInput)
    {
        if (convertedColorDisplay != null)
        {
            int r, g, b;
            if (RGBtoCMYKCheckBox.IsChecked == true)
            {
                if (!isTextInput)
                {
                    isInternalChange = true;
                    r = (int)redSlider.Value;
                    g = (int)greenSlider.Value;
                    b = (int)blueSlider.Value;
                    redTextBox.Text = r.ToString();
                    greenTextBox.Text = g.ToString();
                    blueTextBox.Text = b.ToString();
                    isInternalChange = false;
                }
                else
                {
                    isInternalChange = true;
                    int.TryParse(redTextBox.Text, out r);
                    int.TryParse(greenTextBox.Text, out g);
                    int.TryParse(blueTextBox.Text, out b);

                    redSlider.Value = r;
                    greenSlider.Value = g;
                    blueSlider.Value = b;
                    isInternalChange = false;
                }

                var rgb = new[] { r, g, b }.Select(v => v / 255.0f).ToArray();
                var k = 1 - rgb.Max();
                var c = (1 - rgb[0] - k) / (1 - k);
                var m = (1 - rgb[1] - k) / (1 - k);
                var y = (1 - rgb[2] - k) / (1 - k);

                cyanSlider.Value = Math.Round(c * 100);
                magentaSlider.Value = Math.Round(m * 100);
                yellowSlider.Value = Math.Round(y * 100);
                blackSlider.Value = Math.Round(k * 100);

                cyanTextBox.Text = cyanSlider.Value.ToString();
                magentaTextBox.Text = magentaSlider.Value.ToString();
                yellowTextBox.Text = yellowSlider.Value.ToString();
                blackTextBox.Text = blackSlider.Value.ToString();

                convertedColorDisplay.Fill = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
                colorCodeTextBlock.Text = $"#{r:X2}{g:X2}{b:X2}";
            }
            else if (CMYKtoRGBCheckBox.IsChecked == true)
            {
                int c, m, y, k;
                if (!isTextInput)
                {
                    isInternalChange = true;
                    c = (int)cyanSlider.Value;
                    m = (int)magentaSlider.Value;
                    y = (int)yellowSlider.Value;
                    k = (int)blackSlider.Value;

                    cyanTextBox.Text = c.ToString();
                    magentaTextBox.Text = m.ToString();
                    yellowTextBox.Text = y.ToString();
                    blackTextBox.Text = k.ToString();
                    isInternalChange = false;
                }
                else
                {
                    isInternalChange = true;
                    int.TryParse(cyanTextBox.Text, out c);
                    int.TryParse(magentaTextBox.Text, out m);
                    int.TryParse(yellowTextBox.Text, out y);
                    int.TryParse(blackTextBox.Text, out k);

                    cyanSlider.Value = c;
                    magentaSlider.Value = m;
                    yellowSlider.Value = y;
                    blackSlider.Value = k;
                    isInternalChange = false;
                }

                r = (int)(255 * (1 - Math.Min(1, c / 100.0 * (1 - k / 100.0) + k / 100.0)));
                g = (int)(255 * (1 - Math.Min(1, m / 100.0 * (1 - k / 100.0) + k / 100.0)));
                b = (int)(255 * (1 - Math.Min(1, y / 100.0 * (1 - k / 100.0) + k / 100.0)));

                redSlider.Value = r;
                greenSlider.Value = g;
                blueSlider.Value = b;

                redTextBox.Text = r.ToString();
                greenTextBox.Text = g.ToString();
                blueTextBox.Text = b.ToString();

                convertedColorDisplay.Fill = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
                colorCodeTextBlock.Text = $"#{r:X2}{g:X2}{b:X2}";
            }
        }
    }

    private void BuildRGBCube()
    {
        int cubeSize = 10;
        viewport3D.Children.Add(new DefaultLights());

        for (int x = 0; x < cubeSize; x++)
        {
            for (int y = 0; y < cubeSize; y++)
            {
                for (int z = 0; z < cubeSize; z++)
                {
                    Color color = Color.FromRgb((byte)(x * 255 / cubeSize), (byte)(y * 255 / cubeSize), (byte)(z * 255 / cubeSize));
                    var material = new DiffuseMaterial(new SolidColorBrush(color));

                    var cube = new BoxVisual3D
                    {
                        Center = new Point3D(x, y, z),
                        Length = 1.0,
                        Width = 1.0,
                        Height = 1.0,
                        Material = material
                    };
                    viewport3D.Children.Add(cube);
                }
            }
        }
    }

    private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            previousMousePosition = e.GetPosition(viewport3D);
            isDragging = true;
        }
    }

    private void OnViewportMouseUp(object sender, MouseButtonEventArgs e)
    {
        isDragging = false;
    }

    private void OnViewportMouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging && visualModel != null)
        {
            Point currentMousePos = e.GetPosition(viewport3D);
            double dx = currentMousePos.X - previousMousePosition.X;
            double dy = currentMousePos.Y - previousMousePosition.Y;
            previousMousePosition = currentMousePos;

            horizontalAngle += dx;
            verticalAngle += dy;

            var rotationTransform = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 1, 0), horizontalAngle),
                new Point3D(0, 0, 0)
            );

            visualModel.Transform = rotationTransform;
        }
    }
}