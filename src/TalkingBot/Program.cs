using System.Configuration;
using TalkingBot.Helpers;

namespace TalkingBot
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            AppConstants.OpenAIKey = ConfigurationManager.AppSettings["OpenAIKey"];
            AppConstants.ModelId = ConfigurationManager.AppSettings["ModelId"];
            AppConstants.OpenAIOrg = ConfigurationManager.AppSettings["OpenAIOrg"];
            if (string.IsNullOrEmpty(AppConstants.OpenAIKey)) throw new Exception("Open AI Key is required");
            Application.Run(new Form1());
        }
    }
}