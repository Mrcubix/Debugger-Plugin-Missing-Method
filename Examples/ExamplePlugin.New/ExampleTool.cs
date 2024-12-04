using DebuggerPluginMethodMissing.Plugin;
using ExamplePlugin.Dependency;

namespace ExamplePlugin
{
    public class ExampleToolUpdated : ITool
    {
        public MySharedInstance SharedInstance { get; } = MySharedInstance.Instance;

        public bool Initialize()
        {
            Console.WriteLine("Hello from ExampleTool (New)");
            var test = SharedInstance.FlagC;
            return true;
        }

        public void Dispose()
        {
            
        }
    }
}