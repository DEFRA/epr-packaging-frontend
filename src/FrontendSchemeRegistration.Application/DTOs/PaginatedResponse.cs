namespace FrontendSchemeRegistration.Application.DTOs
{
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; }

        public int CurrentPage { get; set; }

        public int TotalItems { get; set; }

        public int PageSize { get; set; }

        public int PageCount
        {
            get
            {
                if (PageSize == 0)
                {
                    return 0;
                }

                return (TotalItems + (PageSize - 1)) / PageSize;
            }
        }
    }
}
