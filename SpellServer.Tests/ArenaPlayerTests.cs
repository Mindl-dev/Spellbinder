using System;
using System.Data;
using NUnit.Framework;

namespace SpellServer.Tests
{
    [TestFixture]
    public class DataRowCastTests
    {
        private DataTable _table;
        private DataRow _row;

        [SetUp]
        public void SetUp()
        {
            // SQLite returns Int64 for all integer columns.
            // This simulates what happens when you use SQLiteDataAdapter.Fill()
            _table = new DataTable();
            _table.Columns.Add("id", typeof(Int64));
            _table.Columns.Add("name", typeof(String));
            _table.Columns.Add("level", typeof(Int64));
            _table.Columns.Add("experience", typeof(Int64));

            _row = _table.NewRow();
            _row["id"] = (Int64)42;
            _row["name"] = "TestMage";
            _row["level"] = (Int64)5;
            _row["experience"] = (Int64)65000;
            _table.Rows.Add(_row);
        }

        [Test]
        public void ConvertToByte_WorksWithInt64()
        {
            // This is how we fixed the SQLite cast issue
            byte result = Convert.ToByte(_row["level"]);
            Assert.AreEqual(5, result);
        }

        [Test]
        public void ConvertToInt32_WorksWithInt64()
        {
            int result = Convert.ToInt32(_row["id"]);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void ConvertToUInt64_WorksWithInt64()
        {
            ulong result = Convert.ToUInt64(_row["experience"]);
            Assert.AreEqual(65000UL, result);
        }

        [Test]
        public void FieldByte_ThrowsOnInt64Column()
        {
            // This is the original bug — Field<Byte> can't handle Int64
            Assert.Throws<InvalidCastException>(() =>
            {
                _row.Field<Byte>("level");
            });
        }

        [Test]
        public void FieldInt32_ThrowsOnInt64Column()
        {
            Assert.Throws<InvalidCastException>(() =>
            {
                _row.Field<Int32>("id");
            });
        }

        [Test]
        public void DirectCast_ThrowsOnInt64ToByte()
        {
            // Direct (Byte) cast also fails
            Assert.Throws<InvalidCastException>(() =>
            {
                byte b = (byte)_row["level"];
            });
        }

        [Test]
        public void DirectCast_ThrowsOnInt64ToInt32()
        {
            Assert.Throws<InvalidCastException>(() =>
            {
                int i = (int)_row["id"];
            });
        }
    }
}
