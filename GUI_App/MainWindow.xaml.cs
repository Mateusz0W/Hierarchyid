using System;
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





namespace FamilyTreeGraph
{
    public partial class MainWindow : Window
    {
        private const double BoxWidth = 100;
        private const double BoxHeight = 60;
        private const double HorizontalSpacing = 30;
        private const double VerticalSpacing = 80;
        private Person ClickedPerson;
        private readonly HierarchyService service;
        private Border SelectedBorder;


        public MainWindow()
        {
            InitializeComponent();
            string connectionString = "Server=.;Database=Project;TrustServerCertificate=True;Integrated Security=True;";
            service = new HierarchyService(connectionString);
            Dictionary<string,Person> tree = service.readTree();
            Person root = tree.Values.First(p => !tree.Values.Any(x => x.getChildrens().Contains(p)));
            DrawTree(root, 0, 20, out _);
        }

        // Rekursyjne rysowanie drzewa
        private double DrawTree(Person person, double x, double y, out double centerX)
        {
            if (person.getChildrens().Count == 0)
            {
                DrawPersonBox(person.getName(), x, y, person);
                centerX = x + BoxWidth / 2;
                return x + BoxWidth;
            }

            double currentX = x;
            List<double> childCenters = new();

            foreach (var child in person.getChildrens())
            {
                double subtreeX = DrawTree(child, currentX, y + BoxHeight + VerticalSpacing, out double childCenter);
                childCenters.Add(childCenter);
                currentX = subtreeX + HorizontalSpacing;
            }

            centerX = (childCenters[0] + childCenters[^1]) / 2;
            DrawPersonBox(person.getName(), centerX - BoxWidth / 2, y,person);

            // Rysowanie linii do dzieci
            foreach (var childCenter in childCenters)
            {
                DrawLine(centerX, y + BoxHeight, childCenter, y + BoxHeight + VerticalSpacing);
            }

            return currentX;
        }

        private void DrawPersonBox(string name, double x, double y, Person person)
        {
            Border border = new Border
            {
                Width = BoxWidth,
                Height = BoxHeight,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = Brushes.LightYellow,
                Child = new TextBlock
                {
                    Text = $"{person.getName()} {person.GetSurname()} \n ur. {person.GetBirthDate()?.ToShortDateString()}\nsr. {person.GetDeathDate()?.ToShortDateString()}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    //TextWrapping = TextWrapping.Wrap
                },
                Tag = person
            };
            border.MouseLeftButtonDown += PersonBox_Click;

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            TreeCanvas.Children.Add(border);
        }

        private void DrawLine(double x1, double y1, double x2, double y2)
        {
            Line line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            TreeCanvas.Children.Add(line);
        }
        private void AddPerson_Click(object sender, RoutedEventArgs e)
        {
            Person person = new Person(NameTextBox.Text.Trim(), SurnameTextBox.Text.Trim(), BirthDatePicker.SelectedDate,DeathDatePicker.SelectedDate);
            this.service.AddNode(ClickedPerson, person);
            // redraw tree
            TreeCanvas.Children.Clear();
            ClickedPerson = null;
            SelectedBorder = null;
            Dictionary<string, Person> tree = service.readTree();
            Person root = tree.Values.First(p => !tree.Values.Any(x => x.getChildrens().Contains(p)));
            DrawTree(root, 0, 20, out _);

        }
        private void PersonBox_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Person person)
            {
                if (SelectedBorder != null)
                {
                    SelectedBorder.Background = Brushes.LightYellow;
                }
                border.Background = Brushes.LightBlue;
                SelectedBorder = border;
                // Przykład działania: pokazanie danych osoby
                this.ClickedPerson = person;
               
            }
        }
        private void DeleteSubtree_Click(object sender, RoutedEventArgs e)
        {
            this.service.removeSubtree(ClickedPerson);
            RefreshTree();
        }
        private void DeletePerson_Click(object sender, RoutedEventArgs e)
        {
            this.service.RemoveNode(ClickedPerson);
            RefreshTree();
        }
        private void RefreshTree()
        {
            TreeCanvas.Children.Clear();
            ClickedPerson = null;
            SelectedBorder = null;
            Dictionary<string, Person> tree = service.readTree();
            Person root = tree.Values.First(p => !tree.Values.Any(x => x.getChildrens().Contains(p)));
            DrawTree(root, 0, 20, out _);
        }


    }
}
