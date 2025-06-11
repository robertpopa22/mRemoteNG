namespace mRemoteNG.Config.DataProviders
{
    public class InMemoryStringDataProvider(string initialContents = "") : IDataProvider<string>
    {
        private string _contents = initialContents;

        public string Load()
        {
            return _contents;
        }

        public void Save(string contents)
        {
            _contents = contents;
        }
    }
}