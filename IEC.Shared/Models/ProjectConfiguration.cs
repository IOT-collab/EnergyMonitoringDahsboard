using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.Models
{
    public class ProjectConfiguration
    {
        public string ProjectName { get; set; } = "New Project";

        public List<MetersConfig> Meters { get; set; }
            = new List<MetersConfig>();

        public List<SldBreakerConfig> SldBreakers { get; set; }
            = new List<SldBreakerConfig>();

        public List<AlarmRuleConfig> AlarmRules { get; set; }
            = new List<AlarmRuleConfig>();

        // New: persisted user settings
        public UserSettings UserSettings { get; set; } = new UserSettings();

        // User-designed SCADA mimic pages. Kept in the same project JSON so layouts travel with the project.
        public List<ScadaPageConfig> ScadaPages { get; set; } = new List<ScadaPageConfig>();

    }
}

