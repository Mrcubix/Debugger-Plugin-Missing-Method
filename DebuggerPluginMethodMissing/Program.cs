using DebuggerPluginMethodMissing;
using DebuggerPluginMethodMissing.Plugin;

var pluginManager = new PluginManager();
pluginManager.Load();

var toolTypes = pluginManager.GetChildTypes<ITool>();
var tools = new List<ITool>();

foreach (var type in toolTypes)
    if (type.FullName != null)
        tools.Add(pluginManager.ConstructObject<ITool>(type.FullName));

tools.ForEach(t => t.Initialize());