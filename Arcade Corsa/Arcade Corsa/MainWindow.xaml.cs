using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AcManager.Controls.CustomShowroom;
using AcManager.Tools.Helpers;
using AcManager.Tools.Managers;
using AcManager.Tools.Objects;
using AcManager.Tools.SemiGui;
using AcTools.Processes;
using AcTools.Render.Base;
using AcTools.Render.Kn5Specific.Textures;
using AcTools.Render.Wrapper;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using AcTools.Windows;
using ArcadeCorsa.Render.DarkRenderer;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using JetBrains.Annotations;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ArcadeCorsa {
    public partial class MainWindow : INotifyPropertyChanged, IAnyFactory<IGameUi> {
        private ViewModel Model => (ViewModel)DataContext;

        public MainWindow() {
            GameWrapper.RegisterFactory(this);
            DataContext = new ViewModel();
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(Scene, _bestQuality ? BitmapScalingMode.HighQuality : BitmapScalingMode.LowQuality);
        }

        IGameUi IAnyFactory<IGameUi>.Create() {
            return this;
        }

        public Visual SceneVisual => Scene;

        public class ViewModel : NotifyPropertyChanged {
            public readonly List<string> Cars;

            public List<SolidColorBrush> CustomColors { get; }

            public ViewModel() {
                Cars = new List<string> {
                    "ks_audi_r8_plus",
                    "ferrari_f40",
                    "ruf_yellowbird",
                    "shelby_cobra_427sc",
                    "ks_bmw_m4",
                    "ks_alfa_romeo_4c",
                    "ks_ford_mustang_2015",
                    "ks_porsche_718_boxster_s",
                    "peugeot_504",
                    "ks_audi_a1s1",
                    "ks_corvette_c7_stingray",
                    "mclaren_mp412c",
                    "lotus_elise_sc",
                    "ks_mclaren_p1",
                    "mazda_miata",
                    "ks_lamborghini_miura_sv",
                }.Where(x => CarsManager.Instance.GetWrapperById(x) != null).ToList();
                CustomColors = ColorsPalette.CustomColors.Select(x => new SolidColorBrush(x)).ToList();
                _customColor = CustomColors.Last();

                Car = CarsManager.Instance.GetById(Cars.FirstOrDefault());
                TexturesProviderBase.OptionOverrideAsync = true;
            }

            private DarkKn5ObjectRenderer _renderer;

            [CanBeNull]
            public DarkKn5ObjectRenderer Renderer {
                get { return _renderer; }
                set {
                    if (Equals(value, _renderer)) return;
                    _renderer = value;

                    if (_customColor != null) {
                        ApplyColor(_customColor.Color);
                    }

                    if (value != null && ValuesStorage.GetBool("he")) {
                        value.CarLightsEnabled = true;
                    }

                    OnPropertyChanged();
                }
            }

            private bool _stats;

            public bool Stats {
                get { return _stats; }
                set {
                    if (Equals(value, _stats)) return;
                    _stats = value;
                    OnPropertyChanged();
                }
            }

            private CancellationTokenSource _cancellation;
            public bool IsBusy => _cancellation != null;

            private async Task SetCarAsync(int offset) {
                _cancellation?.Cancel();

                CancellationTokenSource token = null;
                try {
                    using (token = new CancellationTokenSource()) {
                        _cancellation = token;
                        OnPropertyChanged(nameof(IsBusy));

                        var id = Cars[((Cars.IndexOf(Car.Id) + offset) % Cars.Count + Cars.Count) % Cars.Count];

                        var car = await CarsManager.Instance.GetByIdAsync(id);
                        Car = car;

                        if (Renderer != null) {
                            await Renderer.SetCarAsync(car,
                                    // ar.SelectedSkin.Id,
                                    null,
                                    _cancellation.Token);
                            if (_customColor != null) {
                                ApplyColor(_customColor.Color);
                            }
                        }
                    }
                } finally {
                    if (token != null && ReferenceEquals(_cancellation, token)) {
                        _cancellation = null;
                        OnPropertyChanged(nameof(IsBusy));
                    }
                }
            }

            private DelegateCommand _hideCarCommand;

            public DelegateCommand HideCarCommand => _hideCarCommand ?? (_hideCarCommand = new DelegateCommand(() => {
                Renderer?.SetCarAsync(null).Forget();
            }));

            private DelegateCommand _previousCarCommand;

            public DelegateCommand PreviousCarCommand => _previousCarCommand ?? (_previousCarCommand = new DelegateCommand(() => {
                SetCarAsync(-1).Forget();
            }));

            private DelegateCommand _nextCarCommand;

            public DelegateCommand NextCarCommand => _nextCarCommand ?? (_nextCarCommand = new DelegateCommand(() => {
                SetCarAsync(1).Forget();
            }));

            private double? _accelerationValue;

            public double? AccelerationValue {
                get { return _accelerationValue; }
                set {
                    value = value?.Saturate();
                    if (Equals(value, _accelerationValue)) return;
                    _accelerationValue = value;
                    OnPropertyChanged();
                }
            }

            private double? _topSpeedValue;

            public double? TopSpeedValue {
                get { return _topSpeedValue; }
                set {
                    value = value?.Saturate();
                    if (Equals(value, _topSpeedValue)) return;
                    _topSpeedValue = value;
                    OnPropertyChanged();
                }
            }

            private double? _weightValue;

            public double? WeightValue {
                get { return _weightValue; }
                set {
                    value = value?.Saturate();
                    if (Equals(value, _weightValue)) return;
                    _weightValue = value;
                    OnPropertyChanged();
                }
            }

            private double _handlingValue;

            public double HandlingValue {
                get { return _handlingValue; }
                set {
                    value = value.Saturate();
                    if (Equals(value, _handlingValue)) return;
                    _handlingValue = value;
                    OnPropertyChanged();
                }
            }

            private string _tyres;

            public string Tyres {
                get { return _tyres; }
                set {
                    if (Equals(value, _tyres)) return;
                    _tyres = value;
                    OnPropertyChanged();
                }
            }

            private async Task LoadSpecs(CarObject value) {
                var acc = FlexibleParser.ParseDouble(value.SpecsAcceleration, 100);
                AccelerationValue = acc < 0.1 ? (double?)null : 1.0 - (acc - 2.0) / 10.0;
                TopSpeedValue = (FlexibleParser.TryParseDouble(value.SpecsTopSpeed) - 150) / 200;
                WeightValue = 1.0 - (FlexibleParser.TryParseDouble(value.SpecsWeight) - 600) / 1200;

                var tyres = await Task.Run(() => value.AcdData.GetIniFile("tyres.ini"));
                var tyresNames = tyres.Values.Select(x => x.GetNonEmpty("NAME")).Where(x => x != null).Distinct().ToList();
                Tyres = tyresNames.JoinToString(", ");

                var handlingValue = 0d;
                foreach (var name in tyresNames) {
                    switch (name.ToLowerInvariant()) {
                        case "michelin xas":
                            handlingValue = Math.Max(handlingValue, 0.1);
                            break;
                        case "street70s":
                        case "street 70s":
                            handlingValue = Math.Max(handlingValue, 0.2);
                            break;
                        case "cinturato":
                        case "vintage":
                        case "street90s":
                        case "street 90s":
                            handlingValue = Math.Max(handlingValue, 0.3);
                            break;
                        case "street":
                        case "modern street":
                            handlingValue = Math.Max(handlingValue, 0.4);
                            break;
                        case "semislick":
                        case "semislicks":
                            handlingValue = Math.Max(handlingValue, 0.5);
                            break;
                        case "slick hard":
                            handlingValue = Math.Max(handlingValue, 0.6);
                            break;
                        case "slick medium":
                            handlingValue = Math.Max(handlingValue, 0.7);
                            break;
                        case "slicks":
                        case "slick soft":
                            handlingValue = Math.Max(handlingValue, 0.8);
                            break;
                        case "hypercar trofeo":
                            handlingValue = Math.Max(handlingValue, 0.9);
                            break;
                    }
                }

                HandlingValue = handlingValue * (0.8 + 0.4 * (WeightValue ?? 0d));
            }

            private CarObject _car;

            public CarObject Car {
                get { return _car; }
                set {
                    if (Equals(value, _car)) return;
                    _car = value;
                    OnPropertyChanged();

                    UpdateLapTimes();
                    LoadSpecs(value).Forget();

                    // _carSkin = Car.SelectedSkin;
                    // CustomColor = null;
                    // OnPropertyChanged(nameof(CarSkin));
                }
            }

            private SolidColorBrush _customColor;

            private void ApplyColor(Color color) {
                var carNode = Renderer?.CarNode;
                if (carNode == null) return;
                Renderer.SelectSkin(null);
                var paintable = PaintShop.GetPaintableItems(Car.Id, carNode.OriginalFile);
                var carPaint = paintable.OfType<PaintShop.CarPaint>().FirstOrDefault();
                if (carPaint != null) {
                    carPaint.SetRenderer(Renderer);
                    carPaint.Color = color;
                    carPaint.Flakes = MathUtils.Random() > 0.5;
                    carPaint.ApplyImmediate();
                }
            }

            [CanBeNull]
            public SolidColorBrush CustomColor {
                get { return _customColor; }
                set {
                    if (Equals(value, _customColor)) return;
                    _customColor = value;

                    if (value != null) {
                        CarSkin = null;
                        ApplyColor(value.Color);
                    }

                    OnPropertyChanged();
                }
            }

            private string _licensePlateText;

            public string LicensePlateText {
                get { return _licensePlateText; }
                set {
                    if (Equals(value, _licensePlateText)) return;
                    _licensePlateText = value;

                    if (value != null) {
                        /*var carNode = Renderer.CarNode;
                        if (carNode == null) return;

                        CarSkin = null;
                        Renderer.SelectSkin(null);

                        var paintable = PaintShop.GetPaintableItems(Car.Id, carNode.OriginalFile);
                        var carPaint = paintable.OfType<PaintShop.LicensePlate>().FirstOrDefault();
                        if (carPaint != null) {
                            carPaint.SetRenderer(Renderer);
                            carPaint.Text = value;
                        }*/
                    }

                    OnPropertyChanged();
                }
            }

            private CarSkinObject _carSkin;

            [CanBeNull]
            public CarSkinObject CarSkin {
                get { return _carSkin; }
                set {
                    if (Equals(value, _carSkin)) return;
                    _carSkin = value;
                    if (Car != null && value != null) {
                        Car.SelectedSkin = value;
                    }
                    if (!IsBusy && value != null) {
                        Renderer?.CarNode?.ClearProceduralOverrides();
                        CustomColor = null;
                        Renderer?.SelectSkin(value.Id);
                    }
                    OnPropertyChanged();
                }
            }

            private List<LapTimeEntry> _lapTimes;

            public List<LapTimeEntry> LapTimes {
                get { return _lapTimes; }
                set {
                    if (Equals(value, _lapTimes)) return;
                    _lapTimes = value;
                    OnPropertyChanged();
                }
            }

            public void UpdateLapTimes() {
                LapTimes = GetLapTimes(Car.Id).ToList();
            }

            private AsyncCommand _goCommand;

            public AsyncCommand GoCommand => _goCommand ?? (_goCommand = new AsyncCommand(async () => {
                try {
                    var sunAngle = MathUtils.Random(-70d, 70d);
                    var seconds = Game.ConditionProperties.GetSeconds(sunAngle);
                    var weather = (WeatherObject)WeatherManager.Instance.WrappersList.Where(x => x.Value.Enabled).RandomElement().Loaded();
                    var skinDirectory = FileUtils.EnsureUnique(Path.Combine(Car.SkinsDirectory, "__tmp"), "_{0}");

                    var renderer = Renderer;
                    if (renderer?.CarNode != null && _customColor != null) {
                        var paintable = PaintShop.GetPaintableItems(Car.Id, renderer.CarNode.OriginalFile);
                        var carPaint = paintable.OfType<PaintShop.CarPaint>().FirstOrDefault();

                        if (carPaint != null) {
                            Directory.CreateDirectory(skinDirectory);
                            await renderer.SaveTexture(Path.Combine(skinDirectory, carPaint.DiffuseTexture), _customColor.Color.ToColor());
                        }
                    }

                    ((Func<Task>)(async () => {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        Directory.Delete(skinDirectory, true);
                    }))().Forget();

                    await GameWrapper.StartAsync(new Game.StartProperties(new Game.BasicProperties {
                        CarId = Car.Id,
                        CarSkinId = Path.GetFileName(skinDirectory),
                        DriverName = "Player",
                        TrackId = "magione"
                    }, null, new Game.ConditionProperties {
                        AmbientTemperature = 20d,
                        CloudSpeed = 2d,
                        SunAngle = sunAngle,
                        RoadTemperature = Game.ConditionProperties.GetRoadTemperature(seconds, 20d, weather.TemperatureCoefficient),
                        TimeMultipler = 1d,
                        WeatherName = weather.Id
                    }, Game.DefaultTrackPropertiesPresets.First().Properties, new Game.HotlapProperties {
                        GhostCar = true,
                        RecordGhostCar = true,
                        Penalties = true
                    }));
                } catch (Exception e) {
                    MessageBox.Show(e.ToString());
                }
            }));
        }
        
        private Kn5WrapperCameraControlHelper _cameraControl;

        private void OnLoaded(object sender, RoutedEventArgs e) {
            _cameraControl = new Kn5WrapperCameraControlHelper();
            EnableRenderer(true);

            if (MainExecutingFile.Name.IndexOf("simple", StringComparison.OrdinalIgnoreCase) != -1) {
                using (var renderer = new DarkKn5ObjectRenderer(Model.Car)) {
                    new BaseKn5FormWrapper(renderer, "Simple Mode", 1000, 800).Run();
                }

                if (MainExecutingFile.Name.IndexOf("exit", StringComparison.OrdinalIgnoreCase) != -1) {
                    Environment.Exit(0);
                }
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            DisableRenderer();
        }

        private D3DImageEx _imageEx;
        
        private IntPtr _lastTarget;
        private int _setCount;

        private void EnableRenderer(bool cameraHigher) {
            try {
                if (_imageEx != null) return;

                var renderer = new DarkKn5ObjectRenderer(Model.Car);
                renderer.SelectSkin(null);
                renderer.Initialize();
                renderer.SetCameraHigher = cameraHigher;
                SetRendererSize(renderer);
                renderer.Draw();
                Model.Renderer = renderer;

                _imageEx = new D3DImageEx();
                Scene.Source = _imageEx;
                _setCount = 0;
                _lastTarget = IntPtr.Zero;

                CompositionTarget.Rendering += OnRendering;
                VisibilityAnimation.SetVisible(Scene, true);
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }
        }

        private async void DisableRenderer() {
            if (_imageEx == null || VisibilityAnimation.GetVisible(Scene) == false) return;

            try {
                VisibilityAnimation.SetVisible(Scene, false);
                await Task.Delay(500);

                if (Model.Renderer != null) {
                    Model.Renderer.Dispose();
                    Model.Renderer = null;
                }

                CompositionTarget.Rendering -= OnRendering;

                Scene.Source = null;

                _imageEx.Lock();
                _imageEx.SetBackBufferEx(D3DResourceTypeEx.ID3D11Texture2D, IntPtr.Zero);
                _imageEx.Unlock();
                _imageEx = null;
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }
        }

        private static TimeSpan _last = TimeSpan.Zero;

        private void OnRendering(object sender, EventArgs e) {
            var args = (RenderingEventArgs)e;

            if (args.RenderingTime == _last) return;
            _last = args.RenderingTime;

            var renderer = Model.Renderer;
            if (_imageEx == null || renderer == null) return;

            if (renderer.Draw()) {
                _lastTarget = IntPtr.Zero;
            }

            var target = renderer.GetRenderTarget();
            if (target != _lastTarget || _setCount < 3) {
                _imageEx.SetBackBufferEx(D3DResourceTypeEx.ID3D11Texture2D, target);
                _setCount++;
                _lastTarget = target;
            }

            _imageEx.Lock();
            _imageEx.AddDirtyRect(new Int32Rect {
                X = 0,
                Y = 0,
                Height = _imageEx.PixelHeight,
                Width = _imageEx.PixelWidth
            });
            _imageEx.Unlock();
        }

        private void SetRendererSize([NotNull] BaseRenderer renderer) {
            renderer.Width = (int)(ActualWidth * (_bestQuality ? 1.41 : 1.0));
            renderer.Height = (int)(ActualHeight * (_bestQuality ? 1.41 : 1.0));
        }

        protected override void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (Model.Renderer != null) {
                SetRendererSize(Model.Renderer);
            }
        }

        private bool _bestQuality = ValuesStorage.GetBool("bq");

        public bool BestQuality {
            get { return _bestQuality; }
            set {
                if (Equals(value, _bestQuality)) return;
                _bestQuality = value;
                OnPropertyChanged();
                if (Model.Renderer != null) {
                    SetRendererSize(Model.Renderer);
                }
                ValuesStorage.Set("bq", value);
                RenderOptions.SetBitmapScalingMode(Scene, value ? BitmapScalingMode.HighQuality : BitmapScalingMode.LowQuality);
            }
        }

        private Point _mousePos;
        private Point _startMousePos;
        private Point _lastMousePos;

        private bool _moved, _moving, _down;

        private void Scene_OnMouseMove(object sender, MouseEventArgs e) {
            if (!IsActive) {
                _moving = false;
                return;
            }

            _mousePos = e.GetPosition(Scene);

            if (Math.Abs(_mousePos.X - _startMousePos.X) > 2 || Math.Abs(_mousePos.Y - _startMousePos.Y) > 2) {
                _moved = true;
            }

            if (_moving && !_down) {
                var dx = _mousePos.X - _lastMousePos.X;
                var dy = _mousePos.Y - _lastMousePos.Y;

                var renderer = Model.Renderer;
                if (Keyboard.Modifiers == ModifierKeys.Alt) {
                    if (e.MiddleButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed && User32.IsKeyPressed(Keys.Space)) {
                        _cameraControl?.CameraMousePan(renderer, dx, dy, Scene.ActualWidth * OptionScale, Scene.ActualHeight * OptionScale);
                    } else if (e.LeftButton == MouseButtonState.Pressed) {
                        _cameraControl?.CameraMouseRotate(renderer, dx, dy, Scene.ActualWidth * OptionScale, Scene.ActualHeight * OptionScale);
                    } else if (e.RightButton == MouseButtonState.Pressed) {
                        _cameraControl?.CameraMouseZoom(renderer, dx, dy, Scene.ActualWidth * OptionScale, Scene.ActualHeight * OptionScale);
                    }
                }
            }

            _down = false;
            _lastMousePos = _mousePos;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Scene_OnMouseDown(object sender, MouseButtonEventArgs e) {
            _moved = false;
            _moving = true;
            _down = true;
            _startMousePos = _mousePos;
        }

        private void Scene_OnMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_moved) {
                //var pos = e.GetPosition(Scene);
                //Model.Renderer.OnClick(new Vector2((float)pos.X, (float)pos.Y));
            }
        }

        private async void OnKeyDown(object sender, KeyEventArgs e) {
            await Task.Delay(1);
            if (e.Handled) return;

            switch (e.Key) {
                case Key.Left:
                    Model.PreviousCarCommand.Execute();
                    break;
                case Key.Right:
                    Model.NextCarCommand.Execute();
                    break;
                case Key.L:
                    if (Model.Renderer != null) {
                        Model.Renderer.CarLightsEnabled = !Model.Renderer.CarLightsEnabled;
                    }
                    break;
                case Key.H:
                    if (Keyboard.Modifiers == ModifierKeys.Alt && Model.Renderer != null) {
                        Model.Renderer.SetCameraHigher = !Model.Renderer.SetCameraHigher;
                    }
                    break;
                /*case Key.PageUp:
                    Model.Renderer?.SelectPreviousSkin();
                    break;
                case Key.PageDown:
                    Model.Renderer?.SelectNextSkin();
                    break;*/
                case Key.Space:
                case Key.Insert:
                    if (Model.Renderer != null) {
                        Model.Renderer.AutoRotate = !Model.Renderer.AutoRotate;
                    }
                    break;
                case Key.Home:
                    Model.Renderer?.ResetCamera();
                    break;
                case Key.F1:
                    Model.Stats = !Model.Stats;
                    break;
                case Key.F2:
                    BestQuality = !BestQuality;
                    break;
                default:
                    return;
            }
            e.Handled = true;
        }

        private async void ToggleButton_OnChecked(object sender, RoutedEventArgs e) {
            await Task.Delay(1);
            ValuesStorage.Set("he", Model.Renderer?.CarLightsEnabled ?? false);
        }
    }
}
