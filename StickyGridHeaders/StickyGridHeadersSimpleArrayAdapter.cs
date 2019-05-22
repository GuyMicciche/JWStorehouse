using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;
using System.Collections.Generic;
using System.Linq;

namespace StickyGridHeaders
{
	/// <summary>
	/// @author Tonic Artos </summary>
	/// @param <T> </param>
	public class StickyGridHeadersSimpleArrayAdapter<T> : BaseAdapter<T>, IStickyGridHeadersSimpleAdapter
	{
        public static string TAG = Java.Lang.Class.FromType(typeof(StickyGridHeadersSimpleArrayAdapter<T>)).SimpleName;
		private int mHeaderResId;
		private LayoutInflater mInflater;
		private int mItemResId;
		private IList<T> mItems;

		public StickyGridHeadersSimpleArrayAdapter(Context context, IList<T> items, int headerResId, int itemResId)
		{
			Initialize(context, items, headerResId, itemResId);
		}

		public StickyGridHeadersSimpleArrayAdapter(Context context, T[] items, int headerResId, int itemResId)
		{
			Initialize(context, items, headerResId, itemResId);
		}

        private void Initialize(Context context, IList<T> items, int headerResId, int itemResId)
        {
            this.mItems = items;
            this.mHeaderResId = headerResId;
            this.mItemResId = itemResId;
            mInflater = LayoutInflater.From(context);
        }

		public override bool AreAllItemsEnabled()
		{
			return false;
		}

		public override int Count
		{
			get
			{
				return mItems.Count;
			}
		}

		public long GetHeaderId(int position)
		{
			T item = this[position];
            ICharSequence value;
			if (item is CharSequence)
			{
				value = (ICharSequence)item;
			}
			else
			{
                value = new Java.Lang.String(item.ToString());
			}

			return value.SubSequence(0, 1)[0];
		}

		public View GetHeaderView(int position, View convertView, ViewGroup parent)
		{
			HeaderViewHolder holder;
			if (convertView == null)
			{
				convertView = mInflater.Inflate(mHeaderResId, parent, false);
				holder = new HeaderViewHolder();
				holder.textView = (TextView)convertView.FindViewById(Android.Resource.Id.Text1);
				convertView.Tag = holder;
			}
			else
			{
				holder = (HeaderViewHolder)convertView.Tag;
			}

            T item = this[position];
            ICharSequence s;
			if (item is CharSequence)
			{
                s = (ICharSequence)item;
			}
			else
			{
                s = new Java.Lang.String(item.ToString());
			}

			// set header text as first char in string
			holder.textView.SetText(s.SubSequence(0, 1), TextView.BufferType.Normal);

			return convertView;
		}

        public override T this[int position]
        {
            get
            {
                return this.mItems[position];
            }
        }

		public override long GetItemId(int position)
		{
			return position;
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			ViewHolder holder;
			if (convertView == null)
			{
				convertView = mInflater.Inflate(mItemResId, parent, false);
				holder = new ViewHolder();
				holder.textView = (TextView)convertView.FindViewById(Android.Resource.Id.Text1);
				convertView.Tag = holder;
			}
			else
			{
				holder = (ViewHolder)convertView.Tag;
			}

			T item = this[position];
            if (item is CharSequence)
            {
                holder.textView.SetText((ICharSequence)item, TextView.BufferType.Normal);
            }
            else
            {
                holder.textView.SetText(item.ToString(), TextView.BufferType.Normal);
            }

			return convertView;
		}

        public class HeaderViewHolder : Java.Lang.Object
		{
			public TextView textView;
		}

        public class ViewHolder : Java.Lang.Object
		{
			public TextView textView;
		}
	}
}