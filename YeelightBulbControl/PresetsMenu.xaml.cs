using YeelightAPI;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Serilog;
using Newtonsoft.Json;

namespace YeelightBulbControl
{
    /// <summary>
    /// Interaction logic for PresetsMenu.xaml
    /// </summary>
    public partial class PresetsMenu : Window
    {
        private string configFilePath;
        private ILogger logger;

        public delegate void PresetSelectedEventHandler(Preset selectedPreset);
        public event PresetSelectedEventHandler PresetSelected;

        public delegate void ChosenPresetToDeleteEventHandler(List<Preset> presets);
        public event ChosenPresetToDeleteEventHandler ChosenPresetToDelete;

        public PresetsMenu(ILogger logger, string configFilePath)
        {
            this.logger = logger;
            this.configFilePath = configFilePath;

            InitializeComponent();

            Preset[] presets = LoadPresetsFromConfig();
            PresetsListBox.ItemsSource = presets;

            logger.Information("[PM] Presets menu initialized");
        }

        private Preset[] LoadPresetsFromConfig()
        {
            if (File.Exists(configFilePath))
            {
                string configJson = File.ReadAllText(configFilePath);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);

                return config.Presets;
            }
            else
            {
                logger.Warning("Config file doesnt exist!!");
                Preset defaultPreset = new Preset
                {
                    Name = "defaultPreset",
                    Brightness = 100,
                    ColorMode = 2,
                    ColorRGB = 1,
                    ColorTemperature = 4500
                };

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

        private void PresetsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Preset selectedPreset = (Preset)PresetsListBox.SelectedItem;

            if (selectedPreset != null)
            {
                logger.Information($"Selected preset: {selectedPreset.Name}");
                PresetSelected?.Invoke(selectedPreset);
            }

            Close();
        }

        private void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            Preset selectedPreset = (Preset)PresetsListBox.SelectedItem;

            if (selectedPreset == null)
            {
                MessageBox.Show("Siily, you should choose preset first (just click on it");
                return;
            }
            List<Preset> presets = LoadPresetsFromConfig().ToList();
            Preset presetToRemove = presets.Single(p => p.Name == selectedPreset.Name);
            if (presetToRemove == null)
            {
                MessageBox.Show("Something went wrong");
                logger.Error($"Preset TO REMOVE IS NULL");
                return;
            }
            presets.Remove(presetToRemove);
            PresetsListBox.ItemsSource = presets;
            ChosenPresetToDelete?.Invoke(presets);
        }
    }

}
