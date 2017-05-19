using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelFish.Models
{
    public class ChatMessage
    {
        public string SourceText { get; set; }

        public string TranslatedText { get; set; }

        public string Language { get; set; }
    }
}
