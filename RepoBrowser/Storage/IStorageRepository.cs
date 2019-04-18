using System;
namespace RepoBrowser.Storage
{
    /// <summary>
    /// Quick CRUD repository interface.
    /// </summary>
    public interface IStorageRepository
    {
        long Create(object result);
        object Read(long id);
        bool Update(long id, object result);
        bool Delete(long id);
    }
}
