using Android.Database;
using Android.Views;
using Android.Widget;
using System.Linq;
using System.Collections.Generic;
using System;

namespace StickyGridHeaders
{
	/// <summary>
	/// Adapter wrapper to insert extra views and otherwise hack around GridView to
	/// add sections and headers.
	/// 
	/// @author Tonic Artos
	/// </summary>
	public class StickyGridHeadersSimpleAdapterWrapper : BaseAdapter, IStickyGridHeadersBaseAdapter
	{
        private IStickyGridHeadersSimpleAdapter mDelegate { get; set; }
		private List<HeaderData> mHeaders;

        public StickyGridHeadersSimpleAdapterWrapper(object adapter)
		{
            mDelegate = adapter as IStickyGridHeadersSimpleAdapter;
            mHeaders = GenerateHeaderList(mDelegate);
            ((IStickyGridHeadersSimpleAdapter)adapter).RegisterDataSetObserver(new DataSetObserverExtension(this));
		}

		public override int Count
		{
			get
			{
				return mDelegate.Count;
			}
		}

		public int GetCountForHeader(int position)
		{
			return mHeaders[position].Count;
		}

		public View GetHeaderView(int position, View convertView, ViewGroup parent)
		{
			return mDelegate.GetHeaderView(mHeaders[position].RefPosition, convertView, parent);
		}

		public override Java.Lang.Object GetItem(int position)
		{
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
				return mHeaders.Count;
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

		public List<HeaderData> GenerateHeaderList(IStickyGridHeadersSimpleAdapter adapter)
		{
            Dictionary<long, HeaderData> mapping = new Dictionary<long, HeaderData>();
            List<HeaderData> headers = new List<HeaderData>();

			for (int i = 0; i < adapter.Count; i++)
			{
				long headerId = adapter.GetHeaderId(i);
                HeaderData headerData;
                if (!mapping.TryGetValue(headerId, out headerData))
				{
                    headerData = new HeaderData(this, i);
                    headers.Add(headerData);
				}
				headerData.IncrementCount();
				mapping[headerId] = headerData;
			}

			return headers;
		}

		private class DataSetObserverExtension : DataSetObserver
		{
			private StickyGridHeadersSimpleAdapterWrapper adapter;

            public DataSetObserverExtension(StickyGridHeadersSimpleAdapterWrapper adapter)
			{
                this.adapter = adapter;
			}

			public override void OnChanged()
			{
				adapter.mHeaders = adapter.GenerateHeaderList(adapter.mDelegate);
                adapter.NotifyDataSetChanged();
			}

			public override void OnInvalidated()
			{
				adapter.mHeaders = adapter.GenerateHeaderList(adapter.mDelegate);
                adapter.NotifyDataSetInvalidated();
			}
		}

		public class HeaderData
		{
			private StickyGridHeadersSimpleAdapterWrapper adapter;

            public int mCount;
            public int mRefPosition;

            public HeaderData(StickyGridHeadersSimpleAdapterWrapper adapter, int refPosition)
			{
                this.adapter = adapter;
				mRefPosition = refPosition;
				mCount = 0;
			}

			public int Count
			{
				get
				{
					return mCount;
				}
			}

			public int RefPosition
			{
				get
				{
					return mRefPosition;
				}
			}

			public void IncrementCount()
			{
				mCount++;
			}
		}
	}
}