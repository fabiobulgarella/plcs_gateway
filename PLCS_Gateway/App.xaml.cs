using System;
using System.Windows;

namespace PLCS_Gateway
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string AppName = "PLCS_Gateway";
        private static System.Windows.Forms.NotifyIcon notifyIcon;

        private MqttServer mqttServer;
        private MqttClient mqttClient;
        private Bridge bridge;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Avvio i servizi MQTT
            StartAll();

            // Inizializzo la NotifyIcon
            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = PLCS_Gateway.Properties.Resources.AppIcon,
                Visible = true
            };

            // Creo il menu contestuale della tray icon
            CreateContextMenu();
        }

        internal void StartAll()
        {
            try
            {
                // Avvio il tutto
                Console.WriteLine("Avvio il tutto...");
                mqttServer = new MqttServer();
                mqttClient = new MqttClient();
                bridge = new Bridge(mqttClient.GetClient(), mqttServer.GetServer());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected error occurred: {0}", e.Message);
                throw e;
            }
        }

        private void ExitApplication()
        {
            notifyIcon.Dispose();
            bridge.Stop();
            mqttServer.Stop();
            mqttClient.Stop();
            notifyIcon = null;
            Current.Shutdown();
        }

        private void CreateContextMenu()
        {
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Esci da " + AppName).Click += (s, e) => ExitApplication();
        }

        public static void ShowBallon(String message, EventHandler callback, System.Windows.Forms.ToolTipIcon icon)
        {
            notifyIcon.BalloonTipClicked += callback;
            notifyIcon.ShowBalloonTip(1200, AppName, message, icon);
        }
    }
}
