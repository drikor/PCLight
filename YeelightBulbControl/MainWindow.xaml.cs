using YeelightAPI;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Serilog;
using Newtonsoft.Json;
using System.Windows.Media;
using System.ComponentModel;

namespace YeelightBulbControl
{
    public partial class MainWindow : Window
    {
        private static ILogger logger = new LoggerConfiguration()
            .WriteTo.Debug()
            .WriteTo.File("log.txt")
            .CreateLogger();

        private Device device;
        private string hostname;
        private bool BulbPowerState;
        private byte r, g, b;
        private bool isConnected = false;

        // для Lightsync
        private Preset LightSyncCurrentPreset;

        // Лучше в будущем поменять, если у юзера нет пресетов, эта переменная будет true
        private bool DoesINeedToCreateDefaultPreset;

        // Путь к файлу конфигурации
        private string configFilePath;

        // Дефолтный пресет
        private Preset defaultPreset = new Preset
        {
            Name = "defaultPreset",
            Brightness = 100,
            ColorMode = 2,
            ColorRGB = 1,
            ColorTemperature = 4500
        };

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            MainScreen.Title = $"[DEBUG] {MainScreen.Title}" ;
            debug_label.Visibility = System.Windows.Visibility.Visible;
            debug_btn.Visibility = System.Windows.Visibility.Visible;
#endif
            logger.Information("Application started");

            // Получаем путь к папке в AppData/Local
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Создаем папку Drikor, если ее нет
            string drikorFolderPath = Path.Combine(appDataPath, "Drikor/PCLight");
            if (!Directory.Exists(drikorFolderPath))
            {
                logger.Information("First time huh. Appdata/Local/Drikor/PCLight folder.");
                Directory.CreateDirectory(drikorFolderPath);
            }

            // Задаем путь к файлу конфигурации
            configFilePath = Path.Combine(drikorFolderPath, "config.json");

            // Загружаем конфигурацию
            LoadConfig();

            logger.Information("Trying to connect automatically...");
            Connect();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            logger.Debug("NumberValidationTextBox method called");
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Switch_Button_Click(object sender, RoutedEventArgs e)
        {

            logger.Information("Switch_Button_Click method called");
            BulbPowerState = !BulbPowerState;
            device.SetPower(BulbPowerState);
            logger.Information("Bulb.power = {BulbPowerState}", BulbPowerState);
        }

        private async void Connect()
        {
            if (isConnected) 
            { 
                MessageBox.Show("Silly, you are already connected :3");
                return;
            }

            hostname = Hostname_TextBox.Text;
            if (hostname == null || hostname == "") return; 

            // никуда не годится. ИЗМЕНИТЬ!!

            Preset[] presets;

            if (DoesINeedToCreateDefaultPreset)
            {
                presets = new Preset[0];
                Array.Resize(ref presets, presets.Length + 1);
                presets[presets.Length - 1] = defaultPreset;
            }
            else
            {
                presets = LoadPresetsFromConfig();
            }

            // Сохраняем hostname в конфиге
            SaveConfig(hostname, presets);

            device = new Device(hostname);

            try
            {
                logger.Information($"Trying to connect: {hostname}...");
                await device.Connect();
                
                logger.Information("{Hostname} connected successfully", hostname);

                UpdateButtonsState();

                BulbPowerState = device.Properties["power"] != "on";

                logger.Information("Bulb.power = {BulbPowerState}", BulbPowerState);
                logger.Information("rgb = {rgb}", device.Properties["rgb"]);

                device.SetPower(BulbPowerState);
                string hexColor = int.Parse((string)device.Properties["rgb"]).ToString("X");

                Brightness_TextBox.Text = (string)device.Properties["bright"];
                Warm_TextBox.Text = (string)device.Properties["ct"];

                RGB_ColorPicker.SelectedColor =
                    (Color)System.Windows.Media.ColorConverter.ConvertFromString($"#{hexColor}");

                isConnected = true;
                Connect_Button.IsEnabled = false;
                Hostname_TextBox.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong! Check console or log.txt!");
                logger.Error(ex, "Failed to connect to {Hostname}!! Error - {ex}", hostname, ex);
            }
        }

        private void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            // главное не загадить эту кнопку до конца.
            Connect();
        }

        private void UpdateButtonsState()
        {
            logger.Debug("UpdateButtonsState method called");
            bool isDeviceExists = device != null;

            Switch_Button.IsEnabled = isDeviceExists;
            Brightness_TextBox.IsEnabled = isDeviceExists;
            Brightness_Button.IsEnabled = isDeviceExists;
            Warm_TextBox.IsEnabled = isDeviceExists;
            Warm_Button.IsEnabled = isDeviceExists;
            Sendrgb_Button.IsEnabled = isDeviceExists;
            RGB_ColorPicker.IsEnabled = isDeviceExists;
            SavePreset_Button.IsEnabled = isDeviceExists;
            Open_Presets_Menu_Button.IsEnabled = isDeviceExists;
        }

