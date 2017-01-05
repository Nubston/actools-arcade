using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using AcManager.Controls.Presentation;
using AcManager.Tools.Data;
using AcManager.Tools.Helpers;
using AcManager.Tools.Managers;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Controls;
using Newtonsoft.Json;
using Application = System.Windows.Application;

namespace ArcadeCorsa {
    public class Options {
        [JsonProperty("acRoot")]
        public string AcRoot;
    }

    public class EntryPoint {
        [STAThread]
        public static void Main() {
            if (!Debugger.IsAttached) {
                SetUnhandledExceptionHandler();
            }

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            new App().Run();
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void SetUnhandledExceptionHandler() {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UnhandledExceptionFancyHandler(Exception e) {
            if (!Application.Current.Windows.OfType<Window>().Any(x => x.IsLoaded && x.IsVisible)) {
                throw new Exception();
            }

            DpiAwareWindow.OnFatalError(e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UnhandledExceptionHandler(Exception e) {
            var text = e?.ToString() ?? @"?";

            try {
                UnhandledExceptionFancyHandler(e);
            } catch {
                System.Windows.Forms.MessageBox.Show(text, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Environment.Exit(1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args) {
            Application.Current.Dispatcher.Invoke(() => {
                var e = args.ExceptionObject as Exception;
                UnhandledExceptionHandler(e);
            });
        }
    }

    public partial class App {
        public App() {
            FilesStorage.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AcTools Arcade Corsa"));
            ValuesStorage.Initialize(FilesStorage.Instance.GetFilename("Values.data"), null);
            CacheStorage.Initialize(FilesStorage.Instance.GetFilename("Cache.data"));
            NonfatalError.Initialize();
            LimitedSpace.Initialize();
            LimitedStorage.Initialize();
            DataProvider.Initialize();
            DpiAwareWindow.OptionScale = 2d;

            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 20 });

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Resources.MergedDictionaries.Add(new SharedResourceDictionary {
                Source = new Uri("/FirstFloor.ModernUI;component/Assets/ModernUI.xaml", UriKind.Relative)
            });

            Resources.MergedDictionaries.Add(new SharedResourceDictionary {
                Source = new Uri("/Arcade Corsa;component/Assets/Theme.xaml", UriKind.Relative)
            });

            if (MainExecutingFile.Name.IndexOf("fancy", StringComparison.OrdinalIgnoreCase) != -1) {
                Resources["EffectsEnabled"] = true;
                Resources["ShadowEffect"] = new DropShadowEffect {
                    RenderingBias = RenderingBias.Performance,
                    BlurRadius = 30,
                    Direction = -90,
                    Color = Colors.Black,
                    ShadowDepth = 4,
                    Opacity = 0.6
                };
            } else {
                Resources["EffectsEnabled"] = false;
                Resources["ShadowEffect"] = null;
            }

            var config = Path.Combine(MainExecutingFile.Directory, "Config.json");
            string acRoot;
            if (File.Exists(config)) {
                var options = JsonConvert.DeserializeObject<Options>(File.ReadAllText(config));
                if (options.AcRoot == null) {
                    options.AcRoot = AcRootDirectory.TryToFind();
                    File.WriteAllText(config, JsonConvert.SerializeObject(options));
                }

                acRoot = options.AcRoot;
            } else {
                acRoot = AcRootDirectory.TryToFind();
            }

            AcRootDirectory.Initialize(acRoot);
            if (!AcRootDirectory.Instance.IsReady) {
                ModernDialog.ShowMessage("Can’t find AC root directory. Please, specify it using Config.json file.");
                return;
            }

            Superintendent.Initialize();

            StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e) {
            Storage.SaveBeforeExit();
        }
    }
}
