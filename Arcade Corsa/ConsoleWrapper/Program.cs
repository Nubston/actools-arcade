using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AcManager.Tools.Data;
using AcManager.Tools.Helpers;
using AcManager.Tools.Managers;
using AcManager.Tools.Managers.Plugins;
using AcManager.Tools.SemiGui;
using AcTools.Processes;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using CommandLine;
using CommandLine.Text;
using FirstFloor.ModernUI.Helpers;
using Nito.AsyncEx;

namespace ConsoleWrapper {
    public enum Mode {
        Practice, HotLap, Race, Drift
    }

    public enum Assist {
        Gamer, Racer, Pro
    }

    public enum Starter {
        Official, Tricky, UiModule, Sse
    }

    public class Options {
        [Option('r', "ac-root", Required = true, HelpText = "AC root directory.")]
        public string AcRoot { get; set; }

        [Option("plugins-dir", DefaultValue = null, HelpText = "CM plugins directory (not needed by default).")]
        public string PluginsDir { get; set; }

        [Option('m', "mode", DefaultValue = Mode.HotLap, HelpText = "Session mode (Practice, HotLap, Race or Drift).")]
        public Mode Mode { get; set; }

        [Option("starter", DefaultValue = Starter.Official, HelpText = "Starter (Official, Tricky or UiModule).")]
        public Starter Starter { get; set; }

        [Option('a', "assists", DefaultValue = Assist.Pro, HelpText = "Assists level (Gamer, Racer or Pro).")]
        public Assist Assist { get; set; }

        [Option('c', "car", DefaultValue = "abarth500", HelpText = "Car ID.")]
        public string CarId { get; set; }

        [Option('s', "skin", DefaultValue = "0_white_scorpion", HelpText = "Car’s skin ID.")]
        public string CarSkinId { get; set; }

        [Option('t', "track", DefaultValue = "imola", HelpText = "Track ID (separate configuration by “/” if needed).")]
        public string TrackId { get; set; }

        [Option('d', "driver", DefaultValue = "Player", HelpText = "Driver name.")]
        public string DriverName { get; set; }

        [Option('w', "weather", DefaultValue = "3_clear", HelpText = "Weather ID.")]
        public string WeatherId { get; set; }

        [Option("temperature", DefaultValue = 20, HelpText = "Ambient temperature (C°).")]
        public double AmbientTemperature { get; set; }

        [Option('l', "laps", DefaultValue = 3, HelpText = "Number of laps for race mode.")]
        public int RaceLaps { get; set; }

