using UnityEngine;

public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

public static class HexDirectionExtensions
{
    /*
    // instance method처럼 호출가능
    HexDirection dir = HexDirection.NE;
    HexDirection opposite = dir.Opposite();
    */
    public static HexDirection Opposite(this HexDirection direction) {
        //Since a hexagon has 6 directions, the opposite direction is always 3 spaces away.
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
        //return (HexDirection)(((int)direction + 3) % 6);
    }

    public static HexDirection Previous(this HexDirection direction) {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    public static HexDirection Next(this HexDirection direction) {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }
}