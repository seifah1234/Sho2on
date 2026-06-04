using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.UserControls
{
    /// <summary>
    /// Interaction logic for ClockTimePicker.xaml
    /// </summary>
    public partial class ClockTimePicker : UserControl
    {
        // ── Constants ──────────────────────────────────────────
        private const double CX = 130;            // Clock center X
        private const double CY = 130;            // Clock center Y
        private const double OUTER_RADIUS = 110;  // Number ring radius
        private const double HR_HAND_LEN = 72;    // Hour hand length
        private const double MIN_HAND_LEN = 96;   // Minute hand length
        private const double NUMBER_R = 92;        // Distance from center to number
        private const double DOT_RADIUS = 16;      // Selection dot / highlight half-size

        // ── State ──────────────────────────────────────────────
        private int _hour = 10;       // 0-23
        private int _minute = 30;
        private bool _isAmPm = true;  // true = AM, false = PM
        private bool _isHourMode = true;
        private bool _isDragging = false;

        // ── Dependency Properties ──────────────────────────────
        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(nameof(SelectedTime), typeof(TimeSpan),
                typeof(ClockTimePicker), new PropertyMetadata(new TimeSpan(10, 30, 0)));

        /// <summary>The selected time (24-hour TimeSpan).</summary>
        public TimeSpan SelectedTime
        {
            get => (TimeSpan)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        /// <summary>Raised when the user clicks OK.</summary>
        public event EventHandler<TimeSpan>? TimeConfirmed;

        // ── Constructor ────────────────────────────────────────
        public ClockTimePicker()
        {
            InitializeComponent();
            Loaded += (_, _) => { DrawNumberLabels(); UpdateClock(); };
        }

        // ── Drawing ────────────────────────────────────────────

        /// <summary>Draws the 12-hour or 60-minute labels on the clock face.</summary>
        private void DrawNumberLabels()
        {
            // Remove old labels
            for (int i = ClockCanvas.Children.Count - 1; i >= 0; i--)
                if (ClockCanvas.Children[i] is FrameworkElement fe &&
                    (string?)fe.Tag == "label")
                    ClockCanvas.Children.RemoveAt(i);

            if (_isHourMode)
            {
                for (int h = 1; h <= 12; h++)
                {
                    double angle = (h / 12.0) * 360.0 - 90.0;
                    var (x, y) = PolarToCanvas(angle, NUMBER_R);
                    bool isActive = (h == (_hour % 12 == 0 ? 12 : _hour % 12));

                    // Highlight circle
                    var circle = new Ellipse
                    {
                        Width = 30,
                        Height = 30,
                        Fill = isActive ? new SolidColorBrush(Color.FromRgb(83, 74, 183))
                                        : Brushes.Transparent,
                        Tag = "label"
                    };
                    Canvas.SetLeft(circle, x - 15);
                    Canvas.SetTop(circle, y - 15);
                    ClockCanvas.Children.Add(circle);

                    // Number text
                    var tb = new TextBlock
                    {
                        Text = h.ToString(),
                        FontSize = 13,
                        FontWeight = isActive ? FontWeights.Medium : FontWeights.Normal,
                        Foreground = isActive
                            ? new SolidColorBrush(Color.FromRgb(238, 237, 254))
                            : new SolidColorBrush(Color.FromRgb(44, 44, 42)),
                        TextAlignment = TextAlignment.Center,
                        Width = 30,
                        Tag = "label"
                    };
                    Canvas.SetLeft(tb, x - 15);
                    Canvas.SetTop(tb, y - 10);
                    ClockCanvas.Children.Add(tb);
                }
            }
            else // Minute mode
            {
                // 5-minute tick marks
                for (int m = 0; m < 60; m += 5)
                {
                    double angle = (m / 60.0) * 360.0 - 90.0;
                    var (x, y) = PolarToCanvas(angle, NUMBER_R);
                    bool isActive = (m == _minute);
                    string label = m == 0 ? "00" : m.ToString();

                    var circle = new Ellipse
                    {
                        Width = 30,
                        Height = 30,
                        Fill = isActive ? new SolidColorBrush(Color.FromRgb(29, 158, 117))
                                        : Brushes.Transparent,
                        Tag = "label"
                    };
                    Canvas.SetLeft(circle, x - 15);
                    Canvas.SetTop(circle, y - 15);
                    ClockCanvas.Children.Add(circle);

                    var tb = new TextBlock
                    {
                        Text = label,
                        FontSize = 12,
                        FontWeight = isActive ? FontWeights.Medium : FontWeights.Normal,
                        Foreground = isActive
                            ? new SolidColorBrush(Color.FromRgb(225, 245, 238))
                            : new SolidColorBrush(Color.FromRgb(44, 44, 42)),
                        TextAlignment = TextAlignment.Center,
                        Width = 30,
                        Tag = "label"
                    };
                    Canvas.SetLeft(tb, x - 15);
                    Canvas.SetTop(tb, y - 10);
                    ClockCanvas.Children.Add(tb);
                }

                // Minute tick dashes (every 1 min, between the 5-min labels)
                for (int m = 0; m < 60; m++)
                {
                    if (m % 5 == 0) continue;
                    double angle = (m / 60.0) * 360.0 - 90.0;
                    var (x1, y1) = PolarToCanvas(angle, OUTER_RADIUS - 4);
                    var (x2, y2) = PolarToCanvas(angle, OUTER_RADIUS - 10);
                    var line = new Line
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        Stroke = new SolidColorBrush(Color.FromRgb(211, 209, 199)),
                        StrokeThickness = 1,
                        Tag = "label"
                    };
                    ClockCanvas.Children.Add(line);
                }
            }
        }

        /// <summary>Positions all hands and the selection indicator.</summary>
        private void UpdateClock()
        {
            // ── Hour hand ──
            double hrAngle = ((_hour % 12) / 12.0) * 360.0 - 90.0;
            var (hx, hy) = PolarToCanvas(hrAngle, HR_HAND_LEN);
            HourHand.X1 = CX; HourHand.Y1 = CY;
            HourHand.X2 = hx; HourHand.Y2 = hy;

            // ── Minute hand ──
            double minAngle = (_minute / 60.0) * 360.0 - 90.0;
            var (mx, my) = PolarToCanvas(minAngle, MIN_HAND_LEN);
            MinuteHand.X1 = CX; MinuteHand.Y1 = CY;
            MinuteHand.X2 = mx; MinuteHand.Y2 = my;

            // ── Selection indicator ──
            double selAngle = _isHourMode ? hrAngle : minAngle;
            double selLen = _isHourMode ? HR_HAND_LEN : MIN_HAND_LEN;
            var (sx, sy) = PolarToCanvas(selAngle, selLen);

            var selColor = _isHourMode
                ? Color.FromRgb(83, 74, 183)
                : Color.FromRgb(29, 158, 117);

            SelectionHighlight.Fill = new SolidColorBrush(selColor) { Opacity = 0.15 };
            Canvas.SetLeft(SelectionHighlight, sx - DOT_RADIUS);
            Canvas.SetTop(SelectionHighlight, sy - DOT_RADIUS);

            SelectionDot.Fill = new SolidColorBrush(selColor);
            Canvas.SetLeft(SelectionDot, sx - 8);
            Canvas.SetTop(SelectionDot, sy - 8);

            // ── Time display ──
            int displayHour = _hour % 12 == 0 ? 12 : _hour % 12;
            TimeDisplay.Text = $"{displayHour:D2}:{_minute:D2}";

            // ── Update DP ──
            SelectedTime = new TimeSpan(_hour, _minute, 0);
        }

        // ── Mouse interaction ──────────────────────────────────

        private void ClockCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            ClockCanvas.CaptureMouse();
            ApplyAngle(e.GetPosition(ClockCanvas));
        }

        private void ClockCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            ApplyAngle(e.GetPosition(ClockCanvas));
        }

        private void ClockCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            ClockCanvas.ReleaseMouseCapture();

            // Auto-switch to minute mode after picking hour
            if (_isHourMode)
                SetMode(isHour: false);
        }

        private void ClockCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ClockCanvas.ReleaseMouseCapture();
            }
        }

        /// <summary>Converts a mouse position to a clock value and updates state.</summary>
        private void ApplyAngle(Point pos)
        {
            double dx = pos.X - CX;
            double dy = pos.Y - CY;
            double angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI + 90.0;
            if (angleDeg < 0) angleDeg += 360.0;

            if (_isHourMode)
            {
                int h = (int)Math.Round(angleDeg / 30.0) % 12;
                if (h == 0) h = 12;
                // Map to 24-hour
                _hour = _isAmPm
                    ? (h == 12 ? 0 : h)
                    : (h == 12 ? 12 : h + 12);
            }
            else
            {
                _minute = (int)Math.Round(angleDeg / 6.0) % 60;
            }

            DrawNumberLabels();
            UpdateClock();
        }

        // ── Mode / AM-PM toggles ───────────────────────────────

        private void HourModeButton_Checked(object sender, RoutedEventArgs e)
        {
            MinuteModeButton.IsChecked = false;
            SetMode(isHour: true);
        }

        private void MinuteModeButton_Checked(object sender, RoutedEventArgs e)
        {
            HourModeButton.IsChecked = false;
            SetMode(isHour: false);
        }

        private void SetMode(bool isHour)
        {
            _isHourMode = isHour;
            HourModeButton.IsChecked = isHour;
            MinuteModeButton.IsChecked = !isHour;
            DrawNumberLabels();
            UpdateClock();
        }

        private void AmButton_Checked(object sender, RoutedEventArgs e)
        {
            PmButton.IsChecked = false;
            _isAmPm = true;
            if (_hour >= 12) _hour -= 12;
            DrawNumberLabels();
            UpdateClock();
        }

        private void PmButton_Checked(object sender, RoutedEventArgs e)
        {
            AmButton.IsChecked = false;
            _isAmPm = false;
            if (_hour < 12) _hour += 12;
            DrawNumberLabels();
            UpdateClock();
        }

        // ── OK button ──────────────────────────────────────────

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            TimeConfirmed?.Invoke(this, SelectedTime);
        }

        // ── Helper ─────────────────────────────────────────────

        /// <summary>Converts polar angle + radius to canvas (x, y) coordinates.</summary>
        private static (double x, double y) PolarToCanvas(double angleDeg, double radius)
        {
            double rad = angleDeg * Math.PI / 180.0;
            return (CX + Math.Cos(rad) * radius, CY + Math.Sin(rad) * radius);
        }

    }
}
