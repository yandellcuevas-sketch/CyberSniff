using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace CyberSniff
{
    public partial class MainWindow : Window
    {
        // ─── Estado ────────────────────────────────────────────────
        private bool _sidebarExpanded = true;
        private const double SidebarExpandedWidth  = 200;
        private const double SidebarCollapsedWidth = 50;

        // Propiedad bindeable para mostrar/ocultar labels en sidebar
        public bool IsSidebarExpanded
        {
            get => _sidebarExpanded;
            set
            {
                _sidebarExpanded = value;
                // Notificar al binding de Visibility de las etiquetas
                // (en un proyecto real usar INotifyPropertyChanged o MVVM)
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(); // Conecta el ViewModel
        }

        // ─── Barra de título: arrastre ─────────────────────────────
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }

        // ─── Controles de ventana ──────────────────────────────────
        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => ToggleMaximize();

        private void ToggleMaximize()
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        // ─── Animación del sidebar ─────────────────────────────────
        /// <summary>
        /// Anima el ancho de la columna del sidebar usando QuinticEase
        /// para una sensación suave y orgánica.
        /// </summary>
        private void BtnToggleSidebar_Click(object sender, MouseButtonEventArgs e)
            => AnimateSidebar();

        private void AnimateSidebar()
        {
            _sidebarExpanded = !_sidebarExpanded;

            double targetWidth = _sidebarExpanded
                ? SidebarExpandedWidth
                : SidebarCollapsedWidth;

            // Anima la GridLength de la columna del sidebar
            var animation = new GridLengthAnimation
            {
                From     = SidebarColumn.Width,
                To       = new GridLength(targetWidth),
                Duration = new Duration(TimeSpan.FromMilliseconds(320)),
                EasingFunction = new QuinticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            SidebarColumn.BeginAnimation(
                ColumnDefinition.WidthProperty,
                animation);

            // Actualiza el ícono de toggle
            TxtToggleIcon.Text  = _sidebarExpanded ? "◁◁" : "▷▷";

            // Muestra / oculta las etiquetas de navegación suavemente
            // (la visibilidad está enlazada a IsSidebarExpanded mediante BoolToVis)
            IsSidebarExpanded = _sidebarExpanded;

            // Forzar actualización de los elementos con binding en sidebar
            // En proyecto real, disparar PropertyChanged aquí.
        }

        // ─── Captura ───────────────────────────────────────────────
        private void BtnCaptureToggle_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.ToggleCapture();

            // Swap visual del botón
            bool capturing = (DataContext as MainViewModel)?.IsCapturing ?? false;
            if (capturing)
            {
                BtnCaptureToggle.Style = (Style)FindResource("BtnDanger");
                TxtCaptureIcon.Text    = "⏹";
                TxtCaptureLabel.Text   = "Detener";
                TxtStatus.Text         = "Capturando";
            }
            else
            {
                BtnCaptureToggle.Style = (Style)FindResource("BtnPrimary");
                TxtCaptureIcon.Text    = "▶";
                TxtCaptureLabel.Text   = "Capturar";
                TxtStatus.Text         = "Pausado";
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
            => (DataContext as MainViewModel)?.ClearPackets();

        private void BtnExport_Click(object sender, RoutedEventArgs e)
            => (DataContext as MainViewModel)?.ExportPcap();

        // ─── Búsqueda ──────────────────────────────────────────────
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.FilterText = TxtSearch.Text;
        }

        // ─── Filtros rápidos de protocolo ──────────────────────────
        private void FilterAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.ProtocolFilter = null; // null = todos
        }

        // ─── Navegación ────────────────────────────────────────────
        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Aquí se gestiona la navegación entre vistas
            // En una app real, usar un Frame o ContentControl con DataTemplates
        }

        private void NavSettings_Click(object sender, MouseButtonEventArgs e)
        {
            // Abre el panel de ajustes (slide-in desde la derecha, por ejemplo)
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }

        // ─── Animación de entrada por fila nueva ───────────────────
        /// <summary>
        /// Cada fila nueva hace fade-in + slide desde arriba, dando
        /// retroalimentación visual de que llega un paquete nuevo.
        /// </summary>
        private void DataGridRow_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not DataGridRow row) return;

            // Solo anima las filas nuevas (las primeras no necesitan)
            if (row.GetIndex() > 3) return;

            var sb = new Storyboard();

            var fadeIn = new DoubleAnimation
            {
                From     = 0,
                To       = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, row);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

            // Requiere un TranslateTransform en el DataGridRow
            row.RenderTransform = new System.Windows.Media.TranslateTransform(0, -6);
            var slideIn = new DoubleAnimation
            {
                From     = -6,
                To       = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideIn, row);
            Storyboard.SetTargetProperty(slideIn,
                new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            sb.Children.Add(fadeIn);
            sb.Children.Add(slideIn);
            sb.Begin();
        }
    }

    // ─── GridLengthAnimation  (helper necesario para animar GridLength) ──
    /// <summary>
    /// WPF no incluye animación nativa de GridLength.
    /// Esta clase cubre ese gap.
    /// </summary>
    public class GridLengthAnimation : AnimationTimeline
    {
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register(nameof(From), typeof(GridLength),
                typeof(GridLengthAnimation));
        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register(nameof(To), typeof(GridLength),
                typeof(GridLengthAnimation));
        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register(nameof(EasingFunction), typeof(IEasingFunction),
                typeof(GridLengthAnimation));

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }
        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }
        public IEasingFunction EasingFunction
        {
            get => (IEasingFunction)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }

        public override Type TargetPropertyType => typeof(GridLength);

        public override object GetCurrentValue(object defaultOriginValue,
                                               object defaultDestinationValue,
                                               AnimationClock animationClock)
        {
            double from = From.Value;
            double to   = To.Value;
            double progress = animationClock.CurrentProgress ?? 0;

            if (EasingFunction != null)
                progress = EasingFunction.Ease(progress);

            return new GridLength(from + (to - from) * progress);
        }

        protected override Freezable CreateInstanceCore()
            => new GridLengthAnimation();
    }
}
