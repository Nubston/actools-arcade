using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AcManager.Tools.Helpers;
using AcManager.Tools.SemiGui;
using AcTools.Processes;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;

namespace ArcadeCorsa {
    public partial class MainWindow {
        private static Storage _lapTimesStorage = new Storage(FilesStorage.Instance.GetFilename("Lap Times.data"), disableCompression: true);

        public class LapTimeEntry {
            public int Place { get; set; }

            public TimeSpan Time { get; set; }

            public string Name { get; set; }
        }

        private static IEnumerable<LapTimeEntry> GetLapTimes(string carId) {
            var key = carId.ToLowerInvariant();
            return _lapTimesStorage.GetStringList(key).Select(x => x.Split(';')).Select(x => new LapTimeEntry {
                Name = x[0],
                Time = TimeSpan.Parse(x[1], CultureInfo.InvariantCulture)
            }).OrderBy(x => x.Time).Take(10).Select((x, i) => {
                x.Place = i + 1;
                return x;
            });
        }

        private static void SetLapTime(string carId, string name, TimeSpan time) {
            var key = carId.ToLowerInvariant();
            _lapTimesStorage.SetStringList(key, _lapTimesStorage.GetStringList(key).Append($"{name};{time}"));
        }
    }
}