namespace ExamplePlugin.Dependency
{
    public class MySharedInstance
    {
        public static MySharedInstance Instance { get; } = new MySharedInstance();

        public bool FlagA { get; set; }
        public bool FlagB { get; set; }
        public bool FlagC { get; set; }
    }
}