        [Option('o', "opponents", DefaultValue = 7, HelpText = "Number of opponents for race mode.")]
        public int Opponents { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage() {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    public static class Program {
        private static Game.AssistsProperties GetAssistsProperties(Assist assist) {
            switch (assist) {
                case Assist.Gamer:
                    return new Game.AssistsProperties {
                        Abs = AssistState.On,
                        AutoBlip = true,
                        AutoBrake = true,
                        AutoClutch = true,
                        AutoShifter = true,
                        Damage = 0,
                        FuelConsumption = 0,
                        IdealLine = true,
                        SlipSteamMultipler = 3,
                        StabilityControl = 100,
                        TractionControl = AssistState.On,
                        TyreBlankets = true,
                        TyreWearMultipler = 0,
                        VisualDamage = true
                    };
                case Assist.Racer:
                    return new Game.AssistsProperties {
                        Abs = AssistState.On,
                        AutoBlip = true,
                        AutoBrake = false,
                        AutoClutch = true,
                        AutoShifter = false,
                        Damage = 0.5,
                        FuelConsumption = 0.5,
                        IdealLine = true,
                        SlipSteamMultipler = 2,
                        StabilityControl = 20,
                        TractionControl = AssistState.Factory,
                        TyreBlankets = false,
                        TyreWearMultipler = 0,
                        VisualDamage = true
                    };
                case Assist.Pro:
                    return new Game.AssistsProperties {
                        Abs = AssistState.Factory,
                        AutoBlip = false,
                        AutoBrake = false,
                        AutoClutch = false,
                        AutoShifter = false,
                        Damage = 1,
                        FuelConsumption = 1,
                        IdealLine = false,
                        SlipSteamMultipler = 1,
                        StabilityControl = 0,
                        TractionControl = AssistState.Factory,
                        TyreBlankets = false,
                        TyreWearMultipler = 1,
                        VisualDamage = true
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(assist), assist, null);
            }
        }

        private static Game.BaseModeProperties GetModeProperties(Options options) {
            switch (options.Mode) {
                case Mode.Practice:
                    return new Game.PracticeProperties {
                        Penalties = false
                    };
                case Mode.HotLap:
                    return new Game.HotlapProperties {
                        GhostCar = true,
                        RecordGhostCar = true,
                        Penalties = true
                    };
                case Mode.Race:
                    var skins = GoodShuffle.Get(CarsManager.Instance.GetById(options.CarId).SkinsManager.WrappersList
                                                           .Where(x => x.Value.Enabled).Select(x => x.Id));

                    return new Game.RaceProperties {
                        Penalties = true,
                        JumpStartPenalty = Game.JumpStartPenaltyType.DriveThrough,
                        AiLevel = 100,
                        RaceLaps = options.RaceLaps,
                        StartingPosition = options.Opponents,
                        BotCars = Enumerable.Range(1, options.Opponents).Select(x => new Game.AiCar {
                            AiLevel = MathUtils.Random(80, 100),
                            CarId = options.CarId,
                            DriverName = "Bot #" + x,
                            SkinId = skins.Next
                        })
                    };
                case Mode.Drift:
                    return new Game.DriftProperties {
                        Penalties = true,
                        StartType = Game.StartType.RegularStart
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static async Task MainAsync(Options options) {
            var sunAngle = MathUtils.Random(-70d, 70d);
            var seconds = Game.ConditionProperties.GetSeconds(sunAngle);
            var weather = WeatherManager.Instance.GetById(options.WeatherId);
            var track = options.TrackId.Split('/');

            switch (options.Starter) {
                case Starter.Official:
                    SettingsHolder.Drive.SelectedStarterType = SettingsHolder.Drive.StarterTypes.GetById("Official");
                    break;
                case Starter.Tricky:
                    SettingsHolder.Drive.SelectedStarterType = SettingsHolder.Drive.StarterTypes.GetById("Tricky");
                    break;
                case Starter.UiModule:
                    SettingsHolder.Drive.SelectedStarterType = SettingsHolder.Drive.StarterTypes.GetById("UI Module");
                    break;
                case Starter.Sse:
                    PluginsManager.Initialize(options.PluginsDir);
                    SettingsHolder.Drive.SelectedStarterType = SettingsHolder.Drive.StarterTypes.GetById("SSE");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var result = await GameWrapper.StartAsync(new Game.StartProperties(new Game.BasicProperties {
                CarId = options.CarId,
                CarSkinId = options.CarSkinId,
                TrackId = track[0],
                TrackConfigurationId = track.ElementAtOrDefault(1),
                DriverName = options.DriverName
            }, GetAssistsProperties(options.Assist), new Game.ConditionProperties {
                AmbientTemperature = options.AmbientTemperature,
                CloudSpeed = 2d,
                SunAngle = sunAngle,
                RoadTemperature = Game.ConditionProperties.GetRoadTemperature(seconds, options.AmbientTemperature, weather.TemperatureCoefficient),
                TimeMultipler = 1d,
                WeatherName = weather.Id
            }, Game.DefaultTrackPropertiesPresets.First().Properties, GetModeProperties(options)));

            if (result == null) {
                Console.WriteLine("Race cancelled");
                return;
            }

            switch (options.Mode) {
                case Mode.Practice:
                    return;
                case Mode.HotLap:
                    var bestLap = result.GetExtraByType<Game.ResultExtraBestLap>()?.Time;
                    if (bestLap.HasValue) {
                        Console.WriteLine(bestLap);
                    } else {
                        Console.WriteLine("Time has not been set");
                    }
                    return;
                case Mode.Race:
                    var position = result.Sessions.LastOrDefault(x => x.BestLaps.Any())?.CarPerTakenPlace?.IndexOf(0);
                    if (position.HasValue) {
                        Console.WriteLine(position);
                    } else {
                        Console.WriteLine("Position has not been taken");
                    }
                    return;
                case Mode.Drift:
                    var points = result.GetExtraByType<Game.ResultExtraDrift>()?.Points;
                    if (points.HasValue) {
                        Console.WriteLine(points);
                    } else {
                        Console.WriteLine("Score has not been set");
                    }
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int Main(string[] args) {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            try {
                var options = new Options();
                if (!Parser.Default.ParseArguments(args, options)) return 1;
                
                FilesStorage.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AcTools Console Wrapper"));
                ValuesStorage.Initialize(null, null);
                CacheStorage.Initialize(null);
                LimitedSpace.Initialize();
                LimitedStorage.Initialize();
                DataProvider.Initialize();

                AcRootDirectory.Initialize(options.AcRoot);
                if (!AcRootDirectory.Instance.IsReady) {
                    Console.Error.WriteLine("Invalid AC root directory");
                    return 3;
                }

                Superintendent.Initialize();

                AsyncContext.Run(() => MainAsync(options));
                return 0;
            } catch (InformativeException e) {
                Console.Error.WriteLine(e.Message);
                return 2;
            } catch (Exception e) {
                Console.Error.WriteLine(e.ToString());
                return 2;
            }
        }
    }
}
