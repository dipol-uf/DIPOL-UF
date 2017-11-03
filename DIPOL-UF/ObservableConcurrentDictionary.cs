using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DIPOL_UF
{
    class ObservableConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, INotifyPropertyChanged, INotifyCollectionChanged 
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        [System.Runtime.CompilerServices.IndexerName("Item")]
        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                bool keyExists = ContainsKey(key);
                base[key] = value;

                OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(keyExists ? NotifyCollectionChangedAction.Replace : NotifyCollectionChangedAction.Add, 
                    new KeyValuePair<TKey, TValue>(key, value)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

                if (!keyExists)
                {
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                }
            }

        }

        public new TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            bool isUpdate = false;
            TValue result = default(TValue);
            try
            {
                isUpdate = ContainsKey(key);
                result = base.AddOrUpdate(key, addValue, updateValueFactory);
                return result;
            }
            finally
            {
                OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(
                    isUpdate ? NotifyCollectionChangedAction.Replace : NotifyCollectionChangedAction.Add,
                    new KeyValuePair<TKey, TValue>(key, result)));

                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));
                if (isUpdate)
                {
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                }

            }
        }
        public new TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            bool isUpdate = false;
            TValue result = default(TValue);
            try
            {
                isUpdate = ContainsKey(key);
                result = base.AddOrUpdate(key, addValueFactory, updateValueFactory);
                return result;
            }
            finally
            {
                OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(
                    isUpdate ? NotifyCollectionChangedAction.Replace : NotifyCollectionChangedAction.Add,
                    new KeyValuePair<TKey, TValue>(key, result)));

                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

                if (isUpdate)
                {
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                }

            }
        }
        public new void Clear()
        {
            base.Clear();

            OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(IsEmpty)));
            OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

        }
        public new TValue GetOrAdd(TKey key, TValue value)
        {
            bool isAdd = false;

            try
            {
                isAdd = ContainsKey(key);
                return base.GetOrAdd(key, value);
            }
            finally
            {
                if (isAdd)
                {
                    OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

                }
            }

        }
        public new TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            bool isAdd = false;

            try
            {
                isAdd = ContainsKey(key);
                return base.GetOrAdd(key, valueFactory);
            }
            finally
            {
                if (isAdd)
                {
                    OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

                }
            }

        }
        public new bool TryAdd(TKey key, TValue value)
        {
            bool added = false;
            try
            {
                return added = TryAdd(key, value);
            }
            finally
            {
                if (added)
                {
                    OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

                }
            }
        }
        public new bool TryRemove(TKey key, out TValue value)
        {
            bool removed = false;
            try
            {
                return removed = base.TryRemove(key, out value);
            }
            finally
            {
                if (removed)
                {
                    OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(IsEmpty)));
                    OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

                }
            }
        }
        public new bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            bool updated = false;
            try
            {
                return updated = base.TryUpdate(key, newValue, comparisonValue);
            }
            finally
            {
                OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

            }
        }

        protected virtual void OnNotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => CollectionChanged?.Invoke(sender, e);

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(sender, e);

    }
}
