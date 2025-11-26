using System;

namespace TestCode;

public class Program() {

    public static ulong TestField = 100;

    [Export]
    public static void Main()
    {
        Console.WriteLine(TestField);
        TestField = 5 + 10;
        TestField *= 10;
        Console.WriteLine(TestField);
    }
}

class Test() { }
class Driving() {
    bool _rightAfterABeer;
    
    public void InYourCar(bool rightAfterABeer) => this._rightAfterABeer = rightAfterABeer;
    public void InMyCar(bool rightAfterABeer) => _rightAfterABeer = rightAfterABeer;
}
