using System;
using System.Collections;

namespace Kztek.Object.Entity
{
    public class BaseObjectCollection<T> : CollectionBase where T : BaseObject
    {
        public BaseObjectCollection()
        {

        }

        public T this[int index]
        {
            get { return (T)InnerList[index]; }
        }

        // Add
        public void Add(T obj)
        {
            InnerList.Add(obj);
        }

        // Remove
        public void Remove(T obj)
        {
            InnerList.Remove(obj);
        }

        public T? GetByName(string obj)
        {
            foreach (T item in this.InnerList)
            {
                if (item.Name.Equals(obj, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return default;
        }
        public T? GetById(string Id)
        {
            foreach (T item in this.InnerList)
            {
                if (item.Id.Equals(Id, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return default;
        }
    }
}