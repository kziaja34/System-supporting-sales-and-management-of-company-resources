namespace SSSMCR.ApiService.Services;

public class FuzzyPriorityEvaluatorService
{
    private double Trapezoidal(double x, double a, double b, double c, double d)
    {
        if (x <= a || x >= d) return 0;
        else if (x >= b && x <= c) return 1.0;
        else if (x > a && x < b) return (x - a) / (b - a);
        else return (d - x) / (d - c);
    }

    public (double Low, double Medium, double High) Evaluate(double priority)
    {
        var low = (a: 0, b: 0, c: 30, d: 50);
        var medium = (a: 40, b: 55, c: 70, d: 85);
        var high = (a: 70, b: 85, c: 100, d: 100);
        
        var uLow = Trapezoidal(priority, low.a, low.b, low.c, low.d);
        var uMedium = Trapezoidal(priority, medium.a, medium.b, medium.c, medium.d);
        var uHigh = Trapezoidal(priority, high.a, high.b, high.c, high.d);
        
        return (uLow, uMedium, uHigh);
    }
}