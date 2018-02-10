using System;
using System.Runtime.Serialization;

namespace NugetVendor.VendorDependenciesReader
{
    public class VendorDependenciesException : Exception
    {
        public VendorDependenciesException()
        {
        }

        public VendorDependenciesException(string message) : base(message)
        {
        }

        public VendorDependenciesException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected VendorDependenciesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}