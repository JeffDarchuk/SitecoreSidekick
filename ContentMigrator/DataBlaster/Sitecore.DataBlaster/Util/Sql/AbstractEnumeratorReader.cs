using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql
{
    /// <summary>
    /// Helps mapping an IEnumerator to an IDataReader so it can e.g. be used in bulk copy.
    /// </summary>
    /// <typeparam name="T">Type to enumerate.</typeparam>
    public abstract class AbstractEnumeratorReader<T> : DbDataReader
    {
        private readonly Lazy<IEnumerator<T>> _lazyEnumerator;

        private IEnumerator<T> Enumerator => _lazyEnumerator.Value;

        public T Current => Enumerator.Current;

        private bool _closed;

        protected AbstractEnumeratorReader(Func<IEnumerator<T>> enumeratorFactory)
        {
            if (enumeratorFactory == null)
                throw new ArgumentNullException(nameof(enumeratorFactory));

            _lazyEnumerator = new Lazy<IEnumerator<T>>(enumeratorFactory);
        }

        #region IDataReader Members

        /// <summary>
        /// Closes the <see cref="T:System.Data.IDataReader"/> Object.
        /// </summary>
        public override void Close()
        {
            // Read to end.
            while (Read())
            {
            }
            _closed = true;
        }

        /// <summary>
        /// Gets a value indicating the depth of nesting for the current row.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The level of nesting.
        /// </returns>
        public override int Depth => 0;

        /// <summary>
        /// Gets a value indicating whether the data reader is closed.
        /// </summary>
        /// <value></value>
        /// <returns>true if the data reader is closed; otherwise, false.
        /// </returns>
        public override bool IsClosed => _closed;

        /// <summary>
        /// Advances the data reader to the next result, when reading the results of batch SQL statements.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise, false.
        /// </returns>
        public override bool NextResult()
        {
            return Read();
        }

        /// <summary>
        /// Advances the <see cref="T:System.Data.IDataReader"/> to the next record.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise, false.
        /// </returns>
        public override bool Read()
        {
            return Enumerator.MoveNext();
        }

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The number of rows changed, inserted, or deleted; 0 if no rows were affected or the statement failed; and -1 for SELECT statements.
        /// </returns>
        public override int RecordsAffected => 0;

        #endregion

        #region IDataRecord Members

        public override bool GetBoolean(int i)
        {
            return GetFieldValue<bool>(i);
        }

        public override byte GetByte(int i)
        {
            return GetFieldValue<byte>(i);
        }

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public override char GetChar(int i)
        {
            return GetFieldValue<char>(i);
        }

        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public override string GetDataTypeName(int i)
        {
            throw new NotSupportedException();
        }

        public override DateTime GetDateTime(int i)
        {
            return GetFieldValue<DateTime>(i);
        }

        public override decimal GetDecimal(int i)
        {
            return GetFieldValue<decimal>(i);
        }

        public override double GetDouble(int i)
        {
            return GetFieldValue<double>(i);
        }

        public override Type GetFieldType(int i)
        {
            throw new NotSupportedException();
        }

        public override float GetFloat(int i)
        {
            return GetFieldValue<float>(i);
        }

        public override Guid GetGuid(int i)
        {
            return GetFieldValue<Guid>(i);
        }

        public override short GetInt16(int i)
        {
            return GetFieldValue<short>(i);
        }

        public override int GetInt32(int i)
        {
            return GetFieldValue<int>(i);
        }

        public override long GetInt64(int i)
        {
            return GetFieldValue<long>(i);
        }

        public override string GetName(int i)
        {
            throw new NotSupportedException();
        }

        public override int GetOrdinal(string name)
        {
            throw new NotSupportedException();
        }

        public override string GetString(int i)
        {
            return GetFieldValue<string>(i);
        }

        public override int GetValues(object[] values)
        {
            throw new NotSupportedException();
        }

        public override bool IsDBNull(int i)
        {
            return DBNull.Value.Equals(GetValue(i));
        }

        public override object this[string name]
        {
            get { throw new NotSupportedException(); }
        }

        public override object this[int i] => GetValue(i);

        #endregion

        #region DbDataReader Members

        public override bool HasRows => true;

        public override IEnumerator GetEnumerator()
        {
            return new DbEnumerator(this, true);
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
                Enumerator.Dispose();
            }
        }

        #endregion
    }
}