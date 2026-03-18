public enum NavigationType { Arrow, Gate, Line }
public enum Route { Route1, Route2, Route3 }

[System.Serializable]
public class NavigationStep
{
    public Route route;
    public NavigationType navigationType;
}
