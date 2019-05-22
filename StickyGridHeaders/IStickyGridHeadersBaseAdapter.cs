using Android.Views;
using Android.Widget;

namespace StickyGridHeaders
{
    public interface IStickyGridHeadersBaseAdapter : IListAdapter
	{
		int GetCountForHeader(int header);
		int NumHeaders {get;}
		View GetHeaderView(int position, View convertView, ViewGroup parent);
	}
}