        private void Brightness_Button_Click(object sender, RoutedEventArgs e)
        {
            logger.Information("Brightness_Button_Click method called");
            if (int.TryParse(Brightness_TextBox.Text, out int brightness))
            {
                // диапазон яркости лампочки от 1 до 100, обработка случая если юзер ввел больше или меньше
                if (brightness > 100) brightness = 100;
                if (brightness < 1) brightness = 1;
                Brightness_TextBox.Text = $"{brightness}";
                device.SetBrightness(brightness);
                logger.Information("Brightness set to {Brightness}", brightness);
            }
            else
            {
                // TODO: сделать НОРМАЛЬНУЮ обработку ошибок
                Brightness_TextBox.Text = "100";
                device.SetBrightness(100);
                logger.Error("Invalid brightness value. User provided {Brightness}. Setting 100", brightness);
            }
        }

        private void Warm_Button_Click(object sender, RoutedEventArgs e)
        {

            logger.Information("Warm_Button_Click method called");

            if (int.TryParse(Warm_TextBox.Text, out int colorTemperature))
            {
                if (colorTemperature < 1700) colorTemperature = 1700;
                if (colorTemperature > 6500) colorTemperature = 6500;
                Warm_TextBox.Text = $"{colorTemperature}";
                device.SetColorTemperature(colorTemperature);
                logger.Information("Color temperature set to {ColorTemperature}", colorTemperature);
            }
            else
            {
                Warm_TextBox.Text = "6500";
                device.SetColorTemperature(6500);
                logger.Error("Error while parsing int. User provided {ColorTemperature}", colorTemperature);
            }
        }

        private void Sendrgb_Button_Click(object sender, RoutedEventArgs e)
        {

            logger.Information("Sendrgb_Button_Click method called");
            device.SetRGBColor(r, g, b);
            logger.Information("RGB color set to R={R}, G={G}, B={B}", r, g, b);
        }

        private void RGB_ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            logger.Debug("RGB_ColorPicker_SelectedColorChanged method called");

            r = e.NewValue.Value.R;
            g = e.NewValue.Value.G;
            b = e.NewValue.Value.B;

