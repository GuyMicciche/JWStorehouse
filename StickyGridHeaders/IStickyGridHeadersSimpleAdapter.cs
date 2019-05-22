using Android.Views;
using Android.Widget;

namespace StickyGridHeaders
{
	public interface IStickyGridHeadersSimpleAdapter : IListAdapter
	{
		long GetHeaderId(int position);
		View GetHeaderView(int position, View convertView, ViewGroup parent);
	}
}