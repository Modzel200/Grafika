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
using System.Xml.Serialization;
using Canvas.Models;

namespace Canvas;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ShapeType selectedShape = ShapeType.Line;
    private List<Shape?> shapes = new List<Shape?>();
    private Point startPoint;
    private Shape? currentShape;
    private bool isDrawing = false;
    private bool isDragging = false;
    private bool isResizing = false;
    private ResizeDirection resizeDirection = ResizeDirection.None;
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Canvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        canvas.Focus();
        startPoint = e.GetPosition(canvas);
        currentShape = GetShapeUnderMouse(startPoint);

        if (currentShape != null)
        {
            isDragging = false;
            isDrawing = false;
            isResizing = false;

            if (IsPointOnEllipseEdge(startPoint, currentShape))
            {
                isResizing = true;
                resizeDirection = ResizeDirection.Ellipse;
            }
            else if (IsPointNearTopEdge(startPoint, currentShape))
            {
                isResizing = true;
                resizeDirection = IsPointNearLeftEdge(startPoint, currentShape) ? 
                    ResizeDirection.TopLeft : 
                    IsPointNearRightEdge(startPoint, currentShape) ? 
                        ResizeDirection.TopRight : ResizeDirection.Top;
            }
            else if (IsPointNearBottomEdge(startPoint, currentShape))
            {
                isResizing = true;
                resizeDirection = IsPointNearLeftEdge(startPoint, currentShape) ? 
                    ResizeDirection.BottomLeft : 
                    IsPointNearRightEdge(startPoint, currentShape) ? 
                        ResizeDirection.BottomRight : ResizeDirection.Bottom;
            }
            else if (IsPointNearLeftEdge(startPoint, currentShape))
            {
                isResizing = true;
                resizeDirection = ResizeDirection.Left;
            }
            else if (IsPointNearRightEdge(startPoint, currentShape))
            {
                isResizing = true;
                resizeDirection = ResizeDirection.Right;
            }
            else
            {
                isDragging = true;
            }

            PopulateEditFields(currentShape);
        }
        else
        {
            isDrawing = true;
            isDragging = false;
            isResizing = false;
        }
    }

    private void Canvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        isDrawing = false;
        isDragging = false;
        isResizing = false;
        currentShape = GetShapeUnderMouse(startPoint);
        PopulateEditFields(currentShape);
    }

    private void Canvas_OnMouseMove(object sender, MouseEventArgs e)
    {
        Point endPoint = e.GetPosition(canvas);

if (isDrawing)
{
    if (currentShape == null)
    {
        currentShape = selectedShape switch
        {
            ShapeType.Line => new Line(),
            ShapeType.Rectangle => new Rectangle(),
            ShapeType.Circle => new Ellipse(),
            _ => null
        };

        if (currentShape != null)
        {
            shapes.Add(currentShape);
            canvas.Children.Add(currentShape);
        }
    }

    switch (currentShape)
    {
        case Line line:
            line.X1 = startPoint.X;
            line.Y1 = startPoint.Y;
            line.X2 = endPoint.X;
            line.Y2 = endPoint.Y;
            break;
        case Rectangle rectangle:
            rectangle.Width = Math.Abs(startPoint.X - endPoint.X);
            rectangle.Height = Math.Abs(startPoint.Y - endPoint.Y);
            break;
        case Ellipse ellipse:
            ellipse.Width = Math.Min(Math.Abs(startPoint.X - endPoint.X), Math.Abs(startPoint.Y - endPoint.Y));
            ellipse.Height = ellipse.Width;
            break;
    }

    if (!(currentShape is Line))
    {
        System.Windows.Controls.Canvas.SetLeft(currentShape, Math.Min(startPoint.X, endPoint.X));
        System.Windows.Controls.Canvas.SetTop(currentShape, Math.Min(startPoint.Y, endPoint.Y));
    }
    currentShape.Stroke = Brushes.Black;
}
else if (isDragging && currentShape != null)
{
    double offsetX = endPoint.X - startPoint.X;
    double offsetY = endPoint.Y - startPoint.Y;

    if (currentShape is Line line)
    {
        line.X1 += offsetX;
        line.Y1 += offsetY;
        line.X2 += offsetX;
        line.Y2 += offsetY;
    }
    else
    {
        System.Windows.Controls.Canvas.SetLeft(currentShape, System.Windows.Controls.Canvas.GetLeft(currentShape) + offsetX);
        System.Windows.Controls.Canvas.SetTop(currentShape, System.Windows.Controls.Canvas.GetTop(currentShape) + offsetY);
    }

    startPoint = endPoint;
}
else if (isResizing && currentShape != null)
{
    var newLeft = System.Windows.Controls.Canvas.GetLeft(currentShape);
    var newTop = System.Windows.Controls.Canvas.GetTop(currentShape);
    var newWidth = currentShape.Width;
    var newHeight = currentShape.Height;

    var offsetX = (endPoint.X - startPoint.X) * 0.01;
    var offsetY = (endPoint.Y - startPoint.Y) * 0.01;

    (newLeft, newTop, newWidth, newHeight) = resizeDirection switch
    {
        ResizeDirection.TopLeft => (endPoint.X, endPoint.Y,
            System.Windows.Controls.Canvas.GetLeft(currentShape) + currentShape.Width - endPoint.X,
            System.Windows.Controls.Canvas.GetTop(currentShape) + currentShape.Height - endPoint.Y),

        ResizeDirection.TopRight => (newLeft, endPoint.Y,
            endPoint.X - System.Windows.Controls.Canvas.GetLeft(currentShape),
            System.Windows.Controls.Canvas.GetTop(currentShape) + currentShape.Height - endPoint.Y),

        ResizeDirection.BottomLeft => (endPoint.X, newTop,
            System.Windows.Controls.Canvas.GetLeft(currentShape) + currentShape.Width - endPoint.X,
            endPoint.Y - System.Windows.Controls.Canvas.GetTop(currentShape)),

        ResizeDirection.BottomRight => (newLeft, newTop,
            endPoint.X - System.Windows.Controls.Canvas.GetLeft(currentShape),
            endPoint.Y - System.Windows.Controls.Canvas.GetTop(currentShape)),

        ResizeDirection.Top => (newLeft, endPoint.Y, newWidth,
            System.Windows.Controls.Canvas.GetTop(currentShape) + currentShape.Height - endPoint.Y),

        ResizeDirection.Bottom => (newLeft, newTop, newWidth,
            endPoint.Y - System.Windows.Controls.Canvas.GetTop(currentShape)),

        ResizeDirection.Left => (endPoint.X, newTop,
            System.Windows.Controls.Canvas.GetLeft(currentShape) + currentShape.Width - endPoint.X, newHeight),

        ResizeDirection.Right => (newLeft, newTop,
            endPoint.X - System.Windows.Controls.Canvas.GetLeft(currentShape), newHeight),

        _ => (newLeft, newTop, newWidth, newHeight)
    };

    if (currentShape is Rectangle rectangle && newWidth >= 0 && newHeight >= 0)
    {
        System.Windows.Controls.Canvas.SetLeft(rectangle, newLeft);
        System.Windows.Controls.Canvas.SetTop(rectangle, newTop);
        rectangle.Width = newWidth;
        rectangle.Height = newHeight;
    }
    else if (currentShape is Line line)
    {
        if (resizeDirection == ResizeDirection.Left)
        {
            line.X1 += offsetX;
            line.Y1 += offsetY;
        }
        else if (resizeDirection == ResizeDirection.Right)
        {
            line.X2 += offsetX;
            line.Y2 += offsetY;
        }
    }
    else if (resizeDirection == ResizeDirection.Ellipse && currentShape is Ellipse ellipse)
    {
        double centerX = System.Windows.Controls.Canvas.GetLeft(ellipse) + ellipse.Width / 2;
        double centerY = System.Windows.Controls.Canvas.GetTop(ellipse) + ellipse.Height / 2;
        double newRadius = Math.Max(Math.Abs(endPoint.X - centerX), Math.Abs(endPoint.Y - centerY));

        System.Windows.Controls.Canvas.SetLeft(ellipse, centerX - newRadius);
        System.Windows.Controls.Canvas.SetTop(ellipse, centerY - newRadius);
        ellipse.Width = 2 * newRadius;
        ellipse.Height = 2 * newRadius;
    }
}
    }

    private void Canvas_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (currentShape != null)
        {
            double offsetX = e.Key switch
            {
                Key.Left => -2,
                Key.Right => 2,
                _ => 0
            };

            double offsetY = e.Key switch
            {
                Key.Up => -2,
                Key.Down => 2,
                _ => 0
            };

            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                if (offsetY != 0)
                {
                    currentShape.Height += offsetY;
                }
                else if (offsetX != 0)
                {
                    currentShape.Width += offsetX;
                }
            }
            else
            {
                System.Windows.Controls.Canvas.SetLeft(currentShape, System.Windows.Controls.Canvas.GetLeft(currentShape) + offsetX);
                System.Windows.Controls.Canvas.SetTop(currentShape, System.Windows.Controls.Canvas.GetTop(currentShape) + offsetY);
            }
        }
    }

    private void ShapeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        RadioButton radioButton = sender as RadioButton;
        if (radioButton?.Tag == null)
            return;

        if (!int.TryParse(radioButton.Tag.ToString(), out int shapeNumber))
            return;

        SizeStackPanel2.Visibility = Visibility.Visible;

        switch (shapeNumber)
        {
            case 1:
                selectedShape = ShapeType.Line;
                break;
            case 2:
                selectedShape = ShapeType.Rectangle;
                break;
            case 3:
                selectedShape = ShapeType.Circle;
                SizeStackPanel2.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void onDrawButtonClick(object sender, RoutedEventArgs e)
    {
        double x = Convert.ToDouble(XTextBox.Text);
        double y = Convert.ToDouble(YTextBox.Text);
        double width = Convert.ToDouble(SizeTextBox1.Text);
        double height = selectedShape != ShapeType.Circle ? Convert.ToDouble(SizeTextBox2.Text) : 0;

        Shape? newShape = selectedShape switch
        {
            ShapeType.Line => new Line(),
            ShapeType.Rectangle => new Rectangle(),
            ShapeType.Circle => new Ellipse(),
            _ => new Line(), // domyślny przypadek
        };

        Draw(newShape, x, y, width, height);
    }

    private void onClearButtonClick(object sender, RoutedEventArgs e)
    {
        canvas.Children.Clear();
        XTextBox.Text = "";
        YTextBox.Text = "";
        SizeTextBox1.Text = "";
        SizeTextBox2.Text = "";
        XEditTextBox.Text = "";
        YEditTextBox.Text = "";
        SizeEditTextBox1.Text = "";
        SizeEditTextBox2.Text = "";
        shapes.Clear();
    }

    private void onSaveButtonClick(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "XML Files|*.xml"
        };

        if (dialog.ShowDialog() == true)
        {
            List<ShapeData?> shapeDataList = new List<ShapeData?>();

            foreach (var shape in shapes)
            {
                ShapeData? shapeData = shape switch
                {
                    Line line => new LineData
                    {
                        X1 = line.X1,
                        Y1 = line.Y1,
                        X2 = line.X2,
                        Y2 = line.Y2,
                    },
                    Rectangle rectangle => new RectangleData
                    {
                        X = System.Windows.Controls.Canvas.GetLeft(rectangle),
                        Y = System.Windows.Controls.Canvas.GetTop(rectangle),
                        Width = rectangle.Width,
                        Height = rectangle.Height,
                    },
                    Ellipse ellipse => new CircleData
                    {
                        X = System.Windows.Controls.Canvas.GetLeft(ellipse),
                        Y = System.Windows.Controls.Canvas.GetTop(ellipse),
                        Diameter = ellipse.Width,
                    },
                    _ => null
                };

                if (shapeData != null)
                {
                    shapeDataList.Add(shapeData);
                }
            }

            CanvasData canvasData = new CanvasData { Shapes = shapeDataList };

            using (StreamWriter sw = new StreamWriter(dialog.FileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(CanvasData));
                serializer.Serialize(sw, canvasData);
            }
        }
    }

    private void onLoadButtonClick(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "XML Files|*.xml"
        };

        if (dialog.ShowDialog() == true)
        {
            using (StreamReader sr = new StreamReader(dialog.FileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(CanvasData));
                CanvasData canvasData = (CanvasData)serializer.Deserialize(sr);

                foreach (var shapeData in canvasData.Shapes)
                {
                    DrawShape(shapeData);
                }
            }
        }

        void DrawShape(ShapeData shapeData)
        {
            switch (shapeData)
            {
                case LineData lineData:
                    Draw(new Line(), lineData.X1, lineData.Y1, lineData.X2, lineData.Y2);
                    break;
                case RectangleData rectangleData:
                    Draw(new Rectangle(), rectangleData.X, rectangleData.Y, rectangleData.Width, rectangleData.Height);
                    break;
                case CircleData circleData:
                    Draw(new Ellipse(), circleData.X, circleData.Y, circleData.Diameter, circleData.Diameter);
                    break;
            }
        }
    }

    private void onEditButtonClick(object sender, RoutedEventArgs e)
    {
        if (currentShape != null)
        {
            try
            {
                if (double.TryParse(XEditTextBox.Text, out double x) &&
                    double.TryParse(YEditTextBox.Text, out double y) &&
                    double.TryParse(SizeEditTextBox1.Text, out double s1))
                {
                    double s2 = double.TryParse(SizeEditTextBox2.Text, out double temp) ? temp : 0;

                    SetShapePosition(currentShape, x, y, s1, s2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nieprawidłowe wartości wprowadzone do edycji kształtu.");
            }
        }

        void SetShapePosition(Shape currentShape, double x, double y, double s1, double s2)
        {
            if (!(currentShape is Line))
            {
                System.Windows.Controls.Canvas.SetLeft(currentShape, x);
                System.Windows.Controls.Canvas.SetTop(currentShape, y);
            }

            switch (currentShape)
            {
                case Rectangle rectangle:
                    rectangle.Width = s1;
                    rectangle.Height = s2;
                    break;
                case Ellipse ellipse:
                    ellipse.Width = s1;
                    ellipse.Height = s1;
                    break;
                case Line line:
                    line.X1 = x;
                    line.Y1 = y;
                    line.X2 = s1;
                    line.Y2 = s2;
                    break;
            }
        }
    }
    private void Draw(Shape? shape, double x1, double y1, double size1, double size2)
    {
        if (shape is Line line)
        {
            line.X1 = x1;
            line.Y1 = y1;
            line.X2 = size1;
            line.Y2 = size2;
            line.Stroke = Brushes.Black;
            shapes.Add(line);
            canvas.Children.Add(line);
        }
        else if (shape is Rectangle rectangle)
        {
            rectangle.Width = size1;
            rectangle.Height = size2;
            rectangle.Stroke = Brushes.Black;
            System.Windows.Controls.Canvas.SetLeft(rectangle, x1);
            System.Windows.Controls.Canvas.SetTop(rectangle, y1);
            shapes.Add(rectangle);
            canvas.Children.Add(rectangle);
        }
        else if (shape is Ellipse circle)
        {
            circle.Width = size1;
            circle.Height = size1;
            circle.Stroke = Brushes.Black;
            System.Windows.Controls.Canvas.SetLeft(circle, x1);
            System.Windows.Controls.Canvas.SetTop(circle, y1);
            shapes.Add(circle);
            canvas.Children.Add(circle);
        }
    }
    private Shape? GetShapeUnderMouse(Point point)
    {
        foreach (var shape in shapes)
        {
            if (shape is Ellipse ellipse)
            {
                if (IsPointInsideEllipse(point, ellipse))
                {
                    return shape;
                }
            }
            else if (shape is Rectangle rectangle)
            {
                if (IsPointInsideRectangle(point, rectangle))
                {
                    return shape;
                }
            }
            else if (shape is Line line)
            {
                if (IsPointNearLine(point, line))
                {
                    return shape;
                }
            }
        }

        return null;
    }
    private bool IsPointInsideEllipse(Point point, Ellipse ellipse)
    {
        double halfWidth = ellipse.Width / 2;
        double halfHeight = ellipse.Height / 2;
        double centerX = System.Windows.Controls.Canvas.GetLeft(ellipse) + halfWidth;
        double centerY = System.Windows.Controls.Canvas.GetTop(ellipse) + halfHeight;

        double normalizedX = (point.X - centerX) / halfWidth;
        double normalizedY = (point.Y - centerY) / halfHeight;

        return (normalizedX * normalizedX + normalizedY * normalizedY) <= 1.0;
    }
    private bool IsPointInsideRectangle(Point point, Rectangle rectangle)
    {
        double left = System.Windows.Controls.Canvas.GetLeft(rectangle);
        double top = System.Windows.Controls.Canvas.GetTop(rectangle);
        double right = left + rectangle.Width;
        double bottom = top + rectangle.Height;

        return point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
    }
    private bool IsPointNearLine(Point point, Line line)
    {
        double x1 = line.X1;
        double y1 = line.Y1;
        double x2 = line.X2;
        double y2 = line.Y2;

        double distance = Math.Abs((y2 - y1) * point.X - (x2 - x1) * point.Y + x2 * y1 - y2 * x1) /
                          Math.Sqrt(Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));

        return distance <= 5;
    }
    private bool IsPointOnEllipseEdge(Point point, Shape? shape)
    {
        if (shape is Ellipse ellipse)
        {
            double centerX = System.Windows.Controls.Canvas.GetLeft(ellipse) + ellipse.Width / 2;
            double centerY = System.Windows.Controls.Canvas.GetTop(ellipse) + ellipse.Height / 2;
            double radius = ellipse.Width / 2;

            double distance = Math.Sqrt(Math.Pow(point.X - centerX, 2) + Math.Pow(point.Y - centerY, 2));

            double margin = 5;

            return Math.Abs(distance - radius) <= margin;
        }

        return false;
    }
    private bool IsPointNearTopEdge(Point point, Shape? shape)
        {
            if (shape is Rectangle rectangle)
            {
                double top = System.Windows.Controls.Canvas.GetTop(rectangle);
                double margin = 5;

                return point.Y >= top - margin && point.Y <= top + margin;
            }

            return false;
        }

        private bool IsPointNearBottomEdge(Point point, Shape? shape)
        {
            if (shape is Rectangle rectangle)
            {
                double top = System.Windows.Controls.Canvas.GetTop(rectangle);
                double bottom = top + rectangle.Height;
                double margin = 5;

                return point.Y >= bottom - margin && point.Y <= bottom + margin;
            }

            return false;
        }

        private bool IsPointNearLeftEdge(Point point, Shape? shape)
        {
            if (shape is Rectangle rectangle)
            {
                double left = System.Windows.Controls.Canvas.GetLeft(rectangle);
                double margin = 5;

                return point.X >= left - margin && point.X <= left + margin;
            }
            else if (shape is Line line)
            {
                double minX = Math.Min(line.X1, line.X2);
                double margin = 5;

                return point.X >= minX - margin && point.X <= minX + margin;
            }

            return false;
        }

        private bool IsPointNearRightEdge(Point point, Shape? shape)
        {
            if (shape is Rectangle rectangle)
            {
                double left = System.Windows.Controls.Canvas.GetLeft(rectangle);
                double right = left + rectangle.Width;
                double margin = 5;

                return point.X >= right - margin && point.X <= right + margin;
            }
            else if (shape is Line line)
            {
                double maxX = Math.Max(line.X1, line.X2);
                double margin = 5;

                return point.X >= maxX - margin && point.X <= maxX + margin;
            }

            return false;
        }
        private void PopulateEditFields(Shape? selectedShape)
        {
            XTextBox.Text = "";
            YTextBox.Text = "";
            SizeTextBox1.Text = "";
            SizeTextBox2.Text = "";
        }
        
}

public enum ShapeType
{
    Line,
    Rectangle,
    Circle
}
public enum ResizeDirection
{
    None,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
    Ellipse
}
