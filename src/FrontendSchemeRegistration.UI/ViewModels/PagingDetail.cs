namespace FrontendSchemeRegistration.UI.ViewModels
{

	public class PagingDetail
    {
        public int CurrentPage { get; set; }

        public int TotalItems { get; set; }

        public int PageSize { get; set; }

        public string PagingLink { get; set; }

        public int NextPage => CurrentPage < PageCount ? CurrentPage + 1 : 0;

        public int PreviousPage => CurrentPage > 1 ? CurrentPage - 1 : 0;

        public int FromItem => (PageSize * CurrentPage) - PageSize + 1;

        public int ToItem
        {
            get
            {
                var lastPagesItem = PageSize * CurrentPage;
                return lastPagesItem < TotalItems ? lastPagesItem : TotalItems;
            }
        }

        public List<int> PagingList
        {
            get
            {
                var pagingList = new List<int>();
                if (PageCount > 0)
                {
                    var pagesBeforeCurrentPage = CurrentPage - 1;
                    var pagesAfterCurrentPage = PageCount - CurrentPage;

                    switch (pagesBeforeCurrentPage)
                    {
                        case 0:
                            break;
                        case 1:
                            pagingList.Add(1);
                            break;
                        case 2:
                            pagingList.Add(1);
                            pagingList.Add(2);
                            break;
                        case 3:
                            pagingList.Add(1);
                            pagingList.Add(2);
                            pagingList.Add(3);
                            break;
                        default:
                            pagingList.Add(1);
                            pagingList.Add(0);
                            pagingList.Add(pagesBeforeCurrentPage);
                            break;
                    }

                    pagingList.Add(CurrentPage);

                    switch (pagesAfterCurrentPage)
                    {
                        case 0:
                            break;
                        case 1:
                            pagingList.Add(CurrentPage + 1);
                            break;
                        case 2:
                            pagingList.Add(CurrentPage + 1);
                            pagingList.Add(CurrentPage + 2);
                            break;
                        case 3:
                            pagingList.Add(CurrentPage + 1);
                            pagingList.Add(CurrentPage + 2);
                            pagingList.Add(CurrentPage + 3);
                            break;
                        default:
                            pagingList.Add(CurrentPage + 1);
                            pagingList.Add(0);
                            pagingList.Add(PageCount);
                            break;
                    }
                }

                return pagingList;
            }
         }

        public int PageCount
        {
            get
            {
                if (TotalPages > 0)
                {
                    return TotalPages;
                }

                if (PageSize == 0)
                {
                    return 0;
                }

                return (TotalItems + (PageSize - 1)) / PageSize;
            }
        }
        public int TotalPages
        {
            get;
            set;
        }
    }
}
