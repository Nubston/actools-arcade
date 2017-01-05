using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AcManager.Tools.SemiGui;
using AcTools.Processes;

namespace ArcadeCorsa {
    public partial class MainWindow : IGameUi {
        async void IGameUi.Show(Game.StartProperties properties) {
            _resultShown = false;

            if (Model.Renderer != null) {
                Model.Renderer.SetCameraHigher = false;
            }

            await Task.Delay(1000);
            ProgressBar.Visibility = Visibility.Visible;
            LoadingText.Visibility = Visibility.Collapsed;
            PlayerNamePanel.Visibility = Visibility.Collapsed;
            GameResultOk.Visibility = Visibility.Hidden;
            LoadingPanel.SetValue(VisibilityAnimation.VisibleProperty, true);

            await Task.Delay(2000);
            DisableRenderer();
        }

        void IGameUi.OnProgress(Game.ProgressState progress) {}

        private bool _resultShown;
        private TimeSpan? _bestLapTime;

        void IGameUi.OnResult(Game.Result result, ReplayHelper replayHelper) {
            EnableRenderer(false);
            ProgressBar.Visibility = Visibility.Collapsed;

            var time = result?.GetExtraByType<Game.ResultExtraBestLap>()?.Time;
            if (!time.HasValue || time.Value == TimeSpan.Zero) {
                _bestLapTime = null;
                RevertUi(true);
            } else {
                _resultShown = true;
                _bestLapTime = time;
                LoadingText.Visibility = Visibility.Visible;
                LoadingText.FontSize = 20d;
                LoadingText.Text = "Best time: " + string.Format("{0:mm}:{0:ss}:{0:fff}", time.Value);
                GameResultOk.Visibility = Visibility.Visible;
                PlayerNamePanel.Visibility = Visibility.Visible;
            }
        }

        void IGameUi.OnError(Exception exception) {
            _resultShown = true;
            _bestLapTime = null;

            EnableRenderer(false);

            ProgressBar.Visibility = Visibility.Collapsed;
            LoadingText.Visibility = Visibility.Visible;
            LoadingText.FontSize = 11d;
            LoadingText.Text = exception.ToString();
            GameResultOk.Visibility = Visibility.Visible;
        }

        void IDisposable.Dispose() {
            RevertUi(false);
        }

        public async void RevertUi(bool force) {
            if (_resultShown && !force) return;
            LoadingPanel.SetValue(VisibilityAnimation.VisibleProperty, false);
            await Task.Delay(200);
            if (Model.Renderer != null) {
                Model.Renderer.SetCameraHigher = true;
            }
        }

        CancellationToken IGameUi.CancellationToken { get; } = default(CancellationToken);

        private void GameResultOk_OnClick(object sender, RoutedEventArgs e) {
            if (_bestLapTime.HasValue) {
                SetLapTime(Model.Car.Id, string.IsNullOrWhiteSpace(PlayerName.Text) ? "Player" : PlayerName.Text.Trim(), _bestLapTime.Value);
                Model.UpdateLapTimes();
            }

            RevertUi(true);
        }
    }
}