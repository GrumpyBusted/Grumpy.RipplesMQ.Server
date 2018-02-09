using System;
using System.Runtime.Serialization;
using Grumpy.Json;

namespace Grumpy.RipplesMQ.Core.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class InvalidMessageTypeException : Exception
    {
        /// <inheritdoc />
        private InvalidMessageTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public InvalidMessageTypeException(string message, string expectedType, string actualType) : base("Invalid Message Type Exception")
        {
            Data.Add(nameof(message), message.TrySerializeToJson());
            Data.Add(nameof(expectedType), expectedType);
            Data.Add(nameof(actualType), actualType);
        }
    }
}