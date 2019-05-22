using Android.Database;
using Android.Views;
using Android.Widget;

namespace StickyGridHeaders
{
	public class StickyGridHeadersListAdapterWrapper : BaseAdapter, IStickyGridHeadersBaseAdapter
	{
		private DataSetObserver mDataSetObserver;
		private IListAdapter mDelegate;

		public StickyGridHeadersListAdapterWrapper(IListAdapter adapter)
		{
		    mDataSetObserver = new StickyGridHeadersListAdapterWrapperObserver(this);

			mDelegate = adapter;
			if (adapter != null)
			{
				adapter.RegisterDataSetObserver(mDataSetObserver);
			}
		}

		public override int Count
		{
			get
			{
				if (mDelegate == null)
				{
					return 0;
				}
				return mDelegate.Count;
			}
		}

        public int GetCountForHeader(int header)
        {
            return 0;
        }

		public View GetHeaderView(int position, View convertView, ViewGroup parent)
		{
			return null;
		}

		public override Java.Lang.Object GetItem(int position)
		{
			if (mDelegate == null)
			{
				return null;
			}
			return mDelegate.GetItem(position);
		}

		public override long GetItemId(int position)
		{
			return mDelegate.GetItemId(position);
		}

		public override int GetItemViewType(int position)
		{
			return mDelegate.GetItemViewType(position);
		}

		public int NumHeaders
		{
			get
			{
				return 0;
			}
		}

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            return mDelegate.GetView(position, convertView, parent);
        }

		public override int ViewTypeCount
		{
			get
			{
				return mDelegate.ViewTypeCount;
			}
		}

        public override bool HasStableIds
        {
            get
            {
                return mDelegate.HasStableIds;
            }
        } 

        private class StickyGridHeadersListAdapterWrapperObserver : DataSetObserver
        {
            private StickyGridHeadersListAdapterWrapper adapter;

            public StickyGridHeadersListAdapterWrapperObserver(StickyGridHeadersListAdapterWrapper adapter)
            {
                this.adapter = adapter;
            }

            public override void OnChanged()
            {
                adapter.NotifyDataSetChanged();
            }

            public override void OnInvalidated()
            {
                adapter.NotifyDataSetInvalidated();
            }
        }
	}
}