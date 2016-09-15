using System;

public class OnePole
{
    private double a0;
    private double b1;
    private double z1;

    public OnePole()
    {
        a0 = 1.0;
        b1 = 0.0;
        z1 = 0.0;
    }

    public OnePole(double Fc)
    {
        z1 = 0.0;
        setFc(Fc);
    }

    public void setFc(double Fc)
    {
        b1 = exp(-2.0 * M_PI * Fc);
        a0 = 1.0 - b1;
    }

    public float Process(float input)
    {
        return z1 = input * a0 + z1 * b1;
    }
}


