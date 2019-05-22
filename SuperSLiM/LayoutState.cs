using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Util;

namespace SuperSLiM
{
	/// <summary>
	/// State to track the current top mMarkerLine views are being mMarkerLine relative to.
	/// </summary>
	public class LayoutState
	{

		public RecyclerView.Recycler recycler;
		public RecyclerView.State recyclerState;
		public SparseArray<Android.Views.View> viewCache;
		public bool isLTR;

		public LayoutState(RecyclerView.LayoutManager layoutManager, RecyclerView.Recycler recycler, RecyclerView.State recyclerState)
		{
            viewCache = new SparseArray<Android.Views.View>(layoutManager.ChildCount);
			this.recyclerState = recyclerState;
			this.recycler = recycler;
			isLTR = layoutManager.LayoutDirection == ViewCompat.LayoutDirectionLtr;
		}

		public void CacheView(int position, Android.Views.View view)
		{
			viewCache.Put(position, view);
		}

		public void DecacheView(int position)
		{
			viewCache.Remove(position);
		}

		public Android.Views.View GetCachedView(int position)
		{
			return viewCache.Get(position);
		}

		public View GetView(int position)
		{
			Android.Views.View child = GetCachedView(position);
			bool wasCached = child != null;
			if (child == null)
			{
				child = recycler.GetViewForPosition(position);
			}

			return new View(child, wasCached);
		}

		public void recycleCache()
		{
			for (int i = 0; i < viewCache.Size(); i++)
			{
				recycler.RecycleView(viewCache.ValueAt(i));
			}
		}

		public class View
		{
			public Android.Views.View view;
			public bool wasCached;

			public View(Android.Views.View child, bool wasCached)
			{
				this.view = child;
				this.wasCached = wasCached;
			}

			public LayoutManager.LayoutParams LayoutParams
			{
				get
				{
					return (LayoutManager.LayoutParams) view.LayoutParameters;
				}
			}
		}
	}
}