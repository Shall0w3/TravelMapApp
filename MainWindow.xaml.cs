using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TravelMapApp
{
    public partial class MainWindow : Window
    {
        private readonly AppDbContext _dbContext;

        public MainWindow()
        {
            InitializeComponent();
            _dbContext = new AppDbContext();

            mapView.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            mapView.Position = new PointLatLng(0, 0);

            LoadPlaces();
        }

        private void MapView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Point mousePosition = e.GetPosition(mapView);
                PointLatLng point = mapView.FromLocalToLatLng((int)mousePosition.X, (int)mousePosition.Y);

                string? placeName = PromptForPlaceName("Wpisz nazwę miejsca:", "Nowe Miejsce", "Nowe Miejsce");

                if (!string.IsNullOrEmpty(placeName))
                {
                    var place = new VisitedPlace
                    {
                        Name = placeName,
                        Latitude = point.Lat,
                        Longitude = point.Lng,
                        VisitDate = DateTime.Now
                    };

                    try
                    {
                        _dbContext.VisitedPlaces.Add(place);
                        _dbContext.SaveChanges();
                        AddMarker(point, placeName, isVisited: true);
                        LoadPlaces();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas zapisywania miejsca: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void MapView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(mapView);
            PointLatLng point = mapView.FromLocalToLatLng((int)mousePosition.X, (int)mousePosition.Y);

            var result = PromptForPlannedPlace("Wpisz nazwę miejsca i datę:", "Nowe Zaplanowane Miejsce", "Nowe Miejsce");

            if (result.HasValue)
            {
                var place = new PlannedPlace
                {
                    Name = result.Value.Name,
                    Latitude = point.Lat,
                    Longitude = point.Lng,
                    PlannedVisitDate = result.Value.Date
                };

                try
                {
                    _dbContext.PlannedPlaces.Add(place);
                    _dbContext.SaveChanges();
                    AddMarker(point, place.Name, isVisited: false);
                    LoadPlaces();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisywania miejsca: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void VisitedPlacesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editVisitedButton.IsEnabled = visitedPlacesListView.SelectedItem != null;
            deleteVisitedButton.IsEnabled = visitedPlacesListView.SelectedItem != null;
        }

        private void PlannedPlacesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editPlannedButton.IsEnabled = plannedPlacesListView.SelectedItem != null;
            deletePlannedButton.IsEnabled = plannedPlacesListView.SelectedItem != null;
        }

        private void EditVisitedButton_Click(object sender, RoutedEventArgs e)
        {
            if (visitedPlacesListView.SelectedItem is VisitedPlace selectedPlace)
            {
                string? newName = PromptForPlaceName("Edytuj nazwę miejsca:", "Edytuj Miejsce", selectedPlace.Name);
                if (!string.IsNullOrEmpty(newName))
                {
                    selectedPlace.Name = newName;
                    try
                    {
                        _dbContext.SaveChanges();
                        LoadPlaces();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas zapisywania zmian: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void EditPlannedButton_Click(object sender, RoutedEventArgs e)
        {
            if (plannedPlacesListView.SelectedItem is PlannedPlace selectedPlace)
            {
                var result = PromptForPlannedPlace("Edytuj nazwę miejsca i datę:", "Edytuj Zaplanowane Miejsce", selectedPlace.Name, selectedPlace.PlannedVisitDate);
                if (result.HasValue)
                {
                    selectedPlace.Name = result.Value.Name;
                    selectedPlace.PlannedVisitDate = result.Value.Date;
                    try
                    {
                        _dbContext.SaveChanges();
                        LoadPlaces();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas zapisywania zmian: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteVisitedButton_Click(object sender, RoutedEventArgs e)
        {
            if (visitedPlacesListView.SelectedItem is VisitedPlace selectedPlace)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Czy na pewno chcesz usunąć miejsce '{selectedPlace.Name}'?",
                    "Potwierdź Usunięcie",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dbContext.VisitedPlaces.Remove(selectedPlace);
                        _dbContext.SaveChanges();
                        LoadPlaces();
                        visitedPlacesListView.SelectedItem = null;
                        editVisitedButton.IsEnabled = false;
                        deleteVisitedButton.IsEnabled = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas usuwania miejsca: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeletePlannedButton_Click(object sender, RoutedEventArgs e)
        {
            if (plannedPlacesListView.SelectedItem is PlannedPlace selectedPlace)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Czy na pewno chcesz usunąć miejsce '{selectedPlace.Name}'?",
                    "Potwierdź Usunięcie",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dbContext.PlannedPlaces.Remove(selectedPlace);
                        _dbContext.SaveChanges();
                        LoadPlaces();
                        plannedPlacesListView.SelectedItem = null;
                        editPlannedButton.IsEnabled = false;
                        deletePlannedButton.IsEnabled = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas usuwania miejsca: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private string? PromptForPlaceName(string message, string title, string defaultResponse)
        {
            Window prompt = new Window
            {
                Width = 300,
                Height = 150,
                Title = title,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White
            };
            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            TextBlock textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White
            };
            TextBox textBox = new TextBox
            {
                Text = defaultResponse,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(5)
            };
            Button okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(74, 74, 74)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(5)
            };

            ControlTemplate buttonTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);

            buttonTemplate.VisualTree = borderFactory;

            Style buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));

            Trigger mouseOverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(85, 85, 85))));

            Trigger pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(96, 96, 96))));

            buttonStyle.Triggers.Add(mouseOverTrigger);
            buttonStyle.Triggers.Add(pressedTrigger);

            okButton.Style = buttonStyle;

            string? result = null;
            okButton.Click += (s, args) => { result = textBox.Text; prompt.Close(); };
            panel.Children.Add(textBlock);
            panel.Children.Add(textBox);
            panel.Children.Add(okButton);
            prompt.Content = panel;

            prompt.ShowDialog();
            return result;
        }

        private (string Name, DateTime Date)? PromptForPlannedPlace(string message, string title, string defaultName, DateTime? defaultDate = null)
        {
            Window prompt = new Window
            {
                Width = 300,
                Height = 200,
                Title = title,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White
            };
            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            TextBlock textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White
            };
            TextBox nameTextBox = new TextBox
            {
                Text = defaultName,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(5)
            };
            DatePicker datePicker = new DatePicker
            {
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                SelectedDate = defaultDate ?? DateTime.Today
            };
            Button okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(74, 74, 74)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(5)
            };

            ControlTemplate buttonTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);

            buttonTemplate.VisualTree = borderFactory;

            Style buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));

            Trigger mouseOverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(85, 85, 85))));

            Trigger pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(96, 96, 96))));

            buttonStyle.Triggers.Add(mouseOverTrigger);
            buttonStyle.Triggers.Add(pressedTrigger);

            okButton.Style = buttonStyle;

            (string Name, DateTime Date)? result = null;
            okButton.Click += (s, args) =>
            {
                if (!string.IsNullOrEmpty(nameTextBox.Text) && datePicker.SelectedDate.HasValue)
                {
                    result = (nameTextBox.Text, datePicker.SelectedDate.Value);
                    prompt.Close();
                }
                else
                {
                    MessageBox.Show("Proszę podać nazwę miejsca i wybrać datę.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            panel.Children.Add(textBlock);
            panel.Children.Add(nameTextBox);
            panel.Children.Add(datePicker);
            panel.Children.Add(okButton);
            prompt.Content = panel;

            prompt.ShowDialog();
            return result;
        }

        private void AddMarker(PointLatLng point, string name, bool isVisited)
        {
            GMapMarker marker = new GMapMarker(point);
            marker.Shape = CreatePinShape(name, isVisited);
            mapView.Markers.Add(marker);
        }

        private UIElement CreatePinShape(string name, bool isVisited)
        {
            Canvas canvas = new Canvas { Width = 40, Height = 40 };

            // Kształt pinezki z gradientem
            Path pin = new Path
            {
                Data = Geometry.Parse("M10,0 A10,10 0 0,1 10,20 A10,10 0 0,1 10,0 M10,20 L10,30"),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            // Gradient dla pinezki
            LinearGradientBrush gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            if (isVisited)
            {
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 80, 80), 0.0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(200, 0, 0), 1.0));
            }
            else
            {
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(80, 80, 255), 0.0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 200), 1.0));
            }
            pin.Fill = gradient;

            // Cień pinezki
            pin.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315,
                ShadowDepth = 3,
                Opacity = 0.5,
                BlurRadius = 5
            };

            // Etykieta z nazwą
            TextBlock label = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                FontSize = 10,
                Padding = new Thickness(4),
                Visibility = Visibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    BlurRadius = 3
                }
            };

            // Pokazuj etykietę po najechaniu myszą
            pin.MouseEnter += (s, e) => label.Visibility = Visibility.Visible;
            pin.MouseLeave += (s, e) => label.Visibility = Visibility.Collapsed;

            // Przesunięcie pinezki w górę, aby końcówka (dolny punkt) była w punkcie (10, 30)
            Canvas.SetLeft(pin, 10);
            Canvas.SetTop(pin, -30);
            Canvas.SetLeft(label, 30);
            Canvas.SetTop(label, 0);

            canvas.Children.Add(pin);
            canvas.Children.Add(label);

            return canvas;
        }

        private void LoadPlaces()
        {
            visitedPlacesListView.ItemsSource = _dbContext.VisitedPlaces.ToList();
            plannedPlacesListView.ItemsSource = _dbContext.PlannedPlaces.ToList();

            mapView.Markers.Clear();
            foreach (var place in _dbContext.VisitedPlaces)
            {
                AddMarker(new PointLatLng(place.Latitude, place.Longitude), place.Name, isVisited: true);
            }
            foreach (var place in _dbContext.PlannedPlaces)
            {
                AddMarker(new PointLatLng(place.Latitude, place.Longitude), place.Name, isVisited: false);
            }
        }
    }
}