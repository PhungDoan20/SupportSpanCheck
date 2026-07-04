using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(SupportSpanCheck.App))]
[assembly: CommandClass(typeof(SupportSpanCheck.Commands))]

namespace SupportSpanCheck
{
    public class App : IExtensionApplication
    {
        public void Initialize()
        {
            // Initialization code if needed
        }

        public void Terminate()
        {
            // Cleanup code if needed
        }
    }
}
