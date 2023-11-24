using System;

namespace Certstream
{
    public class CertstreamException : Exception
    {
        public CertstreamException(string message) : base(message) { }
    }
}