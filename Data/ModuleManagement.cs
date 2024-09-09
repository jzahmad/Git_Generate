using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReadMeGenie.Data
{
    public class ModuleManagement(string language, string packageManager, string directory)
    {
        public string Language { get; set; } = language;
        public string PackageManager { get; set; } = packageManager;
        public string Directory { get; set; } = directory;
    }
}