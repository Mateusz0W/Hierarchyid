﻿using System;
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
        private const double BoxWidth = 140;
        private const double BoxHeight = 90;
        private const double HorizontalSpacing = 30;
        private const double VerticalSpacing = 80;
        private Person ClickedPerson;
        private Person RightClickedPerson;
        private readonly HierarchyService service;
        private Border SelectedBorder;


        public MainWindow()
        {
            InitializeComponent();
            string connectionString = "Server=.;Database=Project;TrustServerCertificate=True;Integrated Security=True;";
            service = new HierarchyService(connectionString);
            Dictionary<string, Person> tree = service.readTree();
            if (tree.Any())
            {
                Person root = tree.Values.First(p => !tree.Values.Any(x => x.getChildrens().Contains(p)));
                DrawTree(root, 0, 20, out _);
            }
        }

       
        private double DrawTree(Person person, double x, double y, out double centerX)
        {
            NumOfNodes.Text = $"{this.service.numberOfNodes()}";
            NumOfLevels.Text = $"{this.service.numberOfLevels()}";

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
            DrawPersonBox(person.getName(), centerX - BoxWidth / 2, y, person);

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
                    Text = $"{person.getName()} {person.GetSurname()}\nstanowisko: {person.GetPosition()} \n start. {person.GetHireDate()?.ToShortDateString()}\nkoniec. {person.GetTerminationDate()?.ToShortDateString()}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    //TextWrapping = TextWrapping.Wrap
                },
                Tag = person
            };
            border.MouseDown += PersonBox_Click;

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
            Person person = new Person(NameTextBox.Text.Trim(), SurnameTextBox.Text.Trim(), BirthDatePicker.SelectedDate,DeathDatePicker.SelectedDate,PositionTextBox.Text.Trim());
            Dictionary<string, Person> tree = service.readTree();
            if (tree.Count == 0)
            {
                if (string.IsNullOrEmpty(NameTextBox.Text) || string.IsNullOrEmpty(SurnameTextBox.Text) || BirthDatePicker.SelectedDate == null || string.IsNullOrEmpty(PositionTextBox.Text))
                {
                    MessageBox.Show("Nie wszystkie pola  zostały wypełnione.\nNie można dodać osoby!");
                    return;
                }
                this.service.CreateRoot(person);
            }
            else
            {
                if (string.IsNullOrEmpty(NameTextBox.Text) || string.IsNullOrEmpty(SurnameTextBox.Text) || BirthDatePicker.SelectedDate == null || ClickedPerson == null || string.IsNullOrEmpty(PositionTextBox.Text))
                {
                    MessageBox.Show("Nie wszystkie pola  zostały wypełnione lub  nie zaznaczono rodzica.\nNie można dodać osoby!");
                    return;
                }

                this.service.AddNode(ClickedPerson, person);
            }
           
            RefreshTree();
        }
        private void PersonBox_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Person person)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (SelectedBorder != null)
                    {
                        SelectedBorder.Background = Brushes.LightYellow;
                    }
                    border.Background = Brushes.LightBlue;
                    SelectedBorder = border;
                  
                    this.ClickedPerson = person;
                    NumOfDescendants.Text = $"{this.service.numberOfDescendants(ClickedPerson.GetID())}";
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    if (SelectedBorder != null)
                    {
                        SelectedBorder.Background = Brushes.LightYellow;
                    }
                    border.Background = Brushes.LightGreen;
                    SelectedBorder = border;
                    this.RightClickedPerson = person;
                }
            }
        }
        private void DeleteSubtree_Click(object sender, RoutedEventArgs e)
        {
            if (ClickedPerson == null) {
                MessageBox.Show("Nie wybrano poddrzewa do usunięca.\n Nie można usunąć poddrzewa!");
                return;
            }
            this.service.removeSubtree(ClickedPerson);
            RefreshTree();
        }
        private void DeletePerson_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, Person> tree = service.readTree();
            Person root=tree.Values.First(p => !tree.Values.Any(x => x.getChildrens().Contains(p)));
            if (ClickedPerson == null)
            {
                MessageBox.Show("Nie wybrano węzła do usunięca.\n Nie można usunąć węzła!");
                return;
            }
            else if (ClickedPerson.GetID() == root.GetID())
            {
                this.service.removeSubtree(root);
            }
            else
                this.service.RemoveNode(ClickedPerson);
            RefreshTree();


        }
        private void RefreshTree()
        {
            TreeCanvas.Children.Clear();
            ClickedPerson = null;
            SelectedBorder = null;
            Dictionary<string, Person> tree = service.readTree();
            if (!tree.Any())
            {
                NumOfDescendants.Text = "0";
                NumOfNodes.Text = "0";
                NumOfLevels.Text = "0";
                return;
            }
            Person root = tree.Values.First(p => !tree.Values.Any(x => x.getChildrens().Contains(p)));
            DrawTree(root, 0, 20, out _);
        }
        private void MovePerson_Click(object sender, RoutedEventArgs e)
        {
            if (RightClickedPerson == null || ClickedPerson == null)
            {
                MessageBox.Show("Nie zaznaczono odpowiedniej ilości węzłów.\n Nie można przenieść poddrzewa!");
                return;
            }
            this.service.MoveSubTree( RightClickedPerson.GetID(), ClickedPerson.GetID());
            RefreshTree();
        }
    }
}
