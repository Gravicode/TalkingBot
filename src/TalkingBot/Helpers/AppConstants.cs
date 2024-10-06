using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalkingBot.Helpers
{
    public class AppConstants
    {
        public static string OpenAIEndpoint { set; get; } = "https://api.openai.com/v1";
        public static string OpenAIKey { set; get; }
        public static string ModelId { set; get; }
        public static string OpenAIOrg { get; set; }
    }
}
