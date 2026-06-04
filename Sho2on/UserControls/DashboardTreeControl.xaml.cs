using HR_Application.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.Controls
{
    public partial class DashboardTreeControl : UserControl
    {
        private double _currentZoom = 1.0;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 2.0;
        private const double ZoomStep = 0.1;
        private Point _lastMousePosition;
        private bool _isPanning = false;

        // ── Dependency Property ──────────────────────────────────────────────
        public static readonly DependencyProperty TreeSourceProperty =
            DependencyProperty.Register(
                nameof(TreeSource),
                typeof(IEnumerable<DashboardTree>),
                typeof(DashboardTreeControl),
                new PropertyMetadata(null, OnTreeSourceChanged));

        public IEnumerable<DashboardTree> TreeSource
        {
            get => (IEnumerable<DashboardTree>)GetValue(TreeSourceProperty);
            set => SetValue(TreeSourceProperty, value);
        }

        private static void OnTreeSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DashboardTreeControl ctrl && e.NewValue is IEnumerable<DashboardTree> treeData)
            {
                ctrl.PopulateCards(treeData);
            }
        }

        // ── Constructor ──────────────────────────────────────────────────────
        public DashboardTreeControl()
        {
            InitializeComponent();

            // Add mouse wheel zoom handler to the main scroll viewer
            MainScrollViewer.PreviewMouseWheel += MainScrollViewer_PreviewMouseWheel;
            MainScrollViewer.PreviewMouseDown += MainScrollViewer_PreviewMouseDown;
            MainScrollViewer.PreviewMouseMove += MainScrollViewer_PreviewMouseMove;
            MainScrollViewer.PreviewMouseUp += MainScrollViewer_PreviewMouseUp;
        }

        // ── Mouse Wheel Zoom ─────────────────────────────────────────────────
        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Check if Ctrl key is pressed for zoom
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true; // Prevent default scroll behavior

                // Get mouse position relative to MainScrollViewer
                Point mousePos = e.GetPosition(MainScrollViewer);

                // Calculate the position relative to MainContentPanel
                Point contentMousePos = e.GetPosition(MainContentPanel);

                if (e.Delta > 0)
                {
                    // Zoom In
                    if (_currentZoom < MaxZoom)
                    {
                        _currentZoom += ZoomStep;
                        ApplyZoom(mousePos, contentMousePos);
                    }
                }
                else
                {
                    // Zoom Out
                    if (_currentZoom > MinZoom)
                    {
                        _currentZoom -= ZoomStep;
                        ApplyZoom(mousePos, contentMousePos);
                    }
                }
            }
            // If Ctrl is not pressed, allow normal scrolling
        }

        // ── Pan with Middle Mouse Button ─────────────────────────────────────
        private void MainScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(MainScrollViewer);
                MainScrollViewer.Cursor = Cursors.ScrollAll;
                MainScrollViewer.CaptureMouse();
                e.Handled = true;
            }
        }

        private void MainScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPosition = e.GetPosition(MainScrollViewer);
                Vector delta = _lastMousePosition - currentPosition;

                MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + delta.X);
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + delta.Y);

                _lastMousePosition = currentPosition;
            }
        }

        private void MainScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && _isPanning)
            {
                _isPanning = false;
                MainScrollViewer.Cursor = Cursors.Arrow;
                MainScrollViewer.ReleaseMouseCapture();
            }
        }

        // ── Populate Cards ───────────────────────────────────────────────────
        private void PopulateCards(IEnumerable<DashboardTree> treeData)
        {
            var treeList = treeData?.ToList();
            if (treeList == null || !treeList.Any()) return;

            var root = treeList.First();
            RootCard.DataContext = root;

            if (root.Children != null && root.Children.Any())
            {
                Level1CardsControl.ItemsSource = root.Children;
            }
        }

        // ── Sector Click - Toggle Branches Visibility ────────────────────────
        private void SectorHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border headerBorder)
            {
                var parentStackPanel = VisualTreeHelper.GetParent(headerBorder) as StackPanel;

                if (parentStackPanel != null)
                {
                    ItemsControl branchesList = null;

                    foreach (var child in parentStackPanel.Children)
                    {
                        if (child is ItemsControl ic && ic.Name == "BranchesList")
                        {
                            branchesList = ic;
                            break;
                        }
                    }

                    if (branchesList != null)
                    {
                        bool isExpanded = branchesList.Visibility != Visibility.Visible;
                        branchesList.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                        UpdateExpandIndicator(headerBorder, isExpanded);
                        UpdateArrowIndicator(headerBorder, isExpanded);
                    }
                }
            }
        }

        // ── Branch Click - Toggle Departments Visibility ─────────────────────
        private void BranchHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border branchBorder)
            {
                var branchStackPanel = branchBorder.Child as StackPanel;
                if (branchStackPanel != null)
                {
                    ItemsControl departmentsList = null;
                    FindDepartmentsListInPanel(branchStackPanel, ref departmentsList);

                    if (departmentsList != null)
                    {
                        bool isExpanded = departmentsList.Visibility != Visibility.Visible;
                        departmentsList.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                        UpdateBranchExpandIndicator(branchBorder, isExpanded);
                        UpdateBranchArrowIndicator(branchBorder, isExpanded);
                    }
                }
            }
        }

        // Helper to find DepartmentsList in a StackPanel
        private void FindDepartmentsListInPanel(StackPanel panel, ref ItemsControl departmentsList)
        {
            foreach (var child in panel.Children)
            {
                if (child is ItemsControl ic && ic.Name == "DepartmentsList")
                {
                    departmentsList = ic;
                    return;
                }

                if (child is Panel nestedPanel)
                {
                    FindDepartmentsListInChildPanel(nestedPanel, ref departmentsList);
                    if (departmentsList != null) return;
                }
            }
        }

        private void FindDepartmentsListInChildPanel(Panel panel, ref ItemsControl departmentsList)
        {
            foreach (var child in panel.Children)
            {
                if (child is ItemsControl ic && ic.Name == "DepartmentsList")
                {
                    departmentsList = ic;
                    return;
                }

                if (child is Panel nestedPanel)
                {
                    FindDepartmentsListInChildPanel(nestedPanel, ref departmentsList);
                    if (departmentsList != null) return;
                }
            }
        }

        // Update expand indicator for sector header
        private void UpdateExpandIndicator(Border headerBorder, bool isExpanded)
        {
            if (isExpanded)
            {
                headerBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D5060"));
                headerBorder.BorderBrush = (Brush)FindResource("AccentBrush");
                headerBorder.BorderThickness = new Thickness(1);
            }
            else
            {
                headerBorder.Background = (Brush)FindResource("HoverBrush");
                headerBorder.BorderBrush = null;
                headerBorder.BorderThickness = new Thickness(0);
            }
        }

        // Update arrow indicator for sector
        private void UpdateArrowIndicator(Border headerBorder, bool isExpanded)
        {
            var grid = headerBorder.Child as Grid;
            if (grid != null && grid.Children.Count > 0)
            {
                var secondRow = grid.Children[1] as StackPanel;
                if (secondRow != null)
                {
                    foreach (var child in secondRow.Children)
                    {
                        if (child is TextBlock tb && tb.Name == "ExpandArrow")
                        {
                            tb.Text = isExpanded ? "▲" : "▼";
                            break;
                        }
                    }
                }
            }
        }

        // Update expand indicator for branch border
        private void UpdateBranchExpandIndicator(Border branchBorder, bool isExpanded)
        {
            if (isExpanded)
            {
                branchBorder.BorderBrush = (Brush)FindResource("AccentBrush");
                branchBorder.BorderThickness = new Thickness(1);
            }
            else
            {
                branchBorder.BorderBrush = null;
                branchBorder.BorderThickness = new Thickness(0);
            }
        }

        // Update arrow indicator for branch
        private void UpdateBranchArrowIndicator(Border branchBorder, bool isExpanded)
        {
            var stackPanel = branchBorder.Child as StackPanel;
            if (stackPanel != null && stackPanel.Children.Count > 0)
            {
                var grid = stackPanel.Children[0] as Grid;
                if (grid != null && grid.Children.Count > 0)
                {
                    var firstRow = grid.Children[0] as StackPanel;
                    if (firstRow != null)
                    {
                        foreach (var child in firstRow.Children)
                        {
                            if (child is TextBlock tb && (tb.Text == "▼" || tb.Text == "▲"))
                            {
                                tb.Text = isExpanded ? "▲" : "▼";
                                break;
                            }
                        }
                    }
                }
            }
        }

        // ── Expand / Collapse All ────────────────────────────────────────────
        private void BtnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllBranchesAndDepartments(true);
        }

        private void BtnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            ToggleAllBranchesAndDepartments(false);
        }

        private void ToggleAllBranchesAndDepartments(bool expand)
        {
            var visibility = expand ? Visibility.Visible : Visibility.Collapsed;

            Level1CardsControl.UpdateLayout();

            for (int i = 0; i < Level1CardsControl.Items.Count; i++)
            {
                var container = Level1CardsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;

                container.ApplyTemplate();
                container.UpdateLayout();

                ExpandCollapseAllInVisualTree(container, visibility);
            }
        }

        private void ExpandCollapseAllInVisualTree(DependencyObject parent, Visibility visibility)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ItemsControl ic)
                {
                    if (ic.Name == "BranchesList" || ic.Name == "DepartmentsList")
                    {
                        ic.Visibility = visibility;
                    }
                }

                ExpandCollapseAllInVisualTree(child, visibility);
            }
        }

        // ── Zoom Controls ────────────────────────────────────────────────────
        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentZoom < MaxZoom)
            {
                _currentZoom += ZoomStep;
                ApplyZoom(null, null);
            }
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_currentZoom > MinZoom)
            {
                _currentZoom -= ZoomStep;
                ApplyZoom(null, null);
            }
        }

        private void BtnZoomReset_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = 1.0;
            ApplyZoom(null, null);
        }

        private void ApplyZoom(Point? mousePosition, Point? contentMousePosition)
        {
            ZoomLevelText.Text = $"{(int)(_currentZoom * 100)}%";

            // Apply LayoutTransform to the Grid container instead of StackPanel
            var scaleTransform = new ScaleTransform(_currentZoom, _currentZoom);
            ZoomContainer.LayoutTransform = scaleTransform;

            // Force update layout
            ZoomContainer.UpdateLayout();
            MainScrollViewer.UpdateLayout();

            // Make sure the StackPanel takes the full width of the Grid
            MainContentPanel.Width = ZoomContainer.ActualWidth > 0 ? ZoomContainer.ActualWidth : double.NaN;

            // If zooming with mouse, adjust scroll position to keep mouse point stable
            if (mousePosition.HasValue && contentMousePosition.HasValue)
            {
                MainScrollViewer.ScrollToHorizontalOffset(
                    contentMousePosition.Value.X * _currentZoom - mousePosition.Value.X);
                MainScrollViewer.ScrollToVerticalOffset(
                    contentMousePosition.Value.Y * _currentZoom - mousePosition.Value.Y);
            }

            // Enable/disable zoom buttons
            BtnZoomIn.IsEnabled = _currentZoom < MaxZoom;
            BtnZoomOut.IsEnabled = _currentZoom > MinZoom;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.Logo))
                RootNodeImage.Source = new BitmapImage(new Uri(Properties.Settings.Default.Logo));
            else
                RootNodeIcon.Visibility = Visibility.Visible;
        }
    }
}