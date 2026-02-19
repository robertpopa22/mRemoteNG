using System;

public class Program
{
    public static void Main()
    {
        string connectionString = "MY-SERVER-CAPS";
        UriBuilder uriBuilder = new UriBuilder();
        uriBuilder.Scheme = "dummyscheme";
        uriBuilder.Host = connectionString;
        
        Console.WriteLine($"Original: {connectionString}");
        Console.WriteLine($"UriBuilder.Host: {uriBuilder.Host}");
        
        if (uriBuilder.Host != connectionString)
        {
            Console.WriteLine("UriBuilder.Host lowercases the hostname!");
        }
        else
        {
            Console.WriteLine("UriBuilder.Host preserves the hostname case.");
        }
    }
}
