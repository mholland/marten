﻿using System.Linq;
using System.Threading.Tasks;
using Marten.Services;
using Marten.Testing.Documents;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace Marten.Testing.Schema.Hierarchies
{
    public class persist_and_load_for_hierarchy_Tests : end_to_end_document_hierarchy_usage_Tests<IdentityMap>
    {
        [Fact]
        public void persist_and_load_top_level()
        {
            theSession.Store(user1);
            theSession.SaveChanges();

            theSession.Load<User>(user1.Id).ShouldBeTheSameAs(user1);

            using (var session = theStore.QuerySession())
            {
                session.Load<User>(user1.Id).ShouldNotBeTheSameAs(user1).ShouldNotBeNull();
            }
        }

        [Fact]
        public void persist_and_delete_top()
        {
            theSession.Store(user1);
            theSession.SaveChanges();

            theSession.Delete<User>(user1.Id);
            theSession.SaveChanges();

            theSession.Load<User>(user1.Id).ShouldBeNull();

        }

        [Fact]
        public void persist_and_delete_top_2()
        {
            theSession.Store(user1);
            theSession.SaveChanges();

            theSession.Delete<User>(user1);
            theSession.SaveChanges();

            theSession.Load<User>(user1.Id).ShouldBeNull();

        }


        [Fact]
        public void persist_and_load_subclass()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            theSession.Load<User>(admin1.Id).ShouldBeTheSameAs(admin1);
            theSession.Load<AdminUser>(admin1.Id).ShouldBeTheSameAs(admin1);

            using (var session = theStore.QuerySession())
            {
                session.Load<AdminUser>(admin1.Id).ShouldNotBeTheSameAs(admin1).ShouldNotBeNull();
                session.Load<User>(admin1.Id).ShouldNotBeTheSameAs(admin1).ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task persist_and_load_subclass_async()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            (await theSession.LoadAsync<User>(admin1.Id)).ShouldBeTheSameAs(admin1);
            (await theSession.LoadAsync<AdminUser>(admin1.Id)).ShouldBeTheSameAs(admin1);

            using (var session = theStore.QuerySession())
            {
                (await session.LoadAsync<AdminUser>(admin1.Id)).ShouldNotBeTheSameAs(admin1).ShouldNotBeNull();
                (await session.LoadAsync<User>(admin1.Id)).ShouldNotBeTheSameAs(admin1).ShouldNotBeNull();
            }
        }

        [Fact]
        public void persist_and_delete_subclass()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            theSession.Delete(admin1);

            theSession.SaveChanges();

            theSession.Load<User>(admin1.Id).ShouldBeNull();
            theSession.Load<AdminUser>(admin1.Id).ShouldBeNull();
        }



        [Fact]
        public void persist_and_delete_subclass_2()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            theSession.Delete<AdminUser>(admin1.Id);

            theSession.SaveChanges();

            theSession.Load<User>(admin1.Id).ShouldBeNull();
            theSession.Load<AdminUser>(admin1.Id).ShouldBeNull();
        }
    }

    public class query_through_mixed_population_Tests : end_to_end_document_hierarchy_usage_Tests<IdentityMap>
    {
        public query_through_mixed_population_Tests()
        {
            loadData();
        }

        [Fact]
        public void load_by_id_with_mixed_results_from_identity_map()
        {
            theSession.LoadMany<User>().ById(admin1.Id, super1.Id, user1.Id)
                .ToArray().ShouldHaveTheSameElementsAs(admin1, super1, user1);
        }

        [Fact]
        public async Task load_by_id_with_mixed_results_from_identity_map_async()
        {
            var users = await theSession.LoadMany<User>().ByIdAsync(admin1.Id, super1.Id, user1.Id);
            users.OrderBy(x => x.FirstName).ShouldHaveTheSameElementsAs(admin1, super1, user1);
        }

        [Fact]
        public void load_by_id_keys_from_base_class_resolved_from_identity_map()
        {
            theSession.LoadMany<AdminUser>().ById(admin1.Id, admin2.Id)
                .ShouldHaveTheSameElementsAs(admin1, admin2);
        }

        [Fact]
        public async Task load_by_id_keys_from_base_class_resolved_from_identity_map_async()
        {
            var users = await theSession.LoadMany<AdminUser>().ByIdAsync(admin1.Id, admin2.Id);
            users.ShouldHaveTheSameElementsAs(admin1, admin2);
        }

        [Fact]
        public void load_by_id_keys_from_base_class_clean()
        {
            using (var session = theStore.QuerySession())
            {
                session.LoadMany<AdminUser>().ById(admin1.Id, admin2.Id)
                    .Select(x => x.Id)
                    .ShouldHaveTheSameElementsAs(admin1.Id, admin2.Id);
            }


        }

        [Fact]
        public void load_by_id_with_mixed_results_fresh()
        {
            using (var session = theStore.QuerySession())
            {
                session.LoadMany<User>().ById(admin1.Id, super1.Id, user1.Id)
                    .ToArray().OrderBy(x => x.FirstName).Select(x => x.Id).ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, user1.Id);
            }
        }

        [Fact]
        public async Task load_by_id_with_mixed_results_fresh_async()
        {
            using (var session = theStore.QuerySession())
            {
                var users = await session.LoadMany<User>().ByIdAsync(admin1.Id, super1.Id, user1.Id);

                users.OrderBy(x => x.FirstName).Select(x => x.Id).ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, user1.Id);
            }
        }

        [Fact]
        public void query_against_all_with_no_where()
        {
            var users = theSession.Query<User>().OrderBy(x => x.FirstName).ToArray();
            users
                .Select(x => x.Id)
                .ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, admin2.Id, user1.Id, super2.Id, user2.Id);

            users.Select(x => x.GetType())
                .ShouldHaveTheSameElementsAs(typeof(AdminUser), typeof(SuperUser), typeof(AdminUser), typeof(User), typeof(SuperUser), typeof(User));
        }


        [Fact]
        public void identity_map_usage_from_select()
        {
            var users = theSession.Query<User>().OrderBy(x => x.FirstName).ToArray();
            users[0].ShouldBeTheSameAs(admin1);
            users[1].ShouldBeTheSameAs(super1);
            users[5].ShouldBeTheSameAs(user2);
        }

        [Fact]
        public void query_against_all_with_where_clause()
        {
            theSession.Query<User>().OrderBy(x => x.FirstName).Where(x => x.UserName.StartsWith("A"))
                .ToArray().Select(x => x.Id)
                .ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, user1.Id);
        }

        [Fact]
        public void query_for_only_a_subclass_with_no_where_clause()
        {
            theSession.Query<AdminUser>().OrderBy(x => x.FirstName).ToArray()
                .Select(x => x.Id).ShouldHaveTheSameElementsAs(admin1.Id, admin2.Id);
        }

        [Fact]
        public void query_for_only_a_subclass_with_where_clause()
        {
            theSession.Query<AdminUser>().Where(x => x.FirstName == "Eric").Single()
                .Id.ShouldBe(admin2.Id);
        }

        [Fact]
        public void clean_by_subclass_only_deletes_the_one_subclass()
        {
            theStore.Advanced.Clean.DeleteDocumentsFor(typeof(AdminUser));

            theSession.Query<User>().Any().ShouldBeTrue();
            theSession.Query<SuperUser>().Any().ShouldBeTrue();

            theSession.Query<AdminUser>().Any().ShouldBeFalse();
        }
    }

