namespace FrontendSchemeRegistration.Application.DTOs
{
    public class PaginatedResponse<T>
    {
	    public string? SearchTerm { get; set; }
        
        public List<T> Items { get; set; }

        public int CurrentPage { get; set; }

        public int TotalItems { get; set; }

        public int PageSize { get; set; }

        public List<string> SearchTerms { get; set; }

        public List<string> TypeAhead { get; set; } = [];

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
