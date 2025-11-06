namespace TestCode;

public class Program() {

    public static ulong TestField = 100;

    public static void Main()
    {
        Driving driving = new();
        driving.InMyCar(rightAfterABeer: true);

        //Console.WriteLine(TestField);
    }
}

class Test() { }
class Driving() {
    public void InYourCar(bool rightAfterABeer) => this.rightAfterABeer = rightAfterABeer;
    public void InMyCar(bool rightAfterABeer) => this.rightAfterABeer = rightAfterABeer;
    bool rightAfterABeer;
}