public abstract class end_to_end_document_hierarchy_usage_Tests<T> : DocumentSessionFixture<T> where T : IIdentityMap
    {
        protected User user1 = new User {UserName = "A1", FirstName = "Justin", LastName = "Houston"};
        protected User user2 = new User { UserName = "B1", FirstName = "Tamba", LastName = "Hali" };
        protected AdminUser admin1 = new AdminUser { UserName = "A2", FirstName = "Derrick", LastName = "Johnson", Region = "Midwest" };
        protected AdminUser admin2 = new AdminUser { UserName = "B2", FirstName = "Eric", LastName = "Berry", Region = "West Coast" };
        protected SuperUser super1 = new SuperUser { UserName = "A3", FirstName = "Dontari", LastName = "Poe", Role = "Expert" };
        protected SuperUser super2 = new SuperUser { UserName = "B3", FirstName = "Sean", LastName = "Smith", Role = "Master" };

        protected end_to_end_document_hierarchy_usage_Tests()
        {
            theStore.Schema.Alter(x =>
            {
                x.For<User>().AddSubclass<SuperUser>().AddSubclass<AdminUser>().Searchable(_ => _.UserName);
            });
        }

        protected void loadData()
        {
            theSession.Store(user1, user2, admin1, admin2, super1, super2);

            theSession.SaveChanges();
        }
    }
}