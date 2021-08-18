using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CompactJson
{
    /// <summary>
    /// compact JSON deserializer
    /// </summary>
    public class CompactJsonDeserializer
    {
        private readonly ReadOnlyDictionary<string, IAdditionalConverter> _convmap;

        /// <summary>
        /// construct
        /// </summary>
        /// <param name="additionalConverters">optional additional converter</param>
        public CompactJsonDeserializer(IEnumerable<IAdditionalConverter> additionalConverters = null)
        {
            if (!(additionalConverters is null))
            {
                _convmap = new ReadOnlyDictionary<string, IAdditionalConverter>(additionalConverters.ToDictionary(x => x.TypeFullName, x => x, StringComparer.Ordinal));
            }
            else
            {
                _convmap = new ReadOnlyDictionary<string, IAdditionalConverter>(new Dictionary<string, IAdditionalConverter>()); // empty.
            }
        }

        /// <summary>
        /// read rows from stream
        /// </summary>
        /// <param name="strm">stream to read from</param>
        /// <returns>resulting rows</returns>
        public List<IDictionary<string, object>> ReadRows(Stream strm)
        {
            var position = 0;
            if (!(strm is MemoryStream ms))
            {
                ms = new MemoryStream();
                strm.CopyTo(ms);
            }
            else
            {
                // if it was already a memory stream, read from its current position, not from start, for consistency
                position = (int)ms.Position;
            }

            return ReadRowsFromStreamTail(position, ms);
        }

        private List<IDictionary<string, object>> ReadRowsFromStreamTail(int position, MemoryStream ms)
        {
            var buf = ms.GetBuffer();
            var span = new Span<byte>(buf, position, (int)ms.Length);

            return ReadRows(span);
        }

        /// <summary>
        /// read rows from stream
        /// </summary>
        /// <param name="strm">stream to read from</param>
        /// <param name="ctoken">optional token for cancellation</param>
        /// <returns>resulting rows</returns>
        public async Task<List<IDictionary<string, object>>> ReadRowsAsync(Stream strm, CancellationToken ctoken = default)
        {
            var position = 0;
            if (!(strm is MemoryStream ms))
            {
                ms = new MemoryStream();
                await strm.CopyToAsync(ms, ctoken).ConfigureAwait(false);
            }
            else
            {
                // if it was already a memory stream, read from its current position, not from start, for consistency
                position = (int)ms.Position;
            }

            return ReadRowsFromStreamTail(position, ms);
        }

        /// <summary>
        /// read rows from span
        /// </summary>
        /// <param name="span">span to read from</param>
        /// <returns>resulting rows</returns>
        public List<IDictionary<string, object>> ReadRows(ReadOnlySpan<byte> span)
        {
            var jr = new Utf8JsonReader(span);
            ReadAsserted(ref jr, JsonTokenType.StartObject);
            ReadAsserted(ref jr, JsonTokenType.PropertyName, "keys");
            ReadAsserted(ref jr, JsonTokenType.StartArray);
            var tempkeys = new List<string>();
            while (jr.Read() && jr.TokenType != JsonTokenType.EndArray)
            {
                tempkeys.Add(jr.GetString());
            }
            var keycount = tempkeys.Count;
            var keys = tempkeys.ToArray();

            ReadAsserted(ref jr, JsonTokenType.PropertyName, "types");
            ReadAsserted(ref jr, JsonTokenType.StartArray);
            var temptypes = new List<string>(keycount);
            while (jr.Read() && jr.TokenType != JsonTokenType.EndArray)
            {
                temptypes.Add(jr.GetString());
            }
            var types = temptypes.ToArray();

            ReadAsserted(ref jr, JsonTokenType.PropertyName, "rowcount");
            ReadAsserted(ref jr, JsonTokenType.Number);
            var rowcount = jr.GetInt32();

            ReadAsserted(ref jr, JsonTokenType.PropertyName, "rows");
            ReadAsserted(ref jr, JsonTokenType.StartArray);

            var result = new List<IDictionary<string, object>>(rowcount);

            for (var ridx = 0; ridx < rowcount; ridx++)
            {
                ReadAsserted(ref jr, JsonTokenType.StartArray);
                ReadAsserted(ref jr, JsonTokenType.String);
                var flagspan = jr.ValueSpan;
                var row = new Dictionary<string, object>(keycount, StringComparer.Ordinal);
                for (var cidx = 0; cidx < keycount; cidx++)
                {
                    void Doset(object vvv)
                    {
                        row.Add(keys[cidx], vvv);
                    }

                    if (flagspan[cidx] == CompactJsonFormats.One)
                    {
                        switch (types[cidx])
                        {
                            case CompactJsonTypes.String:
                                ReadAsserted(ref jr, JsonTokenType.String);
                                Doset(jr.GetString());
                                break;
                            case CompactJsonTypes.Boolean:
                                ReadAsserted(ref jr, JsonTokenType.False, JsonTokenType.True);
                                Doset(jr.GetString());
                                break;
                            case CompactJsonTypes.Byte:
                                ReadAsserted(ref jr, JsonTokenType.Number);
                                Doset(jr.GetByte());
                                break;
                            case CompactJsonTypes.ByteArray:
                                ReadAsserted(ref jr, JsonTokenType.String);
                                Doset(jr.GetBytesFromBase64());
                                break;
                            case CompactJsonTypes.Int16:
                                ReadAsserted(ref jr, JsonTokenType.Number);
                                Doset(jr.GetInt16());
                                break;
                            case CompactJsonTypes.Int32:
                                ReadAsserted(ref jr, JsonTokenType.Number);
                                Doset(jr.GetInt32());
                                break;
                            case CompactJsonTypes.Int64:
                                ReadAsserted(ref jr, JsonTokenType.Number);
                                Doset(jr.GetInt64());
                                break;
                            case CompactJsonTypes.Decimal:
                                ReadAsserted(ref jr, JsonTokenType.Number);
                                Doset(jr.GetDecimal());
                                break;
                            case CompactJsonTypes.Double:
                                ReadAsserted(ref jr, JsonTokenType.Number);
                                Doset(jr.GetDouble());
                                break;
                            case CompactJsonTypes.DateTime:
                                ReadAsserted(ref jr, JsonTokenType.String);
                                Doset(DateTime.ParseExact(jr.GetString(), CompactJsonFormats.DateTime, CultureInfo.InvariantCulture));
                                break;
                            case CompactJsonTypes.DateTimeOffset:
                                ReadAsserted(ref jr, JsonTokenType.String);
                                Doset(DateTimeOffset.ParseExact(jr.GetString(), CompactJsonFormats.DateTimeOffset, CultureInfo.InvariantCulture));
                                break;
                            default:
                                if (_convmap.TryGetValue(types[cidx], out var additionalConverter))
                                {
                                    Doset(additionalConverter.ReadJson(ref jr));
                                }
                                else
                                {
                                    throw new InvalidOperationException($"unsupported type {types[cidx]}");
                                }
                                break;
                        }
                    }
                    else
                    {
                        Doset(null);
                    }
                }

                ReadAsserted(ref jr, JsonTokenType.EndArray);
                result.Add(row);
            }

            ReadAsserted(ref jr, JsonTokenType.EndArray);
            ReadAsserted(ref jr, JsonTokenType.EndObject);

            return result;
        }

        private static void ReadAsserted(ref Utf8JsonReader jr, JsonTokenType expected, string expectedstring = null)
        {
            if (!jr.Read())
            {
                throw new InvalidOperationException("was expecting a token");
            }
            if (jr.TokenType != expected)
            {
                throw new InvalidOperationException($"was expecting {expected} (saw {jr.TokenType} instead)");
            }
            if (!(expectedstring is null))
            {
                if (!jr.ValueTextEquals(expectedstring))
                {
                    throw new InvalidOperationException($"was execting the {jr.TokenType} to say '{expectedstring}'");
                }
            }
        }

        private static void ReadAsserted(ref Utf8JsonReader jr, JsonTokenType expected, JsonTokenType expected2)
        {
            if (!jr.Read())
            {
                throw new InvalidOperationException("was expecting a token");
            }
            if (jr.TokenType != expected && jr.TokenType != expected2)
            {
                throw new InvalidOperationException($"was expecting {expected} or {expected2} (saw {jr.TokenType} instead)");
            }
        }
    }
}
