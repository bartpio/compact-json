using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using System.IO;

namespace CompactJson.Tests
{
    [TestFixture]
    public class TestJsonSerializer
    {
        private Dictionary<string, object> MakeRow(int idx)
        {
            var special = (idx % 10) == 0;

            return new Dictionary<string, object>()
            {
                ["personid"] = idx,
                ["somemoney"] = special ? null : (decimal?)(Convert.ToDecimal(idx) * 100_000.01m),
                ["customercode"] = $"customer {idx + 1000}"
            };
        }

        [Test]
        public void TestSerializeAndDeserialize()
        {
            var rows = Enumerable.Range(0, 1_000_000).Select(x => MakeRow(x)).ToList();

            var cjs = new CompactJsonSerializer(rows);
            var ms = new MemoryStream();
            var sw = Stopwatch.StartNew();
            cjs.WriteRows(ms);
            TestContext.WriteLine("writeout took {0:n0}ms", sw.ElapsedMilliseconds);

            var json = Encoding.UTF8.GetString(ms.ToArray());
            //TestContext.WriteLine(json);

            var cd = new CompactJsonDeserializer();
            ms.Seek(0, SeekOrigin.Begin);
            var swreadback = Stopwatch.StartNew();
            var readback = cd.ReadRows(ms);
            TestContext.WriteLine("readback took {0:n0}ms", swreadback.ElapsedMilliseconds);
            Assert.That(readback.Count, Is.EqualTo(rows.Count));
            var idx = 0;
            foreach (var actualrow in readback)
            {
                Assert.That(actualrow, Is.EqualTo(rows[idx]));
                idx++;
            }
        }

        [Test]
        public void TestSerializeUsingtempfile()
        {
            var rows = Enumerable.Range(0, 10).Select(x => MakeRow(x)).ToList();

            var cjs = new CompactJsonSerializer(rows);

            var tfname = Path.GetTempFileName();
            try
            {
                using (var fs = new FileStream(tfname, FileMode.Open, FileAccess.ReadWrite))
                {
                    cjs.WriteRows(fs);
                }

                var buf = File.ReadAllBytes(tfname);
                var ms = new MemoryStream(buf, false);
                var json = Encoding.UTF8.GetString(ms.ToArray());
                TestContext.WriteLine(json);
            }
            finally
            {
                try
                {
                    File.Delete(tfname);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
