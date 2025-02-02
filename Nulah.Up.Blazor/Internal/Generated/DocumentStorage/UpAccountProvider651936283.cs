// <auto-generated/>
#pragma warning disable
using Marten.Internal;
using Marten.Internal.Storage;
using Marten.Schema;
using Marten.Schema.Arguments;
using Npgsql;
using Nulah.UpApi.Domain.Models;
using System;
using System.Collections.Generic;
using Weasel.Core;
using Weasel.Postgresql;

namespace Marten.Generated.DocumentStorage
{
    // START: UpsertUpAccountOperation651936283
    public class UpsertUpAccountOperation651936283 : Marten.Internal.Operations.StorageOperation<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Nulah.UpApi.Domain.Models.UpAccount _document;
        private readonly string _id;
        private readonly System.Collections.Generic.Dictionary<string, System.Guid> _versions;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public UpsertUpAccountOperation651936283(Nulah.UpApi.Domain.Models.UpAccount document, string id, System.Collections.Generic.Dictionary<string, System.Guid> versions, Marten.Schema.DocumentMapping mapping) : base(document, id, versions, mapping)
        {
            _document = document;
            _id = id;
            _versions = versions;
            _mapping = mapping;
        }


        public const string COMMAND_TEXT = "select public.mt_upsert_upaccount(?, ?, ?, ?)";


        public override void Postprocess(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions)
        {
            storeVersion();
        }


        public override System.Threading.Tasks.Task PostprocessAsync(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions, System.Threading.CancellationToken token)
        {
            storeVersion();
            // Nothing
            return System.Threading.Tasks.Task.CompletedTask;
        }


        public override Marten.Internal.Operations.OperationRole Role()
        {
            return Marten.Internal.Operations.OperationRole.Upsert;
        }


        public override string CommandText()
        {
            return COMMAND_TEXT;
        }


        public override NpgsqlTypes.NpgsqlDbType DbType()
        {
            return NpgsqlTypes.NpgsqlDbType.Text;
        }


