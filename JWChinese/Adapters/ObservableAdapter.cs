using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace JWChinese
{
    public class ObservableAdapter<T> : BaseAdapter<T>
    {
        private IList<T> list;
        private INotifyCollectionChanged notifier;

        public override int Count
        {
            get
            {
                return list == null ? 0 : list.Count;
            }
        }

        public IList<T> DataSource
        {
            get
            {
                return list;
            }
            set
            {
                if (list == value)
                {
                    return;
                }

                if (notifier != null)
                {
                    notifier.CollectionChanged -= NotifierCollectionChanged;
                }

                list = value;
                notifier = list as INotifyCollectionChanged;

                if (notifier != null)
                {
                    notifier.CollectionChanged += NotifierCollectionChanged;
                }
            }
        }

        public Func<int, T, View, View> GetTemplate
        {
            get;
            set;
        }

        public override T this[int index]
        {
            get
            {
                return list == null ? default(T) : list[index];
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (GetTemplate == null)
            {
                return convertView;
            }

            var item = list[position];
            var view = GetTemplate(position, item, convertView);
            return view;
        }

        private void NotifierCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyDataSetChanged();
        }
    }
}