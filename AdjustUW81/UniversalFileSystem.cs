using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace AdjustSdk
{
    /// <summary>
    /// Implementation of <see cref="IFileSystem"/> over Universal Storage APIs
    /// </summary>
    public class UniversalFileSystem : IFileSystem
    {
        Windows.Storage.ApplicationData _applicationData;

        /// <summary>
        /// Creates a new instance of <see cref="UniversalFileSystem"/>
        /// </summary>
        public UniversalFileSystem()
        {
            _applicationData = ApplicationData.Current;
        }
        /// <summary>
        /// A folder representing storage which is local to the current device
        /// </summary>
        public IFolder LocalStorage
        {
            get
            {
                return new UniversalFolder(_applicationData.LocalFolder);
            }
        }

        /// <summary>
        /// A folder representing storage which may be synced with other devices for the same user
        /// </summary>
        public IFolder RoamingStorage
        {
            get
            {
                return new UniversalFolder(_applicationData.RoamingFolder);
            }
        }

        /// <summary>
        /// Gets a file, given its path.  Returns null if the file does not exist.
        /// </summary>
        /// <param name="path">The path to a file, as returned from the <see cref="IFile.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A file for the given path, or null if it does not exist.</returns>
        public async Task<IFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            StorageFile storageFile;
            try
            {
                storageFile = await StorageFile.GetFileFromPathAsync(path).AsTask(cancellationToken).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            return new UniversalFile(storageFile);
        }

        /// <summary>
        /// Gets a folder, given its path.  Returns null if the folder does not exist.
        /// </summary>
        /// <param name="path">The path to a folder, as returned from the <see cref="IFolder.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A folder for the specified path, or null if it does not exist.</returns>
        public async Task<IFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            StorageFolder storageFolder;
            try
            {
                storageFolder = await StorageFolder.GetFolderFromPathAsync(path).AsTask(cancellationToken).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            return new UniversalFolder(storageFolder);
        }
    }
}
