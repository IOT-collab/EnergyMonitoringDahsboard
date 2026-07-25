using System.Collections.Generic;

namespace IEC.Shared.Models
{
    public class ReportFormatConfig
    {
        public string Name { get; set; }
        public List<string> SelectedColumns { get; set; } = new();
    }
}
