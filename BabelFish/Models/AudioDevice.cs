using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelFish.Models
{
    public class AudioDevice
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public AudioDevice(string id, string name)
        {
            Name = name;
            Id = id;
        }

        public override string ToString() => Name;
    }
}
