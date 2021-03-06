using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;
using SharpRepository.Repository.FetchStrategies;

namespace SharpRepository.XmlRepository
{
    public abstract class XmlRepositoryBase<T, TKey> : LinqRepositoryBase<T, TKey> where T : class, new()
    {
        private List<T> _items = new List<T>();
        private string _storagePath;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storagePath">Path to the directory.  The XML filename is determined by the TypeName</param>
        /// <param name="cachingStrategy"></param>
        internal XmlRepositoryBase(string storagePath, ICachingStrategy<T, TKey> cachingStrategy = null) : base(cachingStrategy) 
        {
            Initialize(storagePath);
        }

        private void Initialize(string storagePath)
        {
            _items = new List<T>();
            _storagePath = storagePath;

            if (!_storagePath.EndsWith(@"\"))
            {
                _storagePath += @"\";
            }

            _storagePath = String.Format("{0}{1}.xml", _storagePath, TypeName);

            // load up the items
            LoadItems();
        }

        private void LoadItems()
        {
            if (!File.Exists(_storagePath)) return;

            using (var stream = new FileStream(_storagePath, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                var serializer = new XmlSerializer(typeof(List<T>));
                _items = (List<T>)serializer.Deserialize(reader);
            }
        }

        protected List<T> Items
        {
            get
            {
                return _items;
            }
        }

        protected override IQueryable<T> BaseQuery(IFetchStrategy<T> fetchStrategy = null)
        {
            return Items.AsQueryable();
            //return CloneList(Items).AsQueryable();
        }

        protected override T GetQuery(TKey key, IFetchStrategy<T> fetchStrategy)
        {
            return BaseQuery(fetchStrategy).FirstOrDefault(x => MatchOnPrimaryKey(x, key));
        }

//        private static IEnumerable<T> CloneList(IList<T> list)
//        {
//            // when you Google deep copy of generic list every answer uses either the IClonable interface on the T or having the T be Serializable
//            //  since we can't really put those constraints on T I'm going to do it via reflection
//
//            var type = typeof(T);
//            var properties = type.GetProperties();
//
//            var clonedList = new List<T>(list.Count);
//
//            foreach (T item in list)
//            {
//                var newItem = new T();
//                foreach (var propInfo in properties)
//                {
//                    propInfo.SetValue(newItem, propInfo.GetValue(item, null), null);
//                }
//
//                clonedList.Add(newItem);
//            }
//
//            return clonedList;
//        }

        protected override void AddItem(T entity)
        {
            TKey id;

            if (GenerateKeyOnAdd && GetPrimaryKey(entity, out id) && Equals(id, default(TKey)))
            {
                id = GeneratePrimaryKey();
                SetPrimaryKey(entity, id);
            }
            
            Items.Add(entity);
        }

        protected override void DeleteItem(T entity)
        {
            TKey pkValue;
            GetPrimaryKey(entity, out pkValue);

            var index = Items.FindIndex(x => MatchOnPrimaryKey(x, pkValue));
            if (index >= 0)
            {
                Items.RemoveAt(index);
            }
        }

        protected override void UpdateItem(T entity)
        {
            TKey pkValue;
            GetPrimaryKey(entity, out pkValue);

            var index = Items.FindIndex(x => MatchOnPrimaryKey(x, pkValue));
            if (index >= 0)
            {
                Items[index] = entity;
            }
        }

        // need to match on primary key instead of using Equals() since the objects are not the same
        private bool MatchOnPrimaryKey(T item, TKey keyValue)
        {
            return GetPrimaryKey(item, out TKey value) && keyValue.Equals(value);
        }

        protected override void SaveChanges()
        {
            using (var stream = new FileStream(_storagePath, FileMode.Open))
            using (var writer = new StreamWriter(stream))
            {
                var serializer = new XmlSerializer(typeof(List<T>));
                serializer.Serialize(writer, Items);
            }
        }

        public override void Dispose()
        {
        }

        private TKey GeneratePrimaryKey()
        {
            if (typeof(TKey) == typeof(Guid))
            {
                return (TKey)Convert.ChangeType(Guid.NewGuid(), typeof(TKey));
            }

            if (typeof(TKey) == typeof(string))
            {
                return (TKey)Convert.ChangeType(Guid.NewGuid().ToString("N"), typeof(TKey));
            }

            var last = Items.LastOrDefault() ?? new T();

            if (typeof(TKey) == typeof(Int32))
            {
                TKey pkValue;
                GetPrimaryKey(last, out pkValue);

                var nextInt = Convert.ToInt32(pkValue) + 1;
                return (TKey)Convert.ChangeType(nextInt, typeof(TKey));
            }

            throw new InvalidOperationException("Primary key could not be generated. This only works for GUID, Int32 and String.");
        }

        public override string ToString()
        {
            return "SharpRepository.XmlRepository";
        }
    }
}