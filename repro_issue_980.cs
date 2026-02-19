using System;
using mRemoteNG.Connection;
using mRemoteNG.Container;

public class Repro
{
    public static void Main()
    {
        // Setup
        var root = new ContainerInfo { Name = "Root", Username = "RootUser" };
        var child = new ConnectionInfo { Name = "Child" };
        child.SetParent(root);

        // Verify Inheritance is ON by default
        child.Inheritance.Username = true;
        
        Console.WriteLine($"Parent Username: {root.Username}");
        Console.WriteLine($"Child Username (Inherited): {child.Username}");

        if (child.Username != "RootUser")
        {
            Console.WriteLine("FAIL: Child did not inherit username.");
            return;
        }

        // Action: Set Username on Child
        Console.WriteLine("Setting Child Username to 'ChildUser'...");
        child.Username = "ChildUser";

        // Check result
        Console.WriteLine($"Child Username after set: {child.Username}");
        Console.WriteLine($"Child Inheritance.Username: {child.Inheritance.Username}");

        if (child.Username == "RootUser")
        {
            Console.WriteLine("CONFIRMED: Setting property did NOT disable inheritance, value remains inherited.");
        }
        else if (child.Username == "ChildUser")
        {
            Console.WriteLine("Checking if inheritance was disabled...");
            if (child.Inheritance.Username == false)
            {
                Console.WriteLine("Inheritance was automatically disabled. This works as expected.");
            }
            else
            {
                Console.WriteLine("Inheritance is STILL TRUE, but value is ChildUser? This implies GetPropertyValue logic is different.");
            }
        }
    }
}
