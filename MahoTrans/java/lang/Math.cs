namespace java.lang;

public class Math : Object
{
    public static double abs(double a) => global::System.Math.Abs(a);
    public static float abs(float a) => global::System.Math.Abs(a);
    public static int abs(int a) => global::System.Math.Abs(a);
    public static long abs(long a) => global::System.Math.Abs(a);
    


    public static double sin(double a) => global::System.Math.Sin(a);
    public static double cos(double a) => global::System.Math.Cos(a);
    public static double tan(double a) => global::System.Math.Tan(a);
    public static double toRadians(double a) => a * 180d / global::System.Math.PI;
    public static double toDegrees(double a) => a * global::System.Math.PI / 180d;
    public static double sqrt(double a) => global::System.Math.Sqrt(a);
    public static int min(int a, int b) => global::System.Math.Min(a, b);

    public static int max(int a, int b) => global::System.Math.Max(a, b);
    
    public static long min(long a, long b) => global::System.Math.Min(a, b);

    public static long max(long a, long b) => global::System.Math.Max(a, b);
}