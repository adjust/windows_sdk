using System;

namespace AdjustSdk.Exceptions
{
    /// <exclude/>
    public class FileNotFoundException
        : System.IO.FileNotFoundException
    {
        /// <exclude/>
        public FileNotFoundException(string message)
            : base(message)
        {

        }

        /// <exclude/>
        public FileNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    /// <exclude/>
    public class DirectoryNotFoundException
        : System.IO.FileNotFoundException
    {
        /// <exclude/>
        public DirectoryNotFoundException(string message)
            : base(message)
        {

        }

        /// <exclude/>
        public DirectoryNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
