using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CompactJson
{
    /// <summary>
    /// compact JSON serializer
    /// </summary>
    public sealed class CompactJsonSerializer : IDisposable
    {
        private readonly PeekingEnumerable<IDictionary<string, object>> _enu;
        private readonly string[] _keys;
        private readonly int _keycount;
        private readonly string[] _types;
        private readonly ReadOnlyDictionary<string, IAdditionalConverter> _convmap;

        /// <summary>
        /// if type is a nullable (ex. int?), make it not nullable
        /// </summary>
        /// <param name="typ">a potentially nullable type</param>
        /// <returns>
        /// if typ was of the form <see cref="Nullable<>"/>, the non-nullable variant (aka generic typearg to Nullable)
        /// if typ was not of the form <see cref="Nullable<>"/>, same typ as passed
        /// </returns>
        public static Type MakeNotNulla(Type typ)
        {
            if (typ.IsGenericType && typ.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return typ.GetGenericArguments().Single();
            }
            else
            {
                return typ;
            }
        }

        /// <summary>
        /// construct
        /// </summary>
        /// <param name="enu">rows we'll be serializing</param>
        /// <param name="additionalConverters">optional additional converters</param>
        public CompactJsonSerializer(IEnumerable<IDictionary<string, object>> enu, IEnumerable<IAdditionalConverter> additionalConverters = null)
        {
            if (enu is null)
            {
                throw new ArgumentNullException(nameof(enu));
            }

            _enu = new PeekingEnumerable<IDictionary<string, object>>(enu);
            if (!_enu.Empty)
            {
                _keys = _enu.First.Keys.ToArray();
                _keycount = _keys.Length;
                _types = new string[_keycount];
            }

            if (!(additionalConverters is null))
            {
                _convmap = new ReadOnlyDictionary<string, IAdditionalConverter>(additionalConverters.ToDictionary(x => x.TypeFullName, x => x, StringComparer.Ordinal));
            }
            else
            {
                _convmap = new ReadOnlyDictionary<string, IAdditionalConverter>(new Dictionary<string, IAdditionalConverter>()); // empty.
            }
        }

        private static readonly Regex _rxCheckspacer = new Regex(@"^[ ]+,\""rows\"":");

        /// <summary>
        /// write the rows that were supplied to constructor to the stream
        /// </summary>
        /// <param name="strm">stream to write out to</param>
        public void WriteRows(Stream strm)
        {
            using (var jw = new Utf8JsonWriter(strm))
            {
                var spacer = Enumerable.Repeat((byte)' ', 128 + 32 * _keycount).ToArray();
                var comma = new byte[1] { (byte)',' };

                Span<byte> flags = stackalloc byte[_keycount];
                var objects = new object[_keycount];
                var typeslearned = new List<(int idx, string typename)>(_keycount);
                var je_idx = JsonEncodedText.Encode("idx");
                var je_typename = JsonEncodedText.Encode("typename");

                jw.WriteStartObject();
                jw.WritePropertyName("keys");
                jw.WriteStartArray(); // keys
                foreach (var key in _keys)
                {
                    jw.WriteStringValue(key);
                }
                jw.WriteEndArray(); // END keys

                jw.Flush();
                var spaserpos = strm.Position;
                strm.Write(spacer);

                jw.WritePropertyName("rows");
                jw.WriteStartArray(); // array of rows
                var rowcount = 0;
                foreach (var row in _enu)
                {
                    rowcount++;
                    jw.WriteStartArray(); // a particular row
                    for (var idx = 0; idx < _keycount; idx++)
                    {
                        var valu = row[_keys[idx]];
                        objects[idx] = valu;
                        if (valu is null)
                        {
                            flags[idx] = CompactJsonFormats.Zero;
                        }
                        else
                        {
                            flags[idx] = CompactJsonFormats.One;
                            if (_types[idx] is null)
                            {
                                _types[idx] = MakeNotNulla(valu.GetType()).FullName;
                            }
                        }
                    }

                    jw.WriteStringValue(flags); // nullflags

                    for (var idx = 0; idx < _keycount; idx++)
                    {
                        var valu = objects[idx];
                        if (!(valu is null))
                        {
                            switch (_types[idx])
                            {
                                case CompactJsonTypes.String:
                                    jw.WriteStringValue(valu as string);
                                    break;
                                case CompactJsonTypes.Boolean:
                                    jw.WriteBooleanValue((Boolean)valu);
                                    break;
                                case CompactJsonTypes.Byte:
                                    jw.WriteNumberValue((Byte)valu);
                                    break;
                                case CompactJsonTypes.ByteArray:
                                    jw.WriteBase64StringValue(valu as byte[]);
                                    break;
                                case CompactJsonTypes.Int16:
                                    jw.WriteNumberValue((Int16)valu);
                                    break;
                                case CompactJsonTypes.Int32:
                                    jw.WriteNumberValue((Int32)valu);
                                    break;
                                case CompactJsonTypes.Int64:
                                    jw.WriteNumberValue((Int64)valu);
                                    break;
                                case CompactJsonTypes.Decimal:
                                    jw.WriteNumberValue((Decimal)valu);
                                    break;
                                case CompactJsonTypes.Double:
                                    jw.WriteNumberValue((Double)valu);
                                    break;
                                case CompactJsonTypes.DateTime:
                                    jw.WriteStringValue(((DateTime)valu).ToString(CompactJsonFormats.DateTime, CultureInfo.InvariantCulture));
                                    break;
                                case CompactJsonTypes.DateTimeOffset:
                                    jw.WriteStringValue(((DateTimeOffset)valu).ToString(CompactJsonFormats.DateTimeOffset, CultureInfo.InvariantCulture));
                                    break;
                                default:
                                    if (_convmap.TryGetValue(_types[idx], out var additionalConverter))
                                    {
                                        additionalConverter.WriteJson(valu, jw);
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException($"unsupported type {_types[idx]}");
                                    }
                                    break;
                            }
                        }
                    }
                    jw.WriteEndArray(); // END a particular row
                }

                jw.WriteEndArray(); // END array of rows
                jw.WriteEndObject();
                jw.Flush();
                strm.Seek(spaserpos, SeekOrigin.Begin);
                strm.Write(comma);
                using (var jwheader = new Utf8JsonWriter(strm, new JsonWriterOptions() { SkipValidation = true }))
                {
                    jwheader.WritePropertyName("types");
                    jwheader.WriteStartArray();
                    foreach (var typestring in _types)
                    {
                        jwheader.WriteStringValue(typestring ?? "System.Object");  // note System.Object means all-null
                    }
                    jwheader.WriteEndArray();
                    jwheader.WriteNumber("rowcount", rowcount);
                    jwheader.Flush();
                    var checkspan = new Span<byte>(new byte[spacer.Length * 2]);
                    strm.Read(checkspan);
                    var checkstring = Encoding.UTF8.GetString(checkspan);
                    if (!_rxCheckspacer.IsMatch(checkstring))
                    {
                        throw new InvalidOperationException("assertion failed in CompactJsonSerializer (header area not as expected)");
                    }
                }
                strm.Seek(0, SeekOrigin.End);
            }
        }

        /// <summary>
        /// disposal
        /// </summary>
        public void Dispose()
        {
            _enu.Dispose();
        }
    }
}
