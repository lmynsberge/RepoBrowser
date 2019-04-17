using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RepoBrowser.Storage
{
    public class InMemoryRepository : IStorageRepository
    {
        private string _database;

        private Dictionary<long, object> _storageDictionary = new Dictionary<long, object>();

        public string Database { get => _database; private set => _database = value; }

        public InMemoryRepository(string database)
        {
            Database = database.ToUpper();
        }

        public long Create(object result)
        {
            throw new NotImplementedException();
        }

        public object Read(long id)
        {
            throw new NotImplementedException();
        }

        public bool Update(long id, object result)
        {
            throw new NotImplementedException();
        }

        public bool Delete(long id)
        {
            throw new NotImplementedException();
        }
    }
}
