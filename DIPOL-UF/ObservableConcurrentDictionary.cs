using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Windows.Threading;


namespace DIPOL_UF
{
    class ObservableConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        protected Dispatcher dispatcher = System.Windows.Application.Current?.Dispatcher;

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;


        public ObservableConcurrentDictionary() : base()
        {}
        public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) : base(source)
        { }

        [System.Runtime.CompilerServices.IndexerName("Item")]
        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                TValue old = default(TValue);
                bool keyExists = ContainsKey(key);

                if (keyExists)
                    old = base[key];
                
                base[key] = value;

                if (keyExists)
                    OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                        new KeyValuePair<TKey, TValue>(key, value),
                        new KeyValuePair<TKey, TValue>(key, old)));
                else
                    OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
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
                return added = base.TryAdd(key, value);
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
                    OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
        {
            if (dispatcher == null)
                CollectionChanged?.Invoke(sender, e);
            else
                dispatcher.Invoke(() => CollectionChanged?.Invoke(sender, e), DispatcherPriority.DataBind);
        }

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (dispatcher == null)
                PropertyChanged?.Invoke(sender, e);
            else
                dispatcher.Invoke(() => PropertyChanged?.Invoke(sender, e));
        }

        public void OnNotifyCollectionChanged(NotifyCollectionChangedEventArgs e)
            => OnNotifyCollectionChanged(this, e);


    }
}
