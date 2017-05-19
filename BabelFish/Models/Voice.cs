using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelFish.Models
{
    public class Voice
    {
        public string Code { get; }

        public string Name { get; }

        public Voice(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public override string ToString() => Name;
    }
}
