using System;
using System.Data;
using Marten.Services;
using Marten.Util;
using NpgsqlTypes;

namespace Marten.Schema.Sequences
{
    public class HiloSequence : ISequence
    {
        private readonly IConnectionFactory _factory;
        private readonly string _entityName;
        private readonly object _lock = new object();

        public HiloSequence(IConnectionFactory factory, string entityName, HiloSettings settings)
        {
            _factory = factory;
            _entityName = entityName;

            CurrentHi = -1;
            CurrentLo = 1;
            MaxLo = settings.MaxLo;
            Increment = settings.Increment;
        }

        public string EntityName => _entityName;

        public long CurrentHi { get; private set; }
        public int CurrentLo { get; private set; }

        public int MaxLo { get; }
        public int Increment { get; private set; }

        public int NextInt()
        {
            return (int) NextLong();
        }

        public long NextLong()
        {
            lock (_lock)
            {
                if (ShouldAdvanceHi())
                {
                    AdvanceToNextHi();
                }

                return AdvanceValue();
            }
        }

        public void AdvanceToNextHi()
        {
            using (var conn = _factory.Create())
            {
                conn.Open();

                try
                {
                    var tx = conn.BeginTransaction(IsolationLevel.Serializable);
                    var raw = conn.CreateCommand().CallsSproc("mt_get_next_hi")
                        .With("entity", _entityName)
                        .Returns("next", NpgsqlDbType.Bigint).ExecuteScalar();

                    tx.Commit();

                    CurrentHi = Convert.ToInt64(raw);
                }
                finally
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            CurrentLo = 1;
        }

        public long AdvanceValue()
        {
            var result = (CurrentHi*MaxLo) + CurrentLo;
            CurrentLo++;

            return result;
        }


        public bool ShouldAdvanceHi()
        {
            return CurrentHi < 0 || CurrentLo > MaxLo;
        }
    }
}