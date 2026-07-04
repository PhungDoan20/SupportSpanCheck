using System;
using System.Reflection;
using System.Linq;

class Program {
    static void Main() {
        try {
            var asm = Assembly.LoadFrom(@"E:\AI\AutoCAD Plant 3D 2027 SDK\inc-x64\PnP3dObjectsMgd.dll");
            var portType = asm.GetType("Autodesk.ProcessPower.PnP3dObjects.Port") ?? asm.GetType("Autodesk.ProcessPower.Parts.Port") ?? asm.GetExportedTypes().FirstOrDefault(t => t.Name == "Port");
            if (portType != null) {
                Console.WriteLine("Port properties:");
                foreach(var p in portType.GetProperties()) Console.WriteLine(" - " + p.Name + " : " + p.PropertyType.Name);
            }
            
            var partType = asm.GetExportedTypes().FirstOrDefault(t => t.Name == "Part");
            if (partType != null) {
                Console.WriteLine("Part properties:");
                foreach(var p in partType.GetProperties().Where(pr => pr.Name.Contains("Pos") || pr.Name.Contains("Location"))) Console.WriteLine(" - " + p.Name + " : " + p.PropertyType.Name);
            }
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }
}
