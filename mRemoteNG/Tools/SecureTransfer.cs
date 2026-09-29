using System;
using System.IO;
using mRemoteNG.App;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using static System.IO.FileMode;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.Tools
{
    [SupportedOSPlatform("windows")]
    internal class SecureTransfer : IDisposable
    {
        private readonly string Host = string.Empty;
        private readonly string User = string.Empty;
        private readonly string Password = string.Empty;
        private readonly int Port;
        public readonly SSHTransferProtocol Protocol;
        public string SrcFile = string.Empty;
        public string DstFile = string.Empty;
        public ScpClient? ScpClt;
        public SftpClient? SftpClt;
        public SftpUploadAsyncResult? asyncResult;
        public AsyncCallback? asyncCallback;
        private FileStream? _uploadStream;


        /// <summary>
        /// Where the transfer reports failures. Defaults to the application's message collector, so
        /// the running product is unaffected; a test assigns its own and can then exercise the
        /// transfer without the notification stack behind it.
        ///
        /// A property rather than another constructor parameter: this class already configures
        /// everything else it needs the same way, and it has three constructors already.
        /// </summary>
        public Messages.IOperationMessageSink Messages { get; set; } = new Messages.DefaultMessageSink();

        public SecureTransfer()
        {
        }

        public SecureTransfer(string host, string user, string pass, int port, SSHTransferProtocol protocol)
        {
            Host = host;
            User = user;
            Password = pass;
            Port = port;
            Protocol = protocol;
        }

        public SecureTransfer(string host,
            string user,
            string pass,
            int port,
            SSHTransferProtocol protocol,
            string source,
            string dest)
        {
            Host = host;
            User = user;
            Password = pass;
            Port = port;
            Protocol = protocol;
            SrcFile = source.Trim('"');
            DstFile = dest.Trim('"');
        }

        public void Connect()
        {
            if (Protocol == SSHTransferProtocol.SCP)
            {
                ScpClt = CreateScpClient();
                ScpClt.Connect();
            }

            if (Protocol == SSHTransferProtocol.SFTP)
            {
                SftpClt = new SftpClient(Host, Port, User, Password);
                SftpClt.Connect();
            }
        }

        public void Disconnect()
        {
            if (Protocol == SSHTransferProtocol.SCP)
            {
                ScpClt?.Disconnect();
            }

            if (Protocol == SSHTransferProtocol.SFTP)
            {
                SftpClt?.Disconnect();
            }
        }

        internal ScpClient CreateScpClient() =>
            new(Host, Port, User, Password, RemotePathTransformation.ShellQuote);


        public void Upload()
        {
            var srcFile = SrcFile.Trim('"');
            var dstFile = DstFile.Trim('"');

            if (Protocol == SSHTransferProtocol.SCP)
            {
                if (ScpClt is null || !ScpClt.IsConnected)
                {
                    Messages.Error(
                        Language.SshTransferFailed + Environment.NewLine +
                        "SCP Not Connected!");
                    return;
                }

                ScpClt.Upload(new FileInfo(srcFile), dstFile);
            }

            if (Protocol == SSHTransferProtocol.SFTP)
            {
                if (SftpClt is null || !SftpClt.IsConnected)
                {
                    Messages.Error(
                        Language.SshTransferFailed + Environment.NewLine +
                        "SFTP Not Connected!");
                    return;
                }

                _uploadStream = new FileStream(srcFile, Open, FileAccess.Read, FileShare.Read);
                try
                {
                    asyncResult = (SftpUploadAsyncResult)SftpClt.BeginUploadFile(_uploadStream, dstFile, asyncCallback);
                }
                catch
                {
                    _uploadStream.Dispose();
                    _uploadStream = null;
                    throw;
                }
            }
        }

        public enum SSHTransferProtocol
        {
            SCP = 0,
            SFTP = 1
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Protocol == SSHTransferProtocol.SCP)
            {
                ScpClt?.Dispose();
            }

            if (Protocol == SSHTransferProtocol.SFTP)
            {
                try { SftpClt?.Dispose(); }
                finally { _uploadStream?.Dispose(); }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
