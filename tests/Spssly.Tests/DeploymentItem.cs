using System;
using System.IO;
using System.Reflection;

namespace Spssly.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
    public class DeploymentItem : System.Attribute
    {
        private readonly string _itemPath;
        private readonly string _filePath;
        private readonly string _binFolderPath;
        private readonly string _itemPathInBin;
        private readonly DirectoryInfo _environmentDir;
        private readonly Uri _itemPathUri;
        private readonly Uri _itemPathInBinUri;

        public DeploymentItem(string fileProjectRelativePath)
        {
            _filePath = fileProjectRelativePath.Replace("/", @"\");

            _environmentDir = new DirectoryInfo(Environment.CurrentDirectory);
            _itemPathUri = new Uri(Path.Combine(_environmentDir.Parent.Parent.FullName
                , _filePath));

            _itemPath = _itemPathUri.LocalPath;
            _binFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _itemPathInBinUri = new Uri(Path.Combine(_binFolderPath, _filePath));
            _itemPathInBin = _itemPathInBinUri.LocalPath;

            if (File.Exists(_itemPathInBin))
            {
                File.Delete(_itemPathInBin);
            }

            if (File.Exists(_itemPath))
            {
                File.Copy(_itemPath, _itemPathInBin);
            }
        }
    }
}