using System;
using System.Text;
using mRemoteNG.Connection.Protocol.RDP;

public class TestEncoder {
    public static void Main() {
        var encoder = new AzureLoadBalanceInfoEncoder();
        string[] testCases = { "test", "test1", "", "Cookie: msts=3640205228.20480.0000", "tsv://MS Terminal Services Plugin.1.Collection" };

        foreach (var input in testCases) {
            try {
                string output = encoder.Encode(input);
                Console.WriteLine($"Input: '{input}'");
                Console.WriteLine($"Output Length: {output.Length}");
                Console.WriteLine($"Output Bytes: {BitConverter.ToString(Encoding.Unicode.GetBytes(output))}");
            } catch (Exception ex) {
                Console.WriteLine($"Error encoding '{input}': {ex.Message}");
            }
        }
    }
}
