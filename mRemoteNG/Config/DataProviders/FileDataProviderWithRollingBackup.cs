using System.Runtime.Versioning;

namespace mRemoteNG.Config.DataProviders
{
    [SupportedOSPlatform("windows")]
    public class FileDataProviderWithRollingBackup : FileDataProvider
    {
        private readonly FileBackupCreator _fileBackupCreator = new FileBackupCreator();

        public FileDataProviderWithRollingBackup(string filePath) : base(filePath)
        {
        }

        public override void Save(string content)
        {
            TrySave(content);
        }

        public override bool TrySave(string content)
        {
            FileBackupCreator.CreateBackupFile(FilePath);
            return base.TrySave(content);
        }
    }
}