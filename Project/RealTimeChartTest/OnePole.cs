using System;

public class OnePole
{
    private double _a;
    private double _b;
    private double _z;

    public OnePole()
    {
        _a = 1.0;
        _b = 0.0;
        _z = 0.0;
    }

    public OnePole(float Fc)
    {
        _z = 0.0;
        SetFc(Fc);
    }

    //Low pass
    public void SetFc(float fc)
    {
        _b = Math.Exp(-2.0 * Math.PI * fc);
        _a = 1.0 - _b;
    }

    ////High pass
    //public void SetFc(float fc)
    //{
    //    _b1 = -Math.Exp(-2.0 * Math.PI * (0.5 - fc));
    //    _a0 = 1.0 - _b1;
    //}

    //public float Process(float input)
    //{
    //    return (float) (input * _a0 + _z1 * _b1);
    //}


    public float Process(float input)
    {
        return (float)(input * _b + _z * _a);
    }
}


