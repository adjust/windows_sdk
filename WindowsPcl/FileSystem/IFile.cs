using System.IO;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl.FileSystem
{
    public interface IFile
    {
        string Name { get; }

        string Path { get; }
        
        Task<Stream> OpenAsync();

        Task DeleteAsync();
    }
}
