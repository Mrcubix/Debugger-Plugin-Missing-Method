using DebuggerPluginMethodMissing.Plugin;
using ExamplePlugin.Dependency;

namespace ExamplePlugin
{
    public class ExampleToolOld : ITool
    {
        public MySharedInstance SharedInstance { get; } = MySharedInstance.Instance;

        public bool Initialize()
        {
            Console.WriteLine("Hello from ExampleToolOld");
            return true;
        }

        public void Dispose()
        {
            
        }
    }
}