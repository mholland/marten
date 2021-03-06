﻿using System.Collections.Generic;
using System.IO;
using Baseline;
using Marten.Schema;
using Marten.Services;
using Marten.Util;

namespace Marten.Events
{
    public class EventStoreAdmin : IEventStoreAdmin
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly StoreOptions _options;
        private readonly IDocumentSchemaCreation _creation;
        private readonly ISerializer _serializer;

        public EventStoreAdmin(IConnectionFactory connectionFactory, StoreOptions options, IDocumentSchemaCreation creation, ISerializer serializer)
        {
            _connectionFactory = connectionFactory;
            _options = options;
            _creation = creation;
            _serializer = serializer;
        }

        public void LoadProjections(string directory)
        {
            var files = new FileSystem();

            using (var connection = new ManagedConnection(_connectionFactory))
            {
                files.FindFiles(directory, FileSet.Deep("*.js")).Each(file =>
                {
                    var body = files.ReadStringFromFile(file);
                    var name = Path.GetFileNameWithoutExtension(file);

                    connection.Execute(cmd =>
                    {
                        cmd.CallsSproc("mt_load_projection_body")
                            .With("proj_name", name)
                            .With("body", body)
                            .ExecuteNonQuery();

                    });
                });
            }


        }

        public void LoadProjection(string file)
        {
            throw new System.NotImplementedException();
        }

        public void ClearAllProjections()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ProjectionUsage> InitializeEventStoreInDatabase()
        {
            using (var connection = new ManagedConnection(_connectionFactory))
            {
                connection.Execute(cmd =>
                {
                    cmd.CallsSproc("mt_initialize_projections").ExecuteNonQuery();
                });
            }

            return ProjectionUsages();
        }

        public IEnumerable<ProjectionUsage> ProjectionUsages()
        {
            string json = null;
            using (var connection = new ManagedConnection(_connectionFactory))
            {
                json = connection.Execute(cmd => cmd.CallsSproc("mt_get_projection_usage").ExecuteScalar().As<string>());
            }

            return _serializer.FromJson<ProjectionUsage[]>(json);
        }

        public void RebuildEventStoreSchema()
        {
            _creation.RunScript("mt_stream");
            _creation.RunScript("mt_initialize_projections");
            _creation.RunScript("mt_apply_transform");
            _creation.RunScript("mt_apply_aggregation");

            var js = SchemaBuilder.GetJavascript("mt_transforms");

            using (var connection = new ManagedConnection(_connectionFactory))
            {
                connection.Execute(cmd =>
                {
                    cmd.WithText("insert into mt_modules (name, definition) values (:name, :definition)")
                        .With("name", "mt_transforms")
                        .With("definition", js)
                        .ExecuteNonQuery();
                });
            }
        }
    }
}