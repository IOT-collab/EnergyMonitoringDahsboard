using IEC.Shared.Models;
using IEC.Shared.Services;
using System.Collections.Generic;
using System.Linq;

namespace IECGUI.Services;

public sealed class ScadaLayoutService
{
    private readonly ConfigurationManagerService _configuration;

    public ScadaLayoutService(ConfigurationManagerService configuration)
    {
        _configuration = configuration;
    }

    public List<ScadaPageConfig> LoadPages()
    {
        var pages = _configuration.Configuration.ScadaPages ?? new List<ScadaPageConfig>();
        foreach (var page in pages)
        {
            page.Widgets ??= new List<ScadaWidgetConfig>();
            page.PageName = string.IsNullOrWhiteSpace(page.PageName) ? "Main Plant" : page.PageName;
        }

        if (pages.Count == 0)
            pages.Add(new ScadaPageConfig());

        return pages;
    }

    public bool SavePages(IEnumerable<ScadaPageConfig> pages)
    {
        _configuration.Configuration.ScadaPages = pages?.ToList() ?? new List<ScadaPageConfig>();
        return _configuration.Save();
    }
}
