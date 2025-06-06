using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace PlainNamedBinaryTag.Utils.Tests
{
    [TestClass]
    public class JvmModifiedUtf8Tests
    {
        private void AssertBufferEqual(byte[] comparison, byte[] testing)
        {
            var length = comparison.Length;
            Assert.AreEqual(length, testing.Length);
            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(comparison[i], testing[i]);
            }
        }

        [TestMethod]
        public void Test_AsciiString()
        {
            var original = "Hello World";
            var expected = Encoding.ASCII.GetBytes(original);
            var encoded = JvmModifiedUtf8.GetBytes(original);
            AssertBufferEqual(expected, encoded);
            var decoded = JvmModifiedUtf8.GetString(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void Test_Utf8String()
        {
            var original = "中文 Español";
            var expected = Encoding.UTF8.GetBytes(original);
            var encoded = JvmModifiedUtf8.GetBytes(original);
            AssertBufferEqual(expected, encoded);
            var decoded = JvmModifiedUtf8.GetString(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void Test_EmptyString()
        {
            var original = "";
            var encoded = JvmModifiedUtf8.GetBytes(original);
            Assert.AreEqual(0, encoded.Length);
            var decoded = JvmModifiedUtf8.GetString(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void Test_NullChar()
        {
            var original = "A\0B";
            var encoded = JvmModifiedUtf8.GetBytes(original);
            AssertBufferEqual(new byte[] { 0x41, 0xC0, 0x80, 0x42 }, encoded);
            var decoded = JvmModifiedUtf8.GetString(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void Test_SurrogatePair()
        {
            var original = "𐍈"; //\U00010348
            var expected = new byte[] { 0xED, 0xA0, 0x80, 0xED, 0xBD, 0x88 };
            var encoded = JvmModifiedUtf8.GetBytes(original);
            AssertBufferEqual(expected, encoded);
            var decoded = JvmModifiedUtf8.GetString(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void Test_InvalidSequence()
        {
            var invalidData = new byte[] { 0xC0, 0x41 };
            Assert.ThrowsException<FormatException>(() => JvmModifiedUtf8.GetString(invalidData));
        }
    }
}