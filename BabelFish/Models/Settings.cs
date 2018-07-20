using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelFish.Models
{
    public class Settings
    {
        public string SpeechSubscriptionKey { get; set; }

        public string Source { get; set; }

        public string Translation { get; set; }

        public string Voice { get; set; }
    }
}
