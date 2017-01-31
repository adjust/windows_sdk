using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PCLStorage.Exceptions
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
