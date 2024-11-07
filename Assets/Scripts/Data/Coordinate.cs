// Assets/Scripts/Data/Coordinate.cs

using System;

[Serializable]
public struct Coordinate
{
    public double Latitude;
    public double Longitude;

    public Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