            // await device.SetRGBColor(r, g, b);
            logger.Debug("RGB values: R={R}, G={G}, B={B}", r, g, b);

        }

        private void SaveConfig(string hostname, Preset[] presets)
        {
            logger.Information("SaveConfig method called");
            logger.Information($"Length of Preset[] -- {presets.Length}");
            // Создаем объект конфигурации
            Config config = new Config
            {
                Hostname = hostname,
                Presets = presets
            };

            // Сериализуем объект в JSON и сохраняем в файл
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config));
        }

        private void Debug_Button_Click(object sender, RoutedEventArgs e)
        {
            logger.Information($"Current ColorMode - {byte.Parse($"{device.Properties["color_mode"]}")}");
        }

        private void SetPreset(Preset preset)
        {
            logger.Information("SetPreset method called;");
            Brightness_TextBox.Text = $"{preset.Brightness}";
            device.SetBrightness(preset.Brightness);

            if (preset.ColorMode == 1) 
            {
                string hexColor = preset.ColorRGB.ToString("X");
                (int r, int g, int b) = HexToRgb(hexColor);
                logger.Information($"HexColor -- {hexColor}; R - {r}, G - {g}, B - {b}");
                device.SetRGBColor(r, g, b);
                string hexColorr = preset.ColorRGB.ToString("X");
                RGB_ColorPicker.SelectedColor =
                    (Color)System.Windows.Media.ColorConverter.ConvertFromString($"#{hexColorr}");
                return; 
            }
            Warm_TextBox.Text = $"{preset.ColorTemperature}";
            device.SetColorTemperature(preset.ColorTemperature);
        }

        private void LoadConfig()
        {
            // Проверяем наличие файла конфигурации
            if (File.Exists(configFilePath))
            {
                // Читаем содержимое файла и десериализуем его в объект конфигурации
                string configJson = File.ReadAllText(configFilePath);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);

                // Устанавливаем hostname из конфигурации
                Hostname_TextBox.Text = config.Hostname;
                if (config.Presets == null)
                {
                    DoesINeedToCreateDefaultPreset = true;
                }
            }
        }

        private void Open_Presets_Menu_Button_Click(object sender, RoutedEventArgs e)
        {
            logger.Information("Open_Presets_Menu method called");
            logger.Information("Opening presets menu..");
            PresetsMenu presetsMenu = new PresetsMenu(logger, configFilePath);
            presetsMenu.PresetSelected += PresetsMenu_PresetSelected;
            presetsMenu.ChosenPresetToDelete += PresetsMenu_ChosenPresetToDelete;
            presetsMenu.Show();
        }

        private void PresetsMenu_PresetSelected(Preset selectedPreset)
        {
            logger.Information("PresetsMenu_PresetSelected method called");
            if (selectedPreset.Name == "__MINECRAFT__")
            {
                Preset LightSyncDefaultPreset = new Preset
                {
                    Name = "___LightSyncDefault",
                    Brightness = 100,
                    ColorMode = 2,
                    ColorTemperature = 4444,
                    ColorRGB = 123123123,
                };
                SyncWithMinecraftWindow syncWithMinecraftWindow = new SyncWithMinecraftWindow(logger, device, LightSyncDefaultPreset);
                LightSyncCurrentPreset = GetCurrentPreset();
                ChangeAllButtonsStates();
                syncWithMinecraftWindow.Closing += LightSyncOnWindowClosing;
                syncWithMinecraftWindow.Show();
                return;
            }
            SetPreset(selectedPreset);
            logger.Information("Successfuly set preset {selectedPresetName}", selectedPreset.Name);
        }

        private void PresetsMenu_ChosenPresetToDelete(List<Preset> presets)
        {
            SaveConfig(hostname, presets.ToArray());
            logger.Information("Saved config with deleted preset");
        }

        private void SavePreset_Button_Click(object sender, RoutedEventArgs e)
        {
            logger.Information("SavePreset_Menu method called");
            logger.Information("Opening saving preset menu");

            Preset currentPreset = GetCurrentPreset();

            SavePresetMenu savePresetMenu = new SavePresetMenu(logger, configFilePath, currentPreset);
            savePresetMenu.PresetSaved += SavePresetMenu_PresetSaved;
            savePresetMenu.Show();
        }

        private void SavePresetMenu_PresetSaved(List<Preset> _presets)
        {
            Preset[] presets = _presets.ToArray();
            SaveConfig(hostname, presets);
        }

        private Preset[] LoadPresetsFromConfig()
        {
            if (File.Exists(configFilePath))
            {
                string configJson = File.ReadAllText(configFilePath);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);

                return config.Presets;
            } else
            {
                Preset[] presets = new Preset[0];
                Array.Resize(ref presets, presets.Length + 1);
                presets[presets.Length - 1] = defaultPreset;
                return presets;
            }
        }
        static (int r, int g, int b) HexToRgb(string hex)
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            int intValue = Convert.ToInt32(hex, 16);
            int r = (intValue >> 16) & 255;
            int g = (intValue >> 8) & 255;
            int b = intValue & 255;

            return (r, g, b);
        }

        private string RgbToHex(byte r,byte g, byte b)
        {
            return $"{r:X2}{g:X2}{b:X2}";
        }

        private Preset GetCurrentPreset()
        {
            Preset currentPreset;
            if (byte.Parse($"{device.Properties["color_mode"]}") == 1)
            {
                currentPreset = new Preset
                {
                    Name = "_current",
                    Brightness = Byte.Parse(Brightness_TextBox.Text),
                    ColorMode = byte.Parse($"{device.Properties["color_mode"]}"),
                    ColorRGB = int.Parse(RgbToHex(r, g, b), System.Globalization.NumberStyles.HexNumber),
                    ColorTemperature = 4500
                };
            }
            else
            {
                currentPreset = new Preset
                {
                    Name = "_current",
                    Brightness = Byte.Parse(Brightness_TextBox.Text),
                    ColorMode = byte.Parse($"{device.Properties["color_mode"]}"),
                    ColorTemperature = int.Parse($"{device.Properties["ct"]}"),
                    ColorRGB = 9055202,
                };
            }

            return currentPreset;
        }

        private async void debug_fun()
        {
            IEnumerable<Device> discoveredDevices = await DeviceLocator.DiscoverAsync();
            logger.Information($"found {discoveredDevices.Count()} devices");
            if (discoveredDevices.Count() != 0)
            {
                debug_label.Content = discoveredDevices.ElementAt(0).ToString();
            } else
            {
                debug_label.Content = "No devices found";
            }
        }

        private void debug_btn_Click(object sender, RoutedEventArgs e)
        {
            debug_fun();
        }

        private void debug_label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show((string) debug_label.Content);
        }

        public void LightSyncOnWindowClosing(object sender, CancelEventArgs e)
        {
            ChangeAllButtonsStates();
            SetPreset(LightSyncCurrentPreset);
        }

        private void ChangeAllButtonsStates()
        {
            logger.Debug("ChangeAllButtonsStates method called");

            Switch_Button.IsEnabled = !Switch_Button.IsEnabled;
            Brightness_TextBox.IsEnabled = !Brightness_TextBox.IsEnabled;
            Brightness_Button.IsEnabled = !Brightness_Button.IsEnabled;
            Warm_TextBox.IsEnabled = !Warm_TextBox.IsEnabled;
            Warm_Button.IsEnabled = !Warm_Button.IsEnabled;
            Sendrgb_Button.IsEnabled = !Sendrgb_Button.IsEnabled;
            RGB_ColorPicker.IsEnabled = !RGB_ColorPicker.IsEnabled;
            SavePreset_Button.IsEnabled = !SavePreset_Button.IsEnabled;
            Open_Presets_Menu_Button.IsEnabled = !Open_Presets_Menu_Button.IsEnabled;
        }

    }
    // Класс для хранения конфигурации
    public class Config
    {
        public string Hostname { get; set; }
        public Preset[] Presets { get; set; }
    }

    public class Preset 
    {
        public string Name { get; set; }
        public byte Brightness { get; set; }
        public int ColorMode { get; set; }
        public int ColorTemperature { get; set; } // если ColorMode = 2
        public int ColorRGB { get; set; } // если ColorMode = 1
        
    }
}