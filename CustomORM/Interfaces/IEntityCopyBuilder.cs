namespace CustomORM.Interfaces
{
    public interface IEntityCopyBuilder<T> where T: class, new()
    {
        T CopyEntity(T entity);
    }
}