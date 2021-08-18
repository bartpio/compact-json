using System;
using System.Text.Json;

namespace CompactJson
{
    /// <summary>
    /// additional converter for Compact JSON
    /// cannot be used to override default serializaton/deserialization types
    /// </summary>
    public interface IAdditionalConverter
    {
        /// <summary>
        /// the <see cref="Type.FullName"/> of type we're converting
        /// </summary>
        string TypeFullName { get; }

        /// <summary>
        /// write something to the given writer
        /// </summary>
        /// <param name="value">value to serialize (guaranteed to be of type specified by <see cref="TypeFullName"/>, or null)</param>
        /// <param name="writer">json writer to engage</param>
        void WriteJson(object value, Utf8JsonWriter writer);

        /// <summary>
        /// read and convert a token from the reader
        /// </summary>
        /// <param name="reader">json reader to engage</param>
        /// <returns>a value of type specified by <see cref="TypeFullName"/>, or null</returns>
        object ReadJson(ref Utf8JsonReader reader);
    }
}
