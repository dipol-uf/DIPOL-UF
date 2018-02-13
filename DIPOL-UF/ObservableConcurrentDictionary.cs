using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;


namespace DIPOL_UF
{
    public class ObservableConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        protected Dispatcher Dispatcher = System.Windows.Application.Current?.Dispatcher;

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;


        public ObservableConcurrentDictionary()
        {}
        public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) : base(source)
        { }

        [System.Runtime.CompilerServices.IndexerName("Item")]
        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                var old = default(TValue);
                var keyExists = ContainsKey(key);

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
            var isUpdate = false;
            var result = default(TValue);
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
            var isUpdate = false;
            var result = default(TValue);
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
            var isAdd = false;

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
            var isAdd = false;

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
            var added = false;
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
            var removed = false;
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
            try
            {
                return base.TryUpdate(key, newValue, comparisonValue);
            }
            finally
            {
                OnNotifyCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                    newValue, comparisonValue));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Keys)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Values)));
                OnPropertyChanged(this, new PropertyChangedEventArgs("Item[]"));

            }
        }

        protected virtual void OnNotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Dispatcher == null || !Dispatcher.IsAvailable())
                CollectionChanged?.Invoke(sender, e);
            else
                Dispatcher.Invoke(() => CollectionChanged?.Invoke(sender, e), DispatcherPriority.DataBind);
        }

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Dispatcher == null || !Dispatcher.IsAvailable())
                PropertyChanged?.Invoke(sender, e);
            else
                Dispatcher.Invoke(() => PropertyChanged?.Invoke(sender, e));
        }

        public void OnNotifyCollectionChanged(NotifyCollectionChangedEventArgs e)
            => OnNotifyCollectionChanged(this, e);

        public ObservableValueCollection ObservableValues() => new ObservableValueCollection(this);
        

        public class ObservableValueCollection : ICollection<TValue>, INotifyPropertyChanged, INotifyCollectionChanged,
                                                  IReadOnlyCollection<TValue>
        {
            private readonly ObservableConcurrentDictionary<TKey, TValue> _sourceCollection;

            public IEnumerator<TValue> GetEnumerator()
                => _sourceCollection.Values.GetEnumerator();


            IEnumerator IEnumerable.GetEnumerator()
                => (_sourceCollection.Values as IEnumerable).GetEnumerator();

            public void Add(TValue item) => throw new NotSupportedException("Collection is read-only");
           
            public void Clear() => throw new NotSupportedException("Collection is read-only");

            public bool Contains(TValue item) => _sourceCollection.Values.Contains(item);

            public void CopyTo(TValue[] array, int arrayIndex) => _sourceCollection.Values.CopyTo(array, arrayIndex);
           
            public bool Remove(TValue item) => throw new NotSupportedException("Collection is read-only");

            int ICollection<TValue>.Count => _sourceCollection.Count;

            public bool IsReadOnly => true;
            public event PropertyChangedEventHandler PropertyChanged;
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            int IReadOnlyCollection<TValue>.Count => _sourceCollection.Count;

            public ObservableValueCollection(ObservableConcurrentDictionary<TKey, TValue> init)
            {
                _sourceCollection = init;
                _sourceCollection.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Count))
                        PropertyChanged?.Invoke(this, e);

                };

                _sourceCollection.CollectionChanged += (sender, e) =>
                {
                    List<TValue> newItems = null;
                    List<TValue> oldItems = null;
                    if (e.NewItems != null)
                    {
                        newItems =  new List<TValue>(e.NewItems.Count);
                        newItems.AddRange(from object item in e.NewItems select ((KeyValuePair<TKey, TValue>) item).Value);
                    }

                    if (e.OldItems != null)
                    {
                        oldItems = new List<TValue>(e.OldItems.Count);
                        oldItems.AddRange(from object item in e.OldItems select ((KeyValuePair<TKey, TValue>)item).Value);
                    }

                    NotifyCollectionChangedEventArgs args;
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            args = new NotifyCollectionChangedEventArgs(e.Action, newItems[0]);
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            args = new NotifyCollectionChangedEventArgs(e.Action, newItems[0], oldItems[0]);
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            args = new NotifyCollectionChangedEventArgs(e.Action);
                            break;
                        default:
                            args = null;
                            break;
                    }
                    if (args != null)
                        CollectionChanged?.Invoke(this, args);


                    
                };
            }
        }
    }
}
