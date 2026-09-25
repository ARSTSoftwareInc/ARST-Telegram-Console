using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net.NetworkInformation;
using Telegram.Bot.Polling;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace atcmdHost
{
    public partial class Form1 : Form
    {
        string token = "";

        public Form1()
        {
            InitializeComponent();
            MainSystem.sysRoot = Application.StartupPath;

            networkCheckTimer.Start();

            preinit();
        }

        void preinit()
        {
            try
            {
                if (MainSystem.isEthernetConnectionAvailable)
                {
                    var botClient = new TelegramBotClient(token);
                    var updateHundler = new UpdateHandler();
                    botClient.StartReceiving(updateHundler);

                    MainSystem.init(token);
                    this.Opacity = 100;
                }
            }
            catch (Exception ex)
            {
                MainSystem.throwErr(ex.Message, "INIT_TELEGRAM_CONNECTION_ERROR!", true);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            MainSystem.controlOnlineUsers(true, MainSystem.TargetUserID);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MainSystem.sendToUsrChatMessage(textBox1.Text);
        }

        private void networkCheckTimer_Tick(object sender, EventArgs e)
        {
            MainSystem.isEthernetConnectionAvailable = MainSystem.isNetworkAvailable();

            if(!MainSystem.isEthernetConnectionAvailable)
            {
                this.Opacity = 100;
                this.Size = new Size(444, 69);
                label1.Text = "NO CRITIC: Íåò ñîåäèíåíèÿ!";
            }
            else
            {
                this.Size = new Size(444, 168);
                label1.Text = "Èíèöèàëèçàöèÿ õîñòà ARSTTelegramConsole çàâåðøåíà!";
                
                if(!MainSystem.initialized) preinit();
            }
        }
    }
}
