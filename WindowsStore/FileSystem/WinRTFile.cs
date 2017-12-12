using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using AdjustSdk.Pcl.FileSystem;

namespace AdjustSdk.FileSystem
{
    internal class WinRTFile : IFile
    {
        private readonly IStorageFile _wrappedFile;

        /// <summary>
        /// Creates a new <see cref="WinRTFile"/>
        /// </summary>
        /// <param name="wrappedFile">The WinRT <see cref="IStorageFile"/> to wrap</param>
        public WinRTFile(IStorageFile wrappedFile)
        {
            _wrappedFile = wrappedFile;
        }

        /// <summary>
        /// The name of the file
        /// </summary>
        public string Name => _wrappedFile.Name;

        /// <summary>
        /// The "full path" of the file, which should uniquely identify it within a given File System/>
        /// </summary>
        public string Path => _wrappedFile.Path;

        /// <summary>
        /// Opens the file
        /// </summary>
        /// <returns>A <see cref="Stream"/> which can be used to read from or write to the file</returns>
        public async Task<Stream> OpenAsync()
        {
            var wrtStream = await _wrappedFile.OpenAsync(FileAccessMode.Read).AsTask().ConfigureAwait(false);
            return wrtStream.AsStream();
        }

        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <returns>A task which will complete after the file is deleted.</returns>
        public async Task DeleteAsync()
        {
            await _wrappedFile.DeleteAsync().AsTask().ConfigureAwait(false);
        }
    }
}
