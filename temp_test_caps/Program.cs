using System;

class Program {
    static void Main() {
        try {
            UriBuilder u = new UriBuilder();
            u.Scheme = "dummyscheme";
            u.Host = "SSH://hostname";
            Console.WriteLine($"Host '{u.Host}' set successfully.");
        } catch (Exception e) {
            Console.WriteLine("Error setting Host: " + e.Message);
        }

        try {
            UriBuilder u = new UriBuilder();
            u.Scheme = "dummyscheme";
            u.Host = "SSH:hostname";
            Console.WriteLine($"Host '{u.Host}' set successfully.");
        } catch (Exception e) {
            Console.WriteLine("Error setting Host: " + e.Message);
        }
    }
}
