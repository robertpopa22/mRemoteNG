using System;
using Microsoft.Data.SqlClient;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LiteDB;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.Config.DatabaseConnectors
{
    //[SupportedOSPlatform("windows")]
    /// <summary>
    /// A helper class for testing database connectivity
    /// </summary>
    ///
    using System;
    using System.Data.SqlClient;

    public class DatabaseConnectionTester
    {
        public async Task<ConnectionTestResult> TestConnectivity(string type, string server, string database, string username, string password)
        {
            try
            {
                // Build the connection string based on the provided parameters
                string connectionString = $"Data Source={server};Initial Catalog={database};User ID={username};Password={password}";

                // Attempt to open a connection to the database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                }

                return ConnectionTestResult.ConnectionSucceded;
            }
            catch (SqlException ex)
            {
                // Handle specific SQL exceptions
                switch (ex.Number)
                {
                    case 4060: // Invalid Database
                        return ConnectionTestResult.UnknownDatabase;
                    case 18456: // Login Failed
                        return ConnectionTestResult.CredentialsRejected;
                    case -1: // Server not accessible
                        return ConnectionTestResult.ServerNotAccessible;
                    default:
                        return ConnectionTestResult.UnknownError;
                }
            }
            catch
            {
                // Handle any other exceptions
                return ConnectionTestResult.UnknownError;
            }
        }
    }
    //public class DatabaseConnectionTester
    //{
        //public async Task<ConnectionTestResult> TestConnectivity(string type, string server, string database, string username, string password)
        //{
            //using IDatabaseConnector dbConnector = DatabaseConnectorFactory.DatabaseConnector(type, server, database, username, password);
            //try
            //{
                // Validate architecture compatibility
                //if (!Environment.Is64BitProcess)
                //{
                //    throw new PlatformNotSupportedException("The application must run in a 64-bit process to use this database connector.");
               // }

                // Attempt to connect

                //using (SqlConnection connection = new SqlConnection("Data Source=172.22.155.100,1433;Initial Catalog=Demo;Integrated Security=False;User ID=sa;Password=London123;Multiple Active Result Sets=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Name=mRemoteNG;Application Intent=ReadOnly"))
                //{
                //    connection.Open();
                //    Console.WriteLine("Connection successful!");
                //}
                //Console.WriteLine($"{RuntimeInformation.OSArchitecture}");
                //Console.WriteLine($"{RuntimeInformation.ProcessArchitecture}");
                //try
                //{
                 //   using (SqlConnection connection = new SqlConnection("Data Source=172.22.155.100,1433;Initial Catalog=Demo;Integrated Security=False;User ID=sa;Password=London123;Multiple Active Result Sets=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Name=mRemoteNG;Application Intent=ReadOnly"))
                 //   {
                 //       connection.Open();
                 //       Console.WriteLine("Connection successful!");
                 //   }
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine($"Connection failed: {ex.Message}");
                //}
    //}
/*


                try
                {
                    using (SqlConnection connection = new SqlConnection("Data Source=172.22.155.100,1433;Initial Catalog=Demo;Integrated Security=False;User ID=sa;Password=London123;Multiple Active Result Sets=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Name=mRemoteNG;Application Intent=ReadOnly"))
                    {
                        connection.Open();
                    }
                }
                catch (TypeInitializationException ex)
                {
                    Console.WriteLine($"Type initialization error: {ex.InnerException?.Message}");
                }


                //await dbConnector.ConnectAsync();
                return ConnectionTestResult.ConnectionSucceded;
            }
            catch (PlatformNotSupportedException ex)
            {
                // Log or handle architecture mismatch
                Console.WriteLine(string.Format(Language.ErrorPlatformNotSupported, ex.Message));
                return ConnectionTestResult.UnknownError;
            }
            catch (DllNotFoundException ex)
            {
                // Handle missing native dependencies
                Console.WriteLine(string.Format(Language.ErrorMissingDependency, ex.Message));
                return ConnectionTestResult.UnknownError;
            }
            catch (BadImageFormatException ex)
            {
                // Handle architecture mismatch in native libraries
                Console.WriteLine(string.Format(Language.ErrorArchitectureMismatch, ex.Message));
                return ConnectionTestResult.UnknownError;
            }
            catch (SqlException sqlException)
            {
                if (sqlException.Message.Contains("The server was not found"))
                    return ConnectionTestResult.ServerNotAccessible;
                if (sqlException.Message.Contains("Cannot open database"))
                    return ConnectionTestResult.UnknownDatabase;
                if (sqlException.Message.Contains("Login failed for user"))
                    return ConnectionTestResult.CredentialsRejected;
                return ConnectionTestResult.UnknownError;
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return ConnectionTestResult.UnknownError;
            }
*/
       // }
   // }
}