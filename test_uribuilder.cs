using System;

class Program {
    static void Main() {
        try {
            UriBuilder u = new UriBuilder();
            u.Scheme = "dummyscheme";
            u.Host = "HOSTNAME";
            Console.WriteLine("Host set successfully: " + u.Host);
        } catch (Exception e) {
            Console.WriteLine("Error setting Host: " + e.Message);
        }

        try {
            UriBuilder u = new UriBuilder();
            u.Scheme = "dummyscheme";
            u.Host = "hostname";
            Console.WriteLine("Host set successfully: " + u.Host);
        } catch (Exception e) {
            Console.WriteLine("Error setting Host: " + e.Message);
        }
    }
}
