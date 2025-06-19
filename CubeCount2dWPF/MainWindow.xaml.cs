using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace CubeCount2D
{
    public partial class MainWindow : Window
    {
        private readonly Random random = new Random();
        private int cubeCount;
        private Point lastMousePosition;
        private readonly double rotationSpeed = 0.005;
        private readonly ModelVisual3D cubesModel = new ModelVisual3D();
        private ModelVisual3D boundingCubeModel;
        private PerspectiveCamera camera;
        private double theta = Math.PI / 2;
        private double phi = Math.PI / 2;
        private readonly double radius = 10;
        private List<Point3D> occupiedPositions = new List<Point3D>();
        private DispatcherTimer statusTimer;
        public MainWindow()
        {
            InitializeComponent();
            Setup3DScene();
            GenerateCubes();
            Update2DViews();
        }

        private void Setup3DScene()
        {
            camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 0, radius),
                LookDirection = new Vector3D(0, 0, -radius),
                UpDirection = new Vector3D(0, 1, 0),
                FieldOfView = 60
            };
            viewport.Camera = camera;
            UpdateCameraPosition();

            Model3DGroup modelGroup = new Model3DGroup();
            modelGroup.Children.Add(new AmbientLight(Color.FromRgb(255, 255, 255)));
            ModelVisual3D lighting = new ModelVisual3D();
            lighting.Content = modelGroup;
            viewport.Children.Add(lighting);

            viewport.Children.Add(cubesModel);
            boundingCubeModel = CreateBoundingCube();
            viewport.Children.Add(boundingCubeModel);
        }

        private void UpdateCameraPosition()
        {
            phi = Math.Max(0.1, Math.Min(Math.PI - 0.1, phi));
            double x = -radius * Math.Sin(phi) * Math.Cos(theta);
            double y = radius * Math.Cos(phi);
            double z = radius * Math.Sin(phi) * Math.Sin(theta);
            camera.Position = new Point3D(x, y, z);
            camera.LookDirection = new Vector3D(-x, -y, -z);
        }

        private ModelVisual3D CreateBoundingCube()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double size = 5.0;
            Point3DCollection positions = new Point3DCollection
            {
                new Point3D(-size, -size, -size),
                new Point3D(size, -size, -size),
                new Point3D(size, size, -size),
                new Point3D(-size, size, -size),
                new Point3D(-size, -size, size),
                new Point3D(size, -size, size),
                new Point3D(size, size, size),
                new Point3D(-size, size, size)
            };
            Int32Collection indices = new Int32Collection
            {
                0, 1, 2, 0, 2, 3,
                5, 4, 7, 5, 7, 6,
                4, 0, 3, 4, 3, 7,
                1, 5, 6, 1, 6, 2,
                3, 2, 6, 3, 6, 7,
                4, 5, 1, 4, 1, 0
            };
            mesh.Positions = positions;
            mesh.TriangleIndices = indices;
            DiffuseMaterial material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255,255,255)));
            GeometryModel3D cubeModel = new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
            ModelVisual3D modelVisual = new ModelVisual3D { Content = cubeModel };
            viewport.MouseLeftButtonDown += (s, e) => { if (IsMouseOverBoundingCube(e.GetPosition(viewport))) lastMousePosition = e.GetPosition(viewport); };
            viewport.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && IsMouseOverBoundingCube(e.GetPosition(viewport)))
                {
                    Point currentPosition = e.GetPosition(viewport);
                    theta -= (currentPosition.X - lastMousePosition.X) * rotationSpeed;
                    phi -= (currentPosition.Y - lastMousePosition.Y) * rotationSpeed;
                    UpdateCameraPosition();
                    lastMousePosition = currentPosition;
                }
            };
            return modelVisual;
        }

        private bool IsMouseOverBoundingCube(Point mousePosition)
        {
            HitTestResult result = VisualTreeHelper.HitTest(viewport, mousePosition);
            return result != null && result.VisualHit is ModelVisual3D visual && visual == boundingCubeModel;
        }

        private void GenerateCubes()
        {

            statusTimer = new DispatcherTimer();
            statusTimer.Interval = TimeSpan.FromSeconds(2);
            statusTimer.Tick += (s, e) =>
            {
                StatusText.Text = "";
                statusTimer.Stop();
            };
            cubeCount = random.Next(4, 7);
            cubesModel.Children.Clear();
            occupiedPositions.Clear();
            List<Point3D> possiblePositions = new List<Point3D>();
            Point3D firstPosition = new Point3D(0, 0, 0);
            occupiedPositions.Add(firstPosition);
            AddCubeAtPosition(firstPosition);
            AddPossiblePositions(firstPosition, possiblePositions, occupiedPositions);
            for (int i = 1; i < cubeCount; i++)
            {
                if (possiblePositions.Count == 0) break;
                int index = random.Next(possiblePositions.Count);
                Point3D newPosition = possiblePositions[index];
                possiblePositions.RemoveAt(index);
                AddCubeAtPosition(newPosition);
                occupiedPositions.Add(newPosition);
                if (newPosition.Y > 0)
                {
                    Point3D belowPosition = new Point3D(newPosition.X, newPosition.Y - 1.0, newPosition.Z);
                    if (!occupiedPositions.Contains(belowPosition))
                    {
                        AddCubeAtPosition(belowPosition);
                        occupiedPositions.Add(belowPosition);
                        i++;
                        if (i >= cubeCount) break;
                        AddPossiblePositions(belowPosition, possiblePositions, occupiedPositions);
                    }
                }
                AddPossiblePositions(newPosition, possiblePositions, occupiedPositions);
            }
        }

        private void AddCubeAtPosition(Point3D position)
        {
            MeshGeometry3D cubeMesh = CreateCubeMesh();
            GeometryModel3D cubeModel = new GeometryModel3D
            {
                Geometry = cubeMesh,
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 255, 0))),
                BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 255, 0)))
            };
            GeometryModel3D wireframeModel = CreateCubeWireframe();
            TranslateTransform3D positionTransform = new TranslateTransform3D(position.X, position.Y, position.Z);
            cubeModel.Transform = positionTransform;
            wireframeModel.Transform = positionTransform;
            ModelVisual3D cubeVisual = new ModelVisual3D();
            cubeVisual.Children.Add(new ModelVisual3D { Content = wireframeModel });
            cubeVisual.Children.Add(new ModelVisual3D { Content = cubeModel });
            cubesModel.Children.Add(cubeVisual);
        }

        private GeometryModel3D CreateCubeWireframe()
        {
            MeshGeometry3D wireframeMesh = new MeshGeometry3D();
            double size = 0.5;
            double thickness = 0.015;
            Point3D[] vertices = new Point3D[]
            {
                new Point3D(-size, -size, -size),
                new Point3D(size, -size, -size),
                new Point3D(size, size, -size),
                new Point3D(-size, size, -size),
                new Point3D(-size, -size, size),
                new Point3D(size, -size, size),
                new Point3D(size, size, size),
                new Point3D(-size, size, size)
            };
            int[][] edges = new int[][]
            {
                new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 0 },
                new[] { 4, 5 }, new[] { 5, 6 }, new[] { 6, 7 }, new[] { 7, 4 },
                new[] { 0, 4 }, new[] { 1, 5 }, new[] { 2, 6 }, new[] { 3, 7 }
            };
            Point3DCollection positions = new Point3DCollection();
            Int32Collection indices = new Int32Collection();
            for (int i = 0; i < edges.Length; i++)
            {
                Point3D start = vertices[edges[i][0]];
                Point3D end = vertices[edges[i][1]];
                Vector3D dir = end - start;
                double length = dir.Length;
                dir.Normalize();
                Vector3D up = Math.Abs(dir.Y) < 0.9 ? new Vector3D(0, 1, 0) : new Vector3D(0, 0, 1);
                Vector3D right = Vector3D.CrossProduct(dir, up);
                right.Normalize();
                up = Vector3D.CrossProduct(right, dir);
                up.Normalize();
                Point3D[] crossSectionStart = new Point3D[4];
                Point3D[] crossSectionEnd = new Point3D[4];
                for (int j = 0; j < 4; j++)
                {
                    double angle = j * Math.PI / 2;
                    Vector3D offset = (Math.Cos(angle) * right + Math.Sin(angle) * up) * thickness;
                    crossSectionStart[j] = start + offset;
                    crossSectionEnd[j] = end + offset;
                }
                int baseIndex = positions.Count;
                foreach (var p in crossSectionStart) positions.Add(p);
                foreach (var p in crossSectionEnd) positions.Add(p);
                int[] sideIndices = new int[] { 0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6, 3, 0, 4, 3, 4, 7 };
                foreach (int idx in sideIndices)
                {
                    indices.Add(baseIndex + idx);
                }
            }
            wireframeMesh.Positions = positions;
            wireframeMesh.TriangleIndices = indices;
            GeometryModel3D wireframeModel = new GeometryModel3D
            {
                Geometry = wireframeMesh,
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 0, 0))),
                BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 0, 0)))
            };
            return wireframeModel;
        }

        private void AddPossiblePositions(Point3D position, List<Point3D> possiblePositions, List<Point3D> occupiedPositions)
        {
            double cubeSize = 1.0;
            Point3D[] directions = new Point3D[]
            {
                new Point3D(position.X + cubeSize, position.Y, position.Z),
                new Point3D(position.X - cubeSize, position.Y, position.Z),
                new Point3D(position.X, position.Y + cubeSize, position.Z),
                new Point3D(position.X, position.Y - cubeSize, position.Z),
                new Point3D(position.X, position.Y, position.Z + cubeSize),
                new Point3D(position.X, position.Y, position.Z - cubeSize)
            };
            int neighborCount = 0;
            foreach (var dir in directions)
            {
                if (occupiedPositions.Contains(dir))
                {
                    neighborCount++;
                }
            }
            if (neighborCount == 6)
            {
                return;
            }
            foreach (var newPos in directions)
            {
                if (newPos.Y < 0)
                {
                    continue;
                }
                if (newPos.Y > 0)
                {
                    Point3D belowPos = new Point3D(newPos.X, newPos.Y - cubeSize, newPos.Z);
                    if (!occupiedPositions.Contains(belowPos))
                    {
                        continue;
                    }
                }
                if (!occupiedPositions.Contains(newPos) && !possiblePositions.Contains(newPos))
                {
                    possiblePositions.Add(newPos);
                }
            }
        }

        private MeshGeometry3D CreateCubeMesh()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double size = 0.5;
            Point3DCollection positions = new Point3DCollection
            {
                new Point3D(-size, -size, -size),
                new Point3D(size, -size, -size),
                new Point3D(size, size, -size),
                new Point3D(-size, size, -size),
                new Point3D(-size, -size, size),
                new Point3D(size, -size, size),
                new Point3D(size, size, size),
                new Point3D(-size, size, size)
            };
            Int32Collection indices = new Int32Collection
            {
                0, 1, 2, 0, 2, 3,
                5, 4, 7, 5, 7, 6,
                4, 0, 3, 4, 3, 7,
                1, 5, 6, 1, 6, 2,
                3, 2, 6, 3, 6, 7,
                4, 5, 1, 4, 1, 0
            };
            mesh.Positions = positions;
            mesh.TriangleIndices = indices;
            return mesh;
        }

        private void Update2DViews()
        {
            var views = Calculate2DViews();
            topViewImage.Source = CreateImageFromGrid(views["Top"]);
            leftViewImage.Source = CreateImageFromGrid(views["Left"]);
            frontViewImage.Source = CreateImageFromGrid(views["Front"]);
        }

        private Dictionary<string, bool[,]> Calculate2DViews()
        {
            if (occupiedPositions == null || occupiedPositions.Count == 0) return new Dictionary<string, bool[,]>();

            // Find min and max coordinates to determine array bounds
            int minX = (int)occupiedPositions.Min(p => p.X);
            int maxX = (int)occupiedPositions.Max(p => p.X);
            int minY = (int)occupiedPositions.Min(p => p.Y);
            int maxY = (int)occupiedPositions.Max(p => p.Y);
            int minZ = (int)occupiedPositions.Min(p => p.Z);
            int maxZ = (int)occupiedPositions.Max(p => p.Z);

            // Calculate array dimensions with offsets
            int widthX = maxX - minX + 1;
            int heightY = maxY - minY + 1;
            int depthZ = maxZ - minZ + 1;

            // Initialize 2D arrays with proper sizes
            bool[,] topView = new bool[widthX, depthZ];    // X vs Z (top view)
            bool[,] leftView = new bool[depthZ, heightY];  // Z vs Y (rotated left view)
            bool[,] frontView = new bool[widthX, heightY]; // X vs Y (front view)

            // Populate arrays with cube positions, adjusting for minimum values
            foreach (var pos in occupiedPositions)
            {
                int x = (int)pos.X - minX;
                int y = (int)pos.Y - minY;
                int z = (int)pos.Z - minZ;

                if (x >= 0 && x < widthX && y >= 0 && y < heightY && z >= 0 && z < depthZ)
                {
                    topView[x, z] = true;
                    // Rotate left view 90 degrees counterclockwise: Z becomes vertical, Y becomes horizontal, invert Z for bottom-to-top
                    int rotatedY = heightY -1-y; // Horizontal axis (original Y)
                   
                    if (rotatedY >= 0 && rotatedY < heightY)
                    {
                        leftView[z, rotatedY] = true; // Z (rows) vs Y (columns)
                    }
                    // Invert Y for front view: map higher Y to lower indices
                    int invertedY = heightY - 1 - y;
                    if (invertedY >= 0 && invertedY < heightY)
                    {
                        frontView[x, invertedY] = true;
                    }
                }
            }

            return new Dictionary<string, bool[,]>
            {
                { "Top", topView },
                { "Left", leftView },
                { "Front", frontView }
            };
        }

        private System.Windows.Media.Imaging.BitmapSource CreateImageFromGrid(bool[,] grid)
        {
            // Define a fixed grid size (e.g., 5x5 cells to match the image's complexity)
            int gridWidth = 5;
            int gridHeight = 5;
            int cellSize = 40; // Pixel size of each cell
            int totalWidth = gridWidth * cellSize;
            int totalHeight = gridHeight * cellSize;

            var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(totalWidth, totalHeight, 96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = new byte[totalWidth * totalHeight * 4]; // 4 bytes per pixel (BGRA)

            // Draw grid and fill cells
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < totalWidth; x++)
                {
                    int cellX = x / cellSize;
                    int cellY = y / cellSize;
                    int index = (y * totalWidth + x) * 4;

                    // Draw grid lines (black)
                    if (x % cellSize == 0 || y % cellSize == 0)
                    {
                        pixels[index] = 0;     // B
                        pixels[index + 1] = 0; // G
                        pixels[index + 2] = 0; // R
                        pixels[index + 3] = 255; // A
                    }
                    else
                    {
                        // Fill cell with white background
                        pixels[index] = 255;     // B
                        pixels[index + 1] = 255; // G
                        pixels[index + 2] = 255; // R
                        pixels[index + 3] = 255; // A

                        // Map 2D grid to larger bitmap and fill with yellow if cube present
                        if (cellX < grid.GetLength(0) && cellY < grid.GetLength(1) && grid[cellX, cellY])
                        {
                            pixels[index] = 255;     // B
                            pixels[index + 1] = 144; // G
                            pixels[index + 2] = 30;   // R
                            pixels[index + 3] = 255; // A
                        }
                    }
                }
            }

            bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, totalWidth, totalHeight), pixels, totalWidth * 4, 0);
            return bitmap;
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(guessTextBox.Text, out int userGuess))
            {
                if (userGuess == cubeCount)
                {
                    ShowStatus("Правильно.");
                    return;

                }
                else
                {
                    ShowStatus("Неправильно.");
                    return;
                }
            }
            else
            {
                ShowStatus("Введите число.");
                return;
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateCubes();
            Update2DViews();
            guessTextBox.Text = "";
            StatusText.Text = "";
        }
        private void ShowStatus(string message)
        {
            StatusText.Text = message;
            StatusText.Visibility = Visibility.Visible;
            statusTimer.Stop(); // сбрасываем, если таймер уже бежит
            statusTimer.Start(); // запускаем заново
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewport.Visibility == Visibility.Hidden)
            {
                viewport.Visibility = Visibility.Visible;
            }
            else
            {
                viewport.Visibility = Visibility.Hidden;
            }
        }
    }
}