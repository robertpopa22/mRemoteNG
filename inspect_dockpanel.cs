
using System;
using System.Linq;
using System.Reflection;
using WeifenLuo.WinFormsUI.Docking;

public class Inspector {
    public static void Main() {
        var dp = new DockPanel();
        Console.WriteLine("DockPanel properties:");
        foreach (var p in typeof(DockPanel).GetProperties()) {
            Console.WriteLine(p.Name);
        }

        Console.WriteLine("
DockPanel events:");
        foreach (var e in typeof(DockPanel).GetEvents()) {
            Console.WriteLine(e.Name + " : " + e.EventHandlerType);
        }
    }
}
