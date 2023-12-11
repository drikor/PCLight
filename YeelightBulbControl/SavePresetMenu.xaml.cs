using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace YeelightBulbControl
{
    public partial class SavePresetMenu : Window
    {
        private string configFilePath;
        private ILogger logger;

        public delegate void PresetSavedEventHandler(List<Preset> presets);
        public event PresetSavedEventHandler PresetSaved;

        public SavePresetMenu(ILogger logger, string configFilePath, Preset currentPreset)
        {
            this.logger = logger;
            this.configFilePath = configFilePath;

            InitializeComponent();

            Brightness_TextBox.Text = $"{currentPreset.Brightness}";

            if (currentPreset.ColorMode == 2) 
            {
                ColorTemperature_TextBox.Text = $"{currentPreset.ColorTemperature}";
            }

            ColorTemperature_TextBox.IsEnabled = currentPreset.ColorMode == 2;

            string hexColor = currentPreset.ColorRGB.ToString("X");
            RGB_ColorPicker.IsEnabled = currentPreset.ColorMode == 1;
            RGB_ColorPicker.SelectedColor =
                    (Color)System.Windows.Media.ColorConverter.ConvertFromString($"#{hexColor}");

            ColorMode_CheckBox.IsChecked = currentPreset.ColorMode == 1;

            logger.Information("[SPM] SavePrestMenu iniitialized");
        }

        private Preset[] LoadPresetsFromConfig()
        {
            if (File.Exists(configFilePath))
            {
                string configJson = File.ReadAllText(configFilePath);
                Config config = JsonConvert.DeserializeObject<Config>(configJson);

                logger.Information($"Length of presets in config {config.Presets.Length}");

                return config.Presets;
            }
            else
            {
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

        private List<Preset> SavePreset(Preset preset, Preset[] _presets)
        {
            logger.Information("SavePreset method called");
            List<Preset> presets = _presets.ToList();
            presets.Add(preset);
            return presets;
        }

        private void ColorMode_CheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            RGB_ColorPicker.IsEnabled = (bool)ColorMode_CheckBox.IsChecked;
            ColorTemperature_TextBox.IsEnabled = (bool)!ColorMode_CheckBox.IsChecked;
        }

        private void SavePreset_Button_Click(object sender, RoutedEventArgs e)
        {
            int ct;

            try 
            {
                ct = int.Parse(ColorTemperature_TextBox.Text);
            } 
            catch (Exception exc)
            {
                logger.Warning($"{exc}");
                ct = 4500;
            }

            Preset presetToSave = new Preset
            {
                Name = PresetName_TextBox.Text,
                Brightness = byte.Parse(Brightness_TextBox.Text),
                ColorMode = !(bool)ColorMode_CheckBox.IsChecked ? 2 : 1,
                ColorTemperature = ct,
                ColorRGB = int.Parse(RGB_ColorPicker.SelectedColor.ToString().Substring(3), System.Globalization.NumberStyles.HexNumber),
            };

            PresetSaved?.Invoke(SavePreset(presetToSave, LoadPresetsFromConfig()));
            Close();
        }
    }
}
