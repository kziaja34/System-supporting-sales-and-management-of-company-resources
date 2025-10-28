using MudBlazor;
using MudBlazor.Utilities;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class FuzzyService
{
    private string ULowColor = "#4caf50";
    private string UMediumColor = "#ffb300";
    private string UHighColor = "#f44336";
    public string GetFuzzyGradient(dynamic ctx)
    {
        var entries = new List<(string color, double w)>
        {
            (ULowColor,  (double)ctx.ULow),
            (UMediumColor,  (double)ctx.UMedium),
            (UHighColor,  (double)ctx.UHigh)
        };

        const double EPS = 1e-9;
        
        var nonZero = entries.Where(e => e.w > EPS).ToList();

        if (nonZero.Count == 0)
            return "background: transparent; color: inherit;";
        
        var sum = nonZero.Sum(e => e.w);
        if (sum <= EPS) sum = 1.0;
        var norm = nonZero.Select(e => (e.color, w: e.w / sum)).ToList();
        
        if (norm.Count == 1)
            return $"background: {norm[0].color}; color: white;";
        
        var stops = new List<string>();
        double acc = 0.0;
        for (int i = 0; i < norm.Count; i++)
        {
            var (color, w) = norm[i];
            if (i == 0) stops.Add($"{color} 0%");
            acc += w;
            stops.Add($"{color} {acc * 100:F0}%");
        }

        return $"background: linear-gradient(90deg, {string.Join(", ", stops)}); color: white;";
    }

    public Color GetColor(string val) => val.ToLower() switch
    {
        "low" => Color.Success,
        "medium" => Color.Warning,
        "high" => Color.Error,
        _ => Color.Default
    };
}