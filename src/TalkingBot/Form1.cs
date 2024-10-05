using TalkingBot.Helpers;

namespace TalkingBot
{
    public partial class Form1 : Form
    {
        public RealtimeVoiceBot bot { get; set; }
        public Form1()
        {
            InitializeComponent();
            bot = new();
            bot.LogMessageReceived += (object? a,LogMessage b) => {
                this.Invoke((MethodInvoker)delegate
                {
                    LogTxt.AppendText(b.Message);
                });
            };
            BtnStart.Click += async(a, b) => { await bot.Start(); };
            BtnStop.Click += async(a, b) => { await bot.Stop(); };
            BtnClear.Click += (a, b) => { LogTxt.Clear(); };
        }
    }
}
