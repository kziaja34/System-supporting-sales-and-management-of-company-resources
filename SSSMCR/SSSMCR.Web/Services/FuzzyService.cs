using System.Globalization;
using MudBlazor;
using MudBlazor.Utilities;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class FuzzyService
{
    private const string ULowColor = "#388E3C";
    private const string UMediumColor = "#F57C00";
    private const string UHighColor = "#D32F2F";
    public string GetFuzzyGradient(dynamic ctx)
    {
        var entries = new List<(string color, double w)>
        {
            (ULowColor,    (double)ctx.ULow),
            (UMediumColor, (double)ctx.UMedium),
            (UHighColor,   (double)ctx.UHigh)
        };

        const double EPS = 1e-9;
        var nonZero = entries.Where(e => e.w > EPS).ToList();
        
        if (nonZero.Count == 0)
            return "background: transparent; color: inherit;";
        
        if (nonZero.Count == 1)
            return $"background: {nonZero[0].color}; color: white;";
        
        if (nonZero.Count > 2)
            nonZero = nonZero.OrderByDescending(e => e.w).Take(2).ToList();

        var (color1, w1) = nonZero[0];
        var (color2, w2) = nonZero[1];

        var sum = w1 + w2;
        if (sum <= EPS) sum = 1;
        w1 /= sum; w2 /= sum;
        
        var split = w1 * 100.0;
        
        const double BLEND_WIDTH = 30.0;
        var half = BLEND_WIDTH / 2.0;
        
        double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);

        var startBlend = Clamp(split - half, 0, 100);
        var endBlend   = Clamp(split + half, 0, 100);

        var sb = new System.Text.StringBuilder();
        sb.Append("background: linear-gradient(90deg, ");
        sb.Append($"{color1} 0%, ");
        sb.Append($"{color1} {startBlend.ToString("F2", CultureInfo.InvariantCulture)}%, ");
        sb.Append($"{color2} {endBlend.ToString("F2", CultureInfo.InvariantCulture)}%, ");
        sb.Append($"{color2} 100%); color: white;");

        return sb.ToString();
    }

    public Color GetColor(string val) => val.ToLower() switch
    {
        "low" => Color.Success,
        "medium" => Color.Warning,
        "high" => Color.Error,
        _ => Color.Default
    };
}