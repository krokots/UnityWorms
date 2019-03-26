using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseData  {

    public float[] Data;
    public float Min;
    public float Max;
    public int Layer;
    public bool Solid;

    public NoiseData(int width, int layer, bool solid)
    {
        Data = new float[width];
        Min = float.MaxValue;
        Max = float.MinValue;
        Layer = layer;
        Solid = solid;
    }

    public override string ToString()
    {
        return "Noise layer " + Layer + ", len " + Data.Length + ", solid " + Solid + ", min " + Min + ", max " + Max;
    }
}
