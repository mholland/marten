﻿using System.Linq;
using Marten.Services;
using Marten.Testing.Fixtures;
using Shouldly;
using Xunit;

namespace Marten.Testing.Linq
{
    public class query_with_nullable_types_Tests : DocumentSessionFixture<NulloIdentityMap>
    {
        [Fact]
        public void query_against_non_null()
        {
            theSession.Store(new Target {NullableNumber = 3});
            theSession.Store(new Target {NullableNumber = 6});
            theSession.Store(new Target {NullableNumber = 7});
            theSession.Store(new Target {NullableNumber = 3});
            theSession.Store(new Target {NullableNumber = 5});

            theSession.SaveChanges();

            theSession.Query<Target>().Where(x => x.NullableNumber > 4).Count()
                .ShouldBe(3);
        }

        [Fact]
        public void query_against_null_1()
        {
            theSession.Store(new Target { NullableNumber = 3 });
            theSession.Store(new Target { NullableNumber = null });
            theSession.Store(new Target { NullableNumber = null });
            theSession.Store(new Target { NullableNumber = 3 });
            theSession.Store(new Target { NullableNumber = null });

            theSession.SaveChanges();

            theSession.Query<Target>().Where(x => x.NullableNumber == null).Count()
                .ShouldBe(3);
        }

        [Fact]
        public void query_against_null_2()
        {
            theSession.Store(new Target { NullableNumber = 3 });
            theSession.Store(new Target { NullableNumber = null });
            theSession.Store(new Target { NullableNumber = null });
            theSession.Store(new Target { NullableNumber = 3 });
            theSession.Store(new Target { NullableNumber = null });

            theSession.SaveChanges();

            theSession.Query<Target>().Where(x => !x.NullableNumber.HasValue).Count()
                .ShouldBe(3);

        }

        [Fact]
        public void query_against_not_null()
        {
            theSession.Store(new Target { NullableNumber = 3 });
            theSession.Store(new Target { NullableNumber = null });
            theSession.Store(new Target { NullableNumber = null });
            theSession.Store(new Target { NullableNumber = 3 });
            theSession.Store(new Target { NullableNumber = null });

            theSession.SaveChanges();

            theSession.Query<Target>().Where(x => x.NullableNumber.HasValue).Count()
                .ShouldBe(2);
        }
    }
}