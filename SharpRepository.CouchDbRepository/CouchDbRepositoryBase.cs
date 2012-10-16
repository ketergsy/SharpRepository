﻿using System;
using System.Linq;
using SharpRepository.Repository;
using SharpRepository.Repository.FetchStrategies;

namespace SharpRepository.CouchDbRepository
{
    public class CouchDbRepositoryBase<T> : LinqRepositoryBase<T, string> where T : class, new()
    {
        protected CouchDbClient<T> Client;
        private readonly string _serverUrl;

        internal CouchDbRepositoryBase()
            : this("127.0.0.1", 5984)
         {
         }

        internal CouchDbRepositoryBase(string host)
            : this(host, 5984)
        {
        }

        internal CouchDbRepositoryBase(string host, int port, string database = null, string username = null, string password = null)
        {
            if (String.IsNullOrEmpty(database))
            {
                database = typeof (T).Name;
            }
            database = database.ToLower(); // CouchDb requires lowercase  database names

            _serverUrl = String.Format("http://{0}:{1}", host, port);

            Client = new CouchDbClient<T>(_serverUrl, database);

            if (!CouchDbManager.HasDatabase(_serverUrl, database))
            {
                CouchDbManager.CreateDatabase(_serverUrl, database);
            }
        }

        protected override IQueryable<T> BaseQuery(IFetchStrategy<T> fetchStrategy = null)
        {
            // TODO: this is terrible and ridiculously non-performant, change to be able to convert and use the Hammock fluent syntax or convert to JS map/reduce that CouchDb uses

            return Client.GetAllDocuments().AsQueryable();
        }

        // we override the implementation fro LinqBaseRepository becausee this is built in 
        protected override T GetQuery(string key)
        {
            var item = Client.GetDocument(key);

            // this always returns an object, so check to see if the PK is null, if so then return null
            string id;
            GetPrimaryKey(item, out id);

            return id == null ? null : item;
        }

        protected override void AddItem(T entity)
        {
            string id;
            if (GetPrimaryKey(entity, out id) && Equals(id, default(string)))
            {
                id = GeneratePrimaryKey();
                SetPrimaryKey(entity, id);
            }

            Client.CreateDocument(entity, id);
        }

        protected override void DeleteItem(T entity)
        {
            string id;
            if (!GetPrimaryKey(entity, out id))
                return;

            Client.DeleteDocument(id);
        }

        protected override void UpdateItem(T entity)
        {
            string id;
            GetPrimaryKey(entity, out id);

            Client.UpdateDocument(entity, id);
        }

        protected override void SaveChanges()
        {

        }

        public override void Dispose()
        {
            
        }

        private static string GeneratePrimaryKey()
        {
            return (string) Convert.ChangeType(Guid.NewGuid().ToString("N"), typeof (string));
        }
    }
}