        public override void ConfigureParameters(Npgsql.NpgsqlParameter[] parameters, Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session)
        {
            parameters[0].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            parameters[0].Value = session.Serializer.ToJson(_document);
            // .Net Class Type
            parameters[1].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;
            parameters[1].Value = _document.GetType().FullName;
            parameters[2].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;

            if (document.Id != null)
            {
                parameters[2].Value = document.Id;
            }

            else
            {
                parameters[2].Value = System.DBNull.Value;
            }

            setVersionParameter(parameters[3]);
        }

    }

    // END: UpsertUpAccountOperation651936283
    
    
    // START: InsertUpAccountOperation651936283
    public class InsertUpAccountOperation651936283 : Marten.Internal.Operations.StorageOperation<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Nulah.UpApi.Domain.Models.UpAccount _document;
        private readonly string _id;
        private readonly System.Collections.Generic.Dictionary<string, System.Guid> _versions;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public InsertUpAccountOperation651936283(Nulah.UpApi.Domain.Models.UpAccount document, string id, System.Collections.Generic.Dictionary<string, System.Guid> versions, Marten.Schema.DocumentMapping mapping) : base(document, id, versions, mapping)
        {
            _document = document;
            _id = id;
            _versions = versions;
            _mapping = mapping;
        }


        public const string COMMAND_TEXT = "select public.mt_insert_upaccount(?, ?, ?, ?)";


        public override void Postprocess(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions)
        {
            storeVersion();
        }


        public override System.Threading.Tasks.Task PostprocessAsync(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions, System.Threading.CancellationToken token)
        {
            storeVersion();
            // Nothing
            return System.Threading.Tasks.Task.CompletedTask;
        }


        public override Marten.Internal.Operations.OperationRole Role()
        {
            return Marten.Internal.Operations.OperationRole.Insert;
        }


        public override string CommandText()
        {
            return COMMAND_TEXT;
        }


        public override NpgsqlTypes.NpgsqlDbType DbType()
        {
            return NpgsqlTypes.NpgsqlDbType.Text;
        }


        public override void ConfigureParameters(Npgsql.NpgsqlParameter[] parameters, Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session)
        {
            parameters[0].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            parameters[0].Value = session.Serializer.ToJson(_document);
            // .Net Class Type
            parameters[1].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;
            parameters[1].Value = _document.GetType().FullName;
            parameters[2].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;

            if (document.Id != null)
            {
                parameters[2].Value = document.Id;
            }

            else
            {
                parameters[2].Value = System.DBNull.Value;
            }

            setVersionParameter(parameters[3]);
        }

    }

    // END: InsertUpAccountOperation651936283
    
    
    // START: UpdateUpAccountOperation651936283
    public class UpdateUpAccountOperation651936283 : Marten.Internal.Operations.StorageOperation<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Nulah.UpApi.Domain.Models.UpAccount _document;
        private readonly string _id;
        private readonly System.Collections.Generic.Dictionary<string, System.Guid> _versions;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public UpdateUpAccountOperation651936283(Nulah.UpApi.Domain.Models.UpAccount document, string id, System.Collections.Generic.Dictionary<string, System.Guid> versions, Marten.Schema.DocumentMapping mapping) : base(document, id, versions, mapping)
        {
            _document = document;
            _id = id;
            _versions = versions;
            _mapping = mapping;
        }


        public const string COMMAND_TEXT = "select public.mt_update_upaccount(?, ?, ?, ?)";


        public override void Postprocess(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions)
        {
            storeVersion();
            postprocessUpdate(reader, exceptions);
        }


        public override async System.Threading.Tasks.Task PostprocessAsync(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions, System.Threading.CancellationToken token)
        {
            storeVersion();
            await postprocessUpdateAsync(reader, exceptions, token);
        }


        public override Marten.Internal.Operations.OperationRole Role()
        {
            return Marten.Internal.Operations.OperationRole.Update;
        }


        public override string CommandText()
        {
            return COMMAND_TEXT;
        }


        public override NpgsqlTypes.NpgsqlDbType DbType()
        {
            return NpgsqlTypes.NpgsqlDbType.Text;
        }


        public override void ConfigureParameters(Npgsql.NpgsqlParameter[] parameters, Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session)
        {
            parameters[0].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            parameters[0].Value = session.Serializer.ToJson(_document);
            // .Net Class Type
            parameters[1].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;
            parameters[1].Value = _document.GetType().FullName;
            parameters[2].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;

            if (document.Id != null)
            {
                parameters[2].Value = document.Id;
            }

            else
            {
                parameters[2].Value = System.DBNull.Value;
            }

            setVersionParameter(parameters[3]);
        }

    }

    // END: UpdateUpAccountOperation651936283
    
    
    // START: QueryOnlyUpAccountSelector651936283
    public class QueryOnlyUpAccountSelector651936283 : Marten.Internal.CodeGeneration.DocumentSelectorWithOnlySerializer, Marten.Linq.Selectors.ISelector<Nulah.UpApi.Domain.Models.UpAccount>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public QueryOnlyUpAccountSelector651936283(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Nulah.UpApi.Domain.Models.UpAccount Resolve(System.Data.Common.DbDataReader reader)
        {

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = _serializer.FromJson<Nulah.UpApi.Domain.Models.UpAccount>(reader, 0);
            var lastModified = reader.GetFieldValue<System.DateTimeOffset>(1);
            document.ModifiedAt = lastModified;
            return document;
        }


        public async System.Threading.Tasks.Task<Nulah.UpApi.Domain.Models.UpAccount> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = await _serializer.FromJsonAsync<Nulah.UpApi.Domain.Models.UpAccount>(reader, 0, token).ConfigureAwait(false);
            var lastModified = await reader.GetFieldValueAsync<System.DateTimeOffset>(1, token);
            document.ModifiedAt = lastModified;
            return document;
        }

    }

    // END: QueryOnlyUpAccountSelector651936283
    
    
    // START: LightweightUpAccountSelector651936283
    public class LightweightUpAccountSelector651936283 : Marten.Internal.CodeGeneration.DocumentSelectorWithVersions<Nulah.UpApi.Domain.Models.UpAccount, string>, Marten.Linq.Selectors.ISelector<Nulah.UpApi.Domain.Models.UpAccount>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public LightweightUpAccountSelector651936283(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Nulah.UpApi.Domain.Models.UpAccount Resolve(System.Data.Common.DbDataReader reader)
        {
            var id = reader.GetFieldValue<string>(0);

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = _serializer.FromJson<Nulah.UpApi.Domain.Models.UpAccount>(reader, 1);
            var lastModified = reader.GetFieldValue<System.DateTimeOffset>(2);
            document.ModifiedAt = lastModified;
            _session.MarkAsDocumentLoaded(id, document);
            return document;
        }


        public async System.Threading.Tasks.Task<Nulah.UpApi.Domain.Models.UpAccount> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {
            var id = await reader.GetFieldValueAsync<string>(0, token);

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = await _serializer.FromJsonAsync<Nulah.UpApi.Domain.Models.UpAccount>(reader, 1, token).ConfigureAwait(false);
            var lastModified = await reader.GetFieldValueAsync<System.DateTimeOffset>(2, token);
            document.ModifiedAt = lastModified;
            _session.MarkAsDocumentLoaded(id, document);
            return document;
        }

    }

    // END: LightweightUpAccountSelector651936283
    
    
    // START: IdentityMapUpAccountSelector651936283
    public class IdentityMapUpAccountSelector651936283 : Marten.Internal.CodeGeneration.DocumentSelectorWithIdentityMap<Nulah.UpApi.Domain.Models.UpAccount, string>, Marten.Linq.Selectors.ISelector<Nulah.UpApi.Domain.Models.UpAccount>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public IdentityMapUpAccountSelector651936283(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Nulah.UpApi.Domain.Models.UpAccount Resolve(System.Data.Common.DbDataReader reader)
        {
            var id = reader.GetFieldValue<string>(0);
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = _serializer.FromJson<Nulah.UpApi.Domain.Models.UpAccount>(reader, 1);
            var lastModified = reader.GetFieldValue<System.DateTimeOffset>(2);
            document.ModifiedAt = lastModified;
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            return document;
        }


        public async System.Threading.Tasks.Task<Nulah.UpApi.Domain.Models.UpAccount> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {
            var id = await reader.GetFieldValueAsync<string>(0, token);
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = await _serializer.FromJsonAsync<Nulah.UpApi.Domain.Models.UpAccount>(reader, 1, token).ConfigureAwait(false);
            var lastModified = await reader.GetFieldValueAsync<System.DateTimeOffset>(2, token);
            document.ModifiedAt = lastModified;
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            return document;
        }

    }

    // END: IdentityMapUpAccountSelector651936283
    
    
    // START: DirtyTrackingUpAccountSelector651936283
    public class DirtyTrackingUpAccountSelector651936283 : Marten.Internal.CodeGeneration.DocumentSelectorWithDirtyChecking<Nulah.UpApi.Domain.Models.UpAccount, string>, Marten.Linq.Selectors.ISelector<Nulah.UpApi.Domain.Models.UpAccount>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public DirtyTrackingUpAccountSelector651936283(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Nulah.UpApi.Domain.Models.UpAccount Resolve(System.Data.Common.DbDataReader reader)
        {
            var id = reader.GetFieldValue<string>(0);
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = _serializer.FromJson<Nulah.UpApi.Domain.Models.UpAccount>(reader, 1);
            var lastModified = reader.GetFieldValue<System.DateTimeOffset>(2);
            document.ModifiedAt = lastModified;
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            StoreTracker(_session, document);
            return document;
        }


        public async System.Threading.Tasks.Task<Nulah.UpApi.Domain.Models.UpAccount> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {
            var id = await reader.GetFieldValueAsync<string>(0, token);
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Nulah.UpApi.Domain.Models.UpAccount document;
            document = await _serializer.FromJsonAsync<Nulah.UpApi.Domain.Models.UpAccount>(reader, 1, token).ConfigureAwait(false);
            var lastModified = await reader.GetFieldValueAsync<System.DateTimeOffset>(2, token);
            document.ModifiedAt = lastModified;
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            StoreTracker(_session, document);
            return document;
        }

    }

    // END: DirtyTrackingUpAccountSelector651936283
    
    
    // START: QueryOnlyUpAccountDocumentStorage651936283
    public class QueryOnlyUpAccountDocumentStorage651936283 : Marten.Internal.Storage.QueryOnlyDocumentStorage<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public QueryOnlyUpAccountDocumentStorage651936283(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override string AssignIdentity(Nulah.UpApi.Domain.Models.UpAccount document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            if (string.IsNullOrEmpty(document.Id)) throw new InvalidOperationException("Id/id values cannot be null or empty");
            return document.Id;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override string Identity(Nulah.UpApi.Domain.Models.UpAccount document)
        {
            return document.Id;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.QueryOnlyUpAccountSelector651936283(session, _document);
        }

    }

    // END: QueryOnlyUpAccountDocumentStorage651936283
    
    
    // START: LightweightUpAccountDocumentStorage651936283
    public class LightweightUpAccountDocumentStorage651936283 : Marten.Internal.Storage.LightweightDocumentStorage<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public LightweightUpAccountDocumentStorage651936283(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override string AssignIdentity(Nulah.UpApi.Domain.Models.UpAccount document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            if (string.IsNullOrEmpty(document.Id)) throw new InvalidOperationException("Id/id values cannot be null or empty");
            return document.Id;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override string Identity(Nulah.UpApi.Domain.Models.UpAccount document)
        {
            return document.Id;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.LightweightUpAccountSelector651936283(session, _document);
        }

    }

    // END: LightweightUpAccountDocumentStorage651936283
    
    
    // START: IdentityMapUpAccountDocumentStorage651936283
    public class IdentityMapUpAccountDocumentStorage651936283 : Marten.Internal.Storage.IdentityMapDocumentStorage<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public IdentityMapUpAccountDocumentStorage651936283(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override string AssignIdentity(Nulah.UpApi.Domain.Models.UpAccount document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            if (string.IsNullOrEmpty(document.Id)) throw new InvalidOperationException("Id/id values cannot be null or empty");
            return document.Id;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override string Identity(Nulah.UpApi.Domain.Models.UpAccount document)
        {
            return document.Id;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.IdentityMapUpAccountSelector651936283(session, _document);
        }

    }

    // END: IdentityMapUpAccountDocumentStorage651936283
    
    
    // START: DirtyTrackingUpAccountDocumentStorage651936283
    public class DirtyTrackingUpAccountDocumentStorage651936283 : Marten.Internal.Storage.DirtyCheckedDocumentStorage<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public DirtyTrackingUpAccountDocumentStorage651936283(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override string AssignIdentity(Nulah.UpApi.Domain.Models.UpAccount document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            if (string.IsNullOrEmpty(document.Id)) throw new InvalidOperationException("Id/id values cannot be null or empty");
            return document.Id;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertUpAccountOperation651936283
            (
                document, Identity(document),
                session.Versions.ForType<Nulah.UpApi.Domain.Models.UpAccount, string>(),
                _document
                
            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Nulah.UpApi.Domain.Models.UpAccount document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override string Identity(Nulah.UpApi.Domain.Models.UpAccount document)
        {
            return document.Id;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.DirtyTrackingUpAccountSelector651936283(session, _document);
        }

    }

    // END: DirtyTrackingUpAccountDocumentStorage651936283
    
    
    // START: UpAccountBulkLoader651936283
    public class UpAccountBulkLoader651936283 : Marten.Internal.CodeGeneration.BulkLoader<Nulah.UpApi.Domain.Models.UpAccount, string>
    {
        private readonly Marten.Internal.Storage.IDocumentStorage<Nulah.UpApi.Domain.Models.UpAccount, string> _storage;

        public UpAccountBulkLoader651936283(Marten.Internal.Storage.IDocumentStorage<Nulah.UpApi.Domain.Models.UpAccount, string> storage) : base(storage)
        {
            _storage = storage;
        }


        public const string MAIN_LOADER_SQL = "COPY public.mt_doc_upaccount(\"mt_dotnet_type\", \"id\", \"mt_version\", \"data\") FROM STDIN BINARY";

        public const string TEMP_LOADER_SQL = "COPY mt_doc_upaccount_temp(\"mt_dotnet_type\", \"id\", \"mt_version\", \"data\") FROM STDIN BINARY";

        public const string COPY_NEW_DOCUMENTS_SQL = "insert into public.mt_doc_upaccount (\"id\", \"data\", \"mt_version\", \"mt_dotnet_type\", mt_last_modified) (select mt_doc_upaccount_temp.\"id\", mt_doc_upaccount_temp.\"data\", mt_doc_upaccount_temp.\"mt_version\", mt_doc_upaccount_temp.\"mt_dotnet_type\", transaction_timestamp() from mt_doc_upaccount_temp left join public.mt_doc_upaccount on mt_doc_upaccount_temp.id = public.mt_doc_upaccount.id where public.mt_doc_upaccount.id is null)";

        public const string OVERWRITE_SQL = "update public.mt_doc_upaccount target SET data = source.data, mt_version = source.mt_version, mt_dotnet_type = source.mt_dotnet_type, mt_last_modified = transaction_timestamp() FROM mt_doc_upaccount_temp source WHERE source.id = target.id";

        public const string CREATE_TEMP_TABLE_FOR_COPYING_SQL = "create temporary table mt_doc_upaccount_temp as select * from public.mt_doc_upaccount limit 0";


        public override string CreateTempTableForCopying()
        {
            return CREATE_TEMP_TABLE_FOR_COPYING_SQL;
        }


        public override string CopyNewDocumentsFromTempTable()
        {
            return COPY_NEW_DOCUMENTS_SQL;
        }


        public override string OverwriteDuplicatesFromTempTable()
        {
            return OVERWRITE_SQL;
        }


        public override void LoadRow(Npgsql.NpgsqlBinaryImporter writer, Nulah.UpApi.Domain.Models.UpAccount document, Marten.Storage.Tenant tenant, Marten.ISerializer serializer)
        {
            writer.Write(document.GetType().FullName, NpgsqlTypes.NpgsqlDbType.Varchar);
            writer.Write(document.Id, NpgsqlTypes.NpgsqlDbType.Text);
            writer.Write(Marten.Schema.Identity.CombGuidIdGeneration.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
            writer.Write(serializer.ToJson(document), NpgsqlTypes.NpgsqlDbType.Jsonb);
        }


        public override async System.Threading.Tasks.Task LoadRowAsync(Npgsql.NpgsqlBinaryImporter writer, Nulah.UpApi.Domain.Models.UpAccount document, Marten.Storage.Tenant tenant, Marten.ISerializer serializer, System.Threading.CancellationToken cancellation)
        {
            await writer.WriteAsync(document.GetType().FullName, NpgsqlTypes.NpgsqlDbType.Varchar, cancellation);
            await writer.WriteAsync(document.Id, NpgsqlTypes.NpgsqlDbType.Text, cancellation);
            await writer.WriteAsync(Marten.Schema.Identity.CombGuidIdGeneration.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid, cancellation);
            await writer.WriteAsync(serializer.ToJson(document), NpgsqlTypes.NpgsqlDbType.Jsonb, cancellation);
        }


        public override string MainLoaderSql()
        {
            return MAIN_LOADER_SQL;
        }


        public override string TempLoaderSql()
        {
            return TEMP_LOADER_SQL;
        }

    }

    // END: UpAccountBulkLoader651936283
    
    
    // START: UpAccountProvider651936283
    public class UpAccountProvider651936283 : Marten.Internal.Storage.DocumentProvider<Nulah.UpApi.Domain.Models.UpAccount>
    {
        private readonly Marten.Schema.DocumentMapping _mapping;

        public UpAccountProvider651936283(Marten.Schema.DocumentMapping mapping) : base(new UpAccountBulkLoader651936283(new QueryOnlyUpAccountDocumentStorage651936283(mapping)), new QueryOnlyUpAccountDocumentStorage651936283(mapping), new LightweightUpAccountDocumentStorage651936283(mapping), new IdentityMapUpAccountDocumentStorage651936283(mapping), new DirtyTrackingUpAccountDocumentStorage651936283(mapping))
        {
            _mapping = mapping;
        }


    }

    // END: UpAccountProvider651936283
    
    
}

