// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace java.lang;

public class Math : Object
{
    public static double abs(double a) => global::System.Math.Abs(a);
    public static float abs(float a) => global::System.Math.Abs(a);
    public static int abs(int a) => global::System.Math.Abs(a);
    public static long abs(long a) => global::System.Math.Abs(a);
    public static double ceil(double v) => global::System.Math.Ceiling(v);
    public static double cos(double a) => global::System.Math.Cos(a);
    public static double floor(double v) => global::System.Math.Floor(v);
    public static double max(double a, double b) => global::System.Math.Max(a, b);
    public static float max(float a, float b) => global::System.Math.Max(a, b);
    public static int max(int a, int b) => global::System.Math.Max(a, b);
    public static long max(long a, long b) => global::System.Math.Max(a, b);
    public static double min(double a, double b) => global::System.Math.Min(a, b);
    public static float min(float a, float b) => global::System.Math.Min(a, b);
    public static int min(int a, int b) => global::System.Math.Min(a, b);
    public static long min(long a, long b) => global::System.Math.Min(a, b);
    public static double sin(double a) => global::System.Math.Sin(a);
    public static double sqrt(double a) => global::System.Math.Sqrt(a);
    public static double tan(double a) => global::System.Math.Tan(a);
    public static double toDegrees(double a) => a * global::System.Math.PI / 180d;
    public static double toRadians(double a) => a * 180d / global::System.Math.PI;
}