using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GRandom : MonoBehaviour {

    public static System.Random Rnd;

    public static bool GetBool()
	{
		return (Rnd.Next(0, 2) == 0) ? true : false;
	}

    public static double GetDouble(double min, double max)
    {
        return Rnd.NextDouble() * (max - min) + min;
    }

    public static float GetFloat(float min, float max)
    {
        return (float)GetDouble(min, max);
    }

    public static int GetInt(int min, int max)
    {
        return Rnd.Next(min, max + 1);
    }

    public static int GetIntRaw(int min, int max)
    {
        return Rnd.Next(min, max);
    }

    public static int GetSeed()
    {
        //System.Random seedGen = new System.Random();
        return (int)DateTime.Now.Ticks;
    }
}
