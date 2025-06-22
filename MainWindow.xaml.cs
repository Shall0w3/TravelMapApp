using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
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

        private void MapView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(mapView);
            PointLatLng clickedPoint = mapView.FromLocalToLatLng((int)mousePosition.X, (int)mousePosition.Y);

            // Znajdź najbliższy marker w odległości 0.01 stopni
            foreach (var marker in mapView.Markers)
            {
                var distance = Math.Sqrt(Math.Pow(marker.Position.Lat - clickedPoint.Lat, 2) + Math.Pow(marker.Position.Lng - clickedPoint.Lng, 2));
                if (distance < 0.01)
                {
                    if (marker.Tag is Tuple<int, bool> tag)
                    {
                        if (tag.Item2) // VisitedPlace
                        {
                            var place = _dbContext.VisitedPlaces.FirstOrDefault(p => p.Id == tag.Item1);
                            if (place != null)
                                ShowTodoListWindow(place.Id, null);
                        }
                        else // PlannedPlace
                        {
                            var place = _dbContext.PlannedPlaces.FirstOrDefault(p => p.Id == tag.Item1);
                            if (place != null)
                                ShowTodoListWindow(null, place.Id);
                        }
                    }
                    break;
                }
            }
        }

        private void OpenTodoListVisited_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int placeId)
            {
                ShowTodoListWindow(placeId, null);
            }
        }

        private void OpenTodoListPlanned_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int placeId)
            {
                ShowTodoListWindow(null, placeId);
            }
        }

        private void ShowTodoListWindow(int? visitedPlaceId, int? plannedPlaceId)
        {
            if (visitedPlaceId == null && plannedPlaceId == null)
            {
                MessageBox.Show("Nie wybrano miejsca.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Window todoWindow = new Window
            {
                Width = 400,
                Height = 300,
                Title = "Lista TODO",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            ListBox todoList = new ListBox
            {
                Height = 200,
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };

            // Wczytaj zadania TODO
            var todos = _dbContext.TodoItems
                .Where(t => (t.VisitedPlaceId == visitedPlaceId || t.PlannedPlaceId == plannedPlaceId))
                .ToList();

            if (!todos.Any())
            {
                TextBlock noTasksText = new TextBlock
                {
                    Text = "Brak zadań dla tego miejsca.",
                    Foreground = Brushes.White,
                    Margin = new Thickness(5)
                };
                panel.Children.Add(noTasksText);
            }
            else
            {
                foreach (var todo in todos)
                {
                    StackPanel todoItemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                    CheckBox checkBox = new CheckBox
                    {
                        IsChecked = todo.IsCompleted,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5)
                    };
                    TextBlock textBlock = new TextBlock
                    {
                        Text = todo.Description,
                        Foreground = Brushes.White,
                        Margin = new Thickness(5),
                        TextDecorations = todo.IsCompleted ? TextDecorations.Strikethrough : null
                    };
                    Button editButton = new Button { Content = "Edytuj", Width = 50, Margin = new Thickness(5) };
                    Button deleteButton = new Button { Content = "Usuń", Width = 50, Margin = new Thickness(5) };

                    checkBox.Checked += (s, e) => UpdateTodoStatus(todo, true);
                    checkBox.Unchecked += (s, e) => UpdateTodoStatus(todo, false);
                    editButton.Click += (s, e) => EditTodoItem(todo, textBlock);
                    deleteButton.Click += (s, e) => DeleteTodoItem(todo, todoItemPanel, todoList);

                    todoItemPanel.Children.Add(checkBox);
                    todoItemPanel.Children.Add(textBlock);
                    todoItemPanel.Children.Add(editButton);
                    todoItemPanel.Children.Add(deleteButton);
                    todoList.Items.Add(todoItemPanel);
                }
                panel.Children.Add(todoList);
            }

            TextBox newTodoTextBox = new TextBox
            {
                Margin = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(5)
            };

            Button addButton = new Button
            {
                Content = "Dodaj",
                Width = 75,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(74, 74, 74)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Padding = new Thickness(5)
            };

            addButton.Style = CreateButtonStyle();

            addButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(newTodoTextBox.Text))
                {
                    var todo = new TodoItem
                    {
                        Description = newTodoTextBox.Text,
                        IsCompleted = false,
                        VisitedPlaceId = visitedPlaceId,
                        PlannedPlaceId = plannedPlaceId
                    };

                    try
                    {
                        _dbContext.TodoItems.Add(todo);
                        _dbContext.SaveChanges();

                        StackPanel todoItemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                        CheckBox checkBox = new CheckBox
                        {
                            IsChecked = false,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5)
                        };
                        TextBlock textBlock = new TextBlock
                        {
                            Text = todo.Description,
                            Foreground = Brushes.White,
                            Margin = new Thickness(5)
                        };
                        Button editButton = new Button { Content = "Edytuj", Width = 50, Margin = new Thickness(5) };
                        Button deleteButton = new Button { Content = "Usuń", Width = 50, Margin = new Thickness(5) };

                        checkBox.Checked += (s, e) => UpdateTodoStatus(todo, true);
                        checkBox.Unchecked += (s, e) => UpdateTodoStatus(todo, false);
                        editButton.Click += (s, e) => EditTodoItem(todo, textBlock);
                        deleteButton.Click += (s, e) => DeleteTodoItem(todo, todoItemPanel, todoList);

                        todoItemPanel.Children.Add(checkBox);
                        todoItemPanel.Children.Add(textBlock);
                        todoItemPanel.Children.Add(editButton);
                        todoItemPanel.Children.Add(deleteButton);
                        todoList.Items.Add(todoItemPanel);

                        newTodoTextBox.Text = "";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas dodawania zadania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };

            panel.Children.Add(newTodoTextBox);
            panel.Children.Add(addButton);
            todoWindow.Content = panel;
            todoWindow.ShowDialog();
        }

        private void UpdateTodoStatus(TodoItem todo, bool isCompleted)
        {
            todo.IsCompleted = isCompleted;
            try
            {
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji zadania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTodoItem(TodoItem todo, TextBlock textBlock)
        {
            string? newDescription = PromptForPlaceName("Edytuj zadanie:", "Edytuj", todo.Description);
            if (!string.IsNullOrEmpty(newDescription))
            {
                todo.Description = newDescription;
                try
                {
                    _dbContext.SaveChanges();
                    textBlock.Text = newDescription;
                    textBlock.TextDecorations = todo.IsCompleted ? TextDecorations.Strikethrough : null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas edycji zadania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteTodoItem(TodoItem todo, StackPanel todoPanel, ListBox todoList)
        {
            MessageBoxResult result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć zadanie: '{todo.Description}'?",
                "Potwierdź Usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.TodoItems.Remove(todo);
                    _dbContext.SaveChanges();
                    todoList.Items.Remove(todoPanel);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas usuwania zadania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
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
            int placeId = isVisited
                ? _dbContext.VisitedPlaces.FirstOrDefault(p => p.Name == name && Math.Abs(p.Latitude - point.Lat) < 0.01 && Math.Abs(p.Longitude - point.Lng) < 0.01)?.Id ?? 0
                : _dbContext.PlannedPlaces.FirstOrDefault(p => p.Name == name && Math.Abs(p.Latitude - point.Lat) < 0.01 && Math.Abs(p.Longitude - point.Lng) < 0.01)?.Id ?? 0;
            marker.Tag = new Tuple<int, bool>(placeId, isVisited);
            marker.Shape = CreatePinShape(name, isVisited);
            mapView.Markers.Add(marker);
        }

        private UIElement CreatePinShape(string name, bool isVisited)
        {
            Canvas canvas = new Canvas { Width = 40, Height = 40 };
            Path pin = new Path
            {
                Data = Geometry.Parse("M10,0 A10,10 0 0,1 10,20 A10,10 0 0,1 10,0 M10,20 L10,30"),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

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

            pin.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315,
                ShadowDepth = 3,
                Opacity = 0.5,
                BlurRadius = 5
            };

            TextBlock label = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                FontSize = 10,
                Padding = new Thickness(4),
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    BlurRadius = 3
                }
            };

            pin.MouseEnter += (s, e) => label.Visibility = Visibility.Visible;
            pin.MouseLeave += (s, e) => label.Visibility = Visibility.Collapsed;

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

        private Style CreateButtonStyle()
        {
            Style buttonStyle = new Style(typeof(Button));
            ControlTemplate buttonTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenter);

            buttonTemplate.VisualTree = borderFactory;
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));

            Trigger mouseOverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(85, 85, 85))));

            Trigger pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(96, 96, 96))));

            buttonStyle.Triggers.Add(mouseOverTrigger);
            buttonStyle.Triggers.Add(pressedTrigger);

            return buttonStyle;
        }
    }
}