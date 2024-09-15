using YeelightAPI;
using System.Windows;
using Serilog;
using System.ComponentModel;
using CoreRCON;
using CoreRCON.Parsers.Standard;
using System.Net;
using System.Reflection.Emit;

namespace YeelightBulbControl
{
    /// <summary>
    /// Interaction logic for SyncWithMinecraftWindow.xaml
    /// </summary>
    /// 

    // ЭТО ДОБАВЛЕНО ЧИСТО КАК ПАСХАЛКА
    // Автор этой говнопроги сделал это ПО-ПРИКОЛУ
    // и тут МНОГО говнокода
    // (как и везде в этом проекте)
    public partial class SyncWithMinecraftWindow : Window
    {
        ILogger logger;
        Device device;
        bool needToStop;
        Preset DefaultPreset;

        public SyncWithMinecraftWindow(ILogger logger, Device device, Preset LightSyncDefaultPreset)
        {
            logger.Information("Opened SyncWithMinecraftWindow");
            this.logger = logger;
            this.device = device;
            this.DefaultPreset = LightSyncDefaultPreset;
            
            SetPreset(LightSyncDefaultPreset);

            InitializeComponent();
        }

        private void SetPreset(Preset preset)
        {
            logger.Information("[LIGHTSYNC] SetPreset method called;");
            device.TurnOn();
            device.SetBrightness(preset.Brightness);
            device.SetColorTemperature(preset.ColorTemperature);
        }

        private async void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            Start_Button.IsEnabled = false;
            string nickname = PlayerNick_TextBox.Text;
            try
            {
                RCON rcon = new RCON(
                    IPAddress.Parse(RconIP_TextBox.Text),
                    ushort.Parse(RconPORT_TextBox.Text), 
                    RconPASS_TextBox.Text
                );

                await rcon.ConnectAsync();

                string response = await rcon.SendCommandAsync($"lightsync {nickname}");
                if (response.StartsWith("E"))
                {
                    MessageBox.Show($"{response}; Make sure username is right");
                    Start_Button.IsEnabled = false;
                    return;
                }
                logger.Information(response);
                logger.Information($"Connected to: {RconIP_TextBox.Text}");
                Stop_Button.IsEnabled = true;

                Task task = Task.Run(async () =>
                {
                    logger.Information("Started task");
                    needToStop = false;
                    while (!needToStop)
                    {
                        string response = await rcon.SendCommandAsync($"lightsync {nickname}");
                        logger.Information($"{response}");
                        if (response.StartsWith("E")) 
                        {
                            needToStop = true;
                          //  Status_Label.Content = $"{response}; Stopped";
                        };

                        int res = int.Parse(response);
                        int bra;
                        if (res < 5)
                        {
                            device.TurnOff();
                            bra = 1;
                        }
                        else
                        {
                            device.TurnOn();
                            switch (int.Parse(response))
                            {
                                case 5:
                                    bra = 10;
                                    break;
                                case 6:
                                    bra = 15;
                                    break;
                                case 7:
                                    bra = 20;
                                    break;
                                case 8:
                                    bra = 25;
                                    break;
                                case 9:
                                    bra = 30;
                                    break;
                                case 10:
                                    bra = 35;
                                    break;
                                case 11:
                                    bra = 40;
                                    break;
                                case 12:
                                    bra = 50;
                                    break;
                                case 13:
                                    bra = 60;
                                    break;
                                case 14:
                                    bra = 70;
                                    break;
                                case 15:
                                    bra = 100;
                                    break;
                                // Add a default case if needed
                                default:
                                    bra = 50;
                                    logger.Warning($"wtf response is {response}");
                                    break;
                            }
                            device.SetBrightness(bra);
                        }
                        // Status_Label.Content = $"Running;\nCurrent Brightness (minecraft) -- {response}";
                        await Task.Delay(1700);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error;\n\n{ex}\n\nMake sure server is running");
                logger.Error($"{ex}");
                needToStop = true;
            }
            Start_Button.IsEnabled = false;
            Stop_Button.IsEnabled = false;
        }

        private void StopLightsyncCycle()
        {
            needToStop = true;
            SetPreset(DefaultPreset);
        }
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            StopLightsyncCycle();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            StopLightsyncCycle();
        }
    }
}
