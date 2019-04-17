using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RepoBrowser.Storage
{
    public class InMemoryRepository : IStorageRepository
    {
        private string _database;
        private long _internalID = 0;

        private readonly Dictionary<long, object> _storageDictionary = new Dictionary<long, object>();

        public string Database { get => _database; private set => _database = value; }

        public InMemoryRepository(string database)
        {
            Database = database.ToUpper();
        }

        /// <summary>
        /// Create the specified result and return the ID
        /// </summary>
        /// <returns>The created ID.</returns>
        /// <param name="result">Result.</param>
        public long Create(object result)
        {
            // Increment ID and store
            _internalID++;
            _storageDictionary.Add(_internalID, result);
            return _internalID;
        }

        /// <summary>
        /// Given the specified ID, tries to get the object.
        /// </summary>
        /// <returns>The object</returns>
        /// <param name="id">Identifier.</param>
        public object Read(long id)
        {
            if(_storageDictionary.TryGetValue(id, out object result)) { return result; }
            return null;
        }

        /// <summary>
        /// Update the specified object with the result. Creates if doesn't exist.
        /// </summary>
        /// <returns>True if object existed and was updated, false otherwise.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="result">Result.</param>
        public bool Update(long id, object result)
        {
            if (_storageDictionary.TryGetValue(id, out object tempResult))
            {
                _storageDictionary[id] = result;
                return true;
            }
            else
            {
                // If creating and this number is higher than our existing counter, it is now the counter
                if (id > _internalID) { _internalID = id; }
                _storageDictionary.Add(id, result);
                return false;
            }
        }

        /// <summary>
        /// Deletes the object
        /// </summary>
        /// <returns>True if existed and was deleted, false otherwise</returns>
        /// <param name="id">Identifier.</param>
        public bool Delete(long id)
        {
            if (_storageDictionary.TryGetValue(id, out object tempResult))
            {
                _storageDictionary.Remove(id